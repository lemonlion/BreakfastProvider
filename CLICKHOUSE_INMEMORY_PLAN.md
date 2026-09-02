> **Superseded (2026-09-03).** Phases 0-6 of this document were replaced by [CLICKHOUSE_FEATURE_PLAN.md](CLICKHOUSE_FEATURE_PLAN.md), which has been implemented. Only Part 3 (the deferred Octonica / native-TCP front-end) remains relevant. Known-wrong statements here: the emulator lives in its own project (`tests/InMemoryEmulator.ClickHouse`), not under `Shared/Fakes`; Docker is seeded via `docker-entrypoint-initdb.d`, not a curl sidecar; parameters are bound, not string-substituted; the handshake reply is `25.8.33.6`, not `24.8.1.1`; compressed request bodies are decompressed, not refused; deletes use lightweight `DELETE FROM`, not `ALTER TABLE ... DELETE`; the Phase 3.3 line anchors and `BreakfastProvider.Tests.Component/` paths are stale.

# In-memory ClickHouse for BreakfastProvider component tests

**Status:** plan. Nothing here is implemented yet.
**Goal:** a `RunWithAnInMemoryClickHouse` lane that is fully in-process — no Docker, no subprocess —
producing the *same Kronikol diagram arrows* as the Docker lane, for **both** .NET ClickHouse drivers.

**Two stages, one core.**

| Stage | Driver | Transport | Status |
|---|---|---|---|
| **1** | `ClickHouse.Client` | HTTP — an injected `HttpMessageHandler`, no port at all | What BreakfastProvider ships first |
| **2** | `Octonica.ClickHouseClient` | ClickHouse native TCP — an in-process loopback listener | Alternative for teams already on Octonica in production |

The SQL engine, translator and type maps are **transport-agnostic and shared**. Each stage adds a
*front-end* — a transport plus a codec — over that same core. Stage 2 is additive, not a rewrite,
**provided Stage 1 respects the seams in §1.0.**

---

# Part 0 — Decisions, and the evidence for them

These are load-bearing. Everything downstream assumes them.

### 0.1 Stage 1 targets `ClickHouse.Client`; Stage 2 adds `Octonica.ClickHouseClient`

`ClickHouse.Client` speaks HTTP and lets you inject the transport. Reflected from
`ClickHouse.Client` 7.14.0 (`lib/net8.0`):

```
ClickHouseConnection()
ClickHouseConnection(string connectionString)
ClickHouseConnection(string connectionString, HttpClient httpClient)
ClickHouseConnection(string connectionString, IHttpClientFactory factory)
```

That third constructor is the whole Stage 1 story: `new HttpClient(emulatorHandler)`. No port, no
listener, no lifecycle — just a method call.

Octonica exposes no equivalent seam. Reflection over `Octonica.ClickHouseClient` 3.1.10 shows its
constructors take only connection strings/settings and an `IClickHouseTypeInfoProvider`; the
`TcpClient` is created internally with no substitution point. So Stage 2 must be a **real TCP server
on a loopback port**, which is why it is a second stage rather than a parallel option.

That is a well-trodden path here, not a novelty: the InMemory lane already runs in-process servers on
real ports — `InMemoryFakeHelper` starts Kestrel on 5031–5035 (guarded by `AssertPortIsNotInUse`), and
`FakeSpannerServer.Start()` binds a gRPC port. Stage 2 fits that pattern exactly.

**Why BreakfastProvider itself should pick `ClickHouse.Client`.** Stage 1 is strictly less work and
lands sooner; ClickHouse's HTTP interface is the publicly documented, stable one, whereas the native
protocol is gated on revision numbers that shift between server releases. Since ClickHouse is not yet
in `src/`, this is free today and becomes a real refactor once the connection factory, services, health
check, parameter binding and bulk-insert calls are all written against one driver's API. Teams that are
*already* on Octonica in production have made that investment and shouldn't have to unwind it — hence
Stage 2.

**Correcting an earlier estimate.** An initial draft of this plan put Stage 2 at "5–10× the work". That
was wrong, and the findings in §0.2 are why. The honest version: Stage 2's *transport* is
substantially more work, and its *codec* is substantially less. Net, still more — but not by an order
of magnitude, and the work is bounded and well-specified rather than open-ended.

### 0.2 Three findings that make Stage 2 tractable

All three reflected from `Octonica.ClickHouseClient` 3.1.10. Each removes a large chunk of what
"implement the native protocol" would otherwise mean.

**(a) Octonica's column serializers are public — so the codec is nearly free.**

```
DefaultTypeInfoProvider.GetTypeInfo(string)  →  IClickHouseColumnTypeInfo
IClickHouseColumnTypeInfo.CreateColumnWriter(string name, IReadOnlyList<T> rows, ClickHouseColumnSettings)
                                             →  IClickHouseColumnWriter
IClickHouseColumnTypeInfo.CreateColumnReader(int)  →  IClickHouseColumnReader
```

All `public`. The emulator can serialize block columns using **Octonica's own writers**, driven by
ClickHouse type strings. This is the exact inverse of Stage 1, where `ClickHouse.Client`'s
`ClickHouseType` is `internal` and every serializer must be hand-written (Part 1). The block-columnar
encoding I flagged as the scary part of the native protocol is largely a library call.

**(b) Compression is opt-out via the connection string — so LZ4 disappears.**
`ClickHouseConnectionStringBuilder` exposes `bool Compress`. The fake's connection string sets
`Compress=false`, and `CompressionAlgorithm` has only `None` and `Lz4` — no ZSTD to worry about.
**Do not implement LZ4.** Assert loudly if a caller passes `Compress=true`.

**(c) Revision negotiation can be steered *down* — so ~15 gated features never activate.**
`ClickHouseProtocolRevisions` is `public`:

```
CurrentRevision      = 54483
MinSupportedRevision = 54423
```

The fake server advertises `MinSupportedRevision` in its Hello. Everything gated above that never
appears on the wire, including:

| Constant | Revision | What advertising 54423 avoids |
|---|---|---|
| `MinRevisionWithChunkedPackets` | 54470 | chunked packet framing |
| `MinRevisionWithTimezoneUpdates` | 54464 | `TimezoneUpdate` packets |
| `MinRevisionWithParameters` | 54459 | query-parameter block |
| `MinRevisionWithAddendum` | 54458 | the Hello addendum exchange |
| `MinRevisionWithCustomSerialization` | 54454 | custom column serialization modes |
| `MinRevisionWithParallelReplicas` | 54453 | parallel-replica negotiation |
| `MinRevisionWithOpenTelemetry` | 54442 | trace-context propagation |

One caveat to verify by test rather than assume: 54423 is *below*
`MinRevisionWithSettingsSerializedAsStrings` (54429), so the client writes query settings in the older
binary form. The emulator only needs to *skip* the settings block (read to its empty-string terminator),
so either form is fine — but confirm against Octonica's writer before relying on it.

### 0.3 Emulate at the transport layer, not behind a repository interface

This is the same layering as `UseInMemoryBigQuery`
([`ServiceCollectionExtensions.cs:940`](tests/BreakfastProvider.Tests.Component.Shared/Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs#L940)),
which hands the SDK a fake handler via `WithHttpMessageHandlerWrapper`.

```
STAGE 1                                    STAGE 2
ClickHouse.Client.ADO.ClickHouseConnection │ Octonica…ClickHouseConnection
   ↑ Kronikol decorates HERE (both stages) │    ↑ …and HERE
  └─ HttpClient(handler)                   │   └─ TcpClient → 127.0.0.1:<port>
       └─ InMemoryClickHouseHandler        │        └─ NativeProtocolServer
            └─ RowBinary / TSV codec       │             └─ native block codec
                        ↓                  │                        ↓
            ┌───────────────────────────────────────────────────┐
            │  SHARED CORE — IClickHouseQueryEngine (DuckDB)    │
            │  + ClickHouseSqlTranslator + type maps            │
            └───────────────────────────────────────────────────┘
```

**Why not the cheaper repository-level fake.** `Kronikol.Extensions.ClickHouse` is a
`DbConnection`/`DbCommand` decorator — `AddClickHouseTestTracking` calls `DecorateAll<DbConnection>`
and gates on `IsClickHouseConnection`, which walks the base-type chain looking for the type *name*
`ClickHouseConnection`. **Both** drivers name their connection type `ClickHouseConnection`, so the
decorator attaches identically in both stages. Keep a real connection on top of the fake transport and
tracking runs byte-identically in every lane, so the diagrams match. A repository fake bypasses the
extension entirely and the InMemory reports would silently diverge from the Docker reports — which
for this project is the whole point of the exercise.

This is also what makes the two stages *comparable*: the same scenario, run against Stage 1 and Stage 2,
should emit the same Kronikol arrows. That is the cross-stage acceptance test (Phase 8).

A corollary that constrains Part 2: `ClickHouseTrackingOptions` defaults to `LogResponseContent =
true`, `ResponseDetail = RowCountAndColumns`, `MaxResponseRows = 10`. **The tracking decorator
enumerates result rows to render the diagram arrow.** So the response encoder is not merely "good
enough to not throw" — its correctness is visible in the report.

### 0.4 DuckDB executes the SQL

`DuckDB.NET.Data.Full` ships native binaries for win-x64 / linux-x64 / osx, is genuinely in-process,
and is columnar-OLAP — its semantics (`GROUP BY`, window functions, `quantile_cont`, `arg_max`,
`date_trunc`, `LIST`/`STRUCT`) map onto ClickHouse analytics SQL far more directly than SQLite's.

SQLite is the fallback if the query surface turns out trivial — it is already a package reference in
the Shared test project, and it is what
[`UseInMemoryReportingDatabase`](tests/BreakfastProvider.Tests.Component.Shared/Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs#L538)
uses for SQL Server. **Put the engine behind `IClickHouseQueryEngine` so this stays a one-file
swap.** Do not let DuckDB types leak past that interface.

chDB — the only true embedded ClickHouse — is out, though not for the reason an earlier draft of this
plan gave. A .NET binding **does** exist: [`chdb-io/chdb-dotnet`](https://github.com/chdb-io/chdb-dotnet),
publishing the [`chdb`](https://www.nuget.org/packages/chdb/) and
[`chdb-tool`](https://www.nuget.org/packages/chdb-tool/) NuGet packages. It is explicitly experimental
(~18 stars, ~81 commits).

What rules it out is the layer below: **`libchdb` has no native Windows build** — the binding's own
README says "there is no windows build in sight, but you can still use it in WSL", and
[ClickHouse#86093](https://github.com/clickhouse/clickhouse/issues/86093) tracks the same question
upstream. WSL does not help here: the .NET test process runs on the Windows kernel and cannot P/Invoke
a `.so` running under the Linux kernel. Only moving the entire test run onto Linux would work.

Secondary, but still disqualifying for this project: chDB is not ADO.NET, so it exposes no
`DbConnection` for `Kronikol.Extensions.ClickHouse` to decorate — the diagram-parity requirement in
§0.3 would fail even on Linux.

**Revisit if** either the Windows build lands, or you decide component tests run on Linux only. Either
would delete Part 1, Part 3a and the entire translator risk in one move, since chDB *is* ClickHouse.

### 0.5 Build it in-repo first, extract to a package later

The house pattern is published packages (`CosmosDB.InMemoryEmulator`, `Spanner.InMemoryEmulator`,
`InMemoryEmulator.MongoDB`, `InMemoryEmulator.BigQuery`). Resist that **during Stage 1**: the
ClickHouse query surface does not exist yet, so the emulator's required scope is unknown. Build it under
`tests/BreakfastProvider.Tests.Component.Shared/Fakes/ClickHouse/` in a self-contained namespace
(`InMemoryEmulator.ClickHouse`) with **no references to BreakfastProvider types**, so extraction is a
directory move plus a `.csproj`.

**Stage 2 is the trigger to extract.** Its whole justification is teams outside this repo who are
already on Octonica — which only pays off as a published package. The expected shape:

| Package | Contents | Depends on |
|---|---|---|
| `InMemoryEmulator.ClickHouse` | shared core — engine, translator, type maps, options/schema seeding | DuckDB.NET |
| `InMemoryEmulator.ClickHouse.Http` | `HttpMessageHandler` + RowBinary/TSV codec | core |
| `InMemoryEmulator.ClickHouse.Native` | TCP listener + native protocol + block codec | core, `Octonica.ClickHouseClient` |

Splitting the front-ends keeps each driver dependency optional — an HTTP-only consumer never pulls in
Octonica, and vice versa. **This is the reason §1.0's seams are non-negotiable in Stage 1**: get the
core/front-end boundary wrong and this split becomes a rewrite.

---

# Part 1 — The HTTP wire protocol (Stage 1)

The only consumer is `ClickHouse.Client`. It is the authoritative spec, and it is on disk — prefer
reading/decompiling it over reading ClickHouse's docs where the two disagree. Part 5 does the same job
for the native protocol.

### 1.1 Connection handshake

Extracted from the assembly's string heap: on open, the client issues

```
SELECT version(), timezone() FORMAT TSV
```

**This is TSV, not RowBinary, and it is the first thing the emulator must answer.** Reply with a
single tab-separated line, e.g. `24.8.1.1\tUTC\n`. Get this wrong and every test fails at
`OpenAsync` with a confusing parse error.

### 1.2 Reads

Confirmed from the assembly: results come back as **`RowBinaryWithNamesAndTypes`**. Layout:

| Segment | Encoding |
|---|---|
| Column count *N* | varint (LEB128) |
| *N* column names | varint length + UTF-8 |
| *N* column type strings | varint length + UTF-8 (`String`, `UInt64`, `Nullable(Float64)`, …) |
| Rows | *N* values in column order, per-type encoding, repeated until stream end |

Per-value encodings needed for a first cut:

| ClickHouse type | Encoding |
|---|---|
| `String` | varint length + UTF-8 bytes |
| `UInt8/16/32/64`, `Int8/16/32/64` | fixed-width little-endian |
| `Float32/64` | IEEE-754 LE |
| `Date` | `UInt16` days since 1970-01-01 |
| `DateTime` | `UInt32` unix seconds |
| `DateTime64(p)` | `Int64` ticks at precision *p* |
| `Nullable(T)` | 1 null-flag byte, then the `T` value if not null |
| `Array(T)` | varint element count, then elements |
| `Decimal(P,S)` | scaled integer at the width implied by *P* |

**Two shortcuts, both verified:**

- `System.IO.BinaryWriter.Write(string)` already emits varint-length-prefixed UTF-8 — that is exactly
  ClickHouse's `String` encoding. The whole string/varint layer is free.
- `ClickHouse.Client.Formats.ExtendedBinaryWriter` is **public** and adds `Write7BitEncodedInt`.
  Use it for the varints.

**One thing that is not available:** `ClickHouse.Client.Types.ClickHouseType` (the abstract base with
`Read`/`Write`) is **internal**. The per-type serializers cannot be reused — we write our own. That
is the single largest work item in this plan.

### 1.3 Writes

Two distinct paths, both must work:

- **Statement INSERT** — plain SQL text (`INSERT INTO t VALUES (…)`, or with `@parameters`). Goes
  through the normal query path.
- **`ClickHouseBulkCopy`** — request body is `RowBinary` (no names/types header; see the public
  `ClickHouse.Client.Copy.RowBinaryFormat`). The emulator must **decode** this, using the column list
  from the `INSERT INTO t (a, b, c) FORMAT RowBinary` preamble to know the types.

Defer bulk copy to §2.5 — do not block the first green test on it.

### 1.4 Errors

Non-2xx status with a ClickHouse-shaped body: `Code: 62. DB::Exception: Syntax error: …`. The client's
exact expectations here were not confirmed by inspection — **verify with a deliberate failing-query
test in §2.4** rather than assuming.

### 1.5 Query-string parameters to tolerate

`default_format`, `query_id`, `session_id`, `enable_http_compression`, `compress`, `database`. Accept
and ignore all of them; honour `default_format` only if a query carries no explicit `FORMAT` clause.
SQL may arrive in the POST body *or* the `query=` parameter — handle both.

---

# Part 2 — Work breakdown (Stage 1)

Phases 0–6 — Stage 1, the HTTP front-end. Stage 2 is Part 5 (phases 7–9). Each phase is independently
green-able. TDD throughout, per
[`.claude/skills/component-tests/SKILL.md`](.claude/skills/component-tests/SKILL.md) — the codec work
in particular is pure unit-testable logic with no excuse for skipping red-green-refactor.

---

## Phase 0 — The src-side ClickHouse surface (prerequisite)

Nothing here is test infrastructure; it is the feature you were about to build anyway. Mirror the
Spanner shape, which is the closest existing precedent because it uses a **connection factory**
rather than a DI-registered `DbConnection`.

| File | Content |
|---|---|
| `src/BreakfastProvider.Api/Configuration/ClickHouseConfig.cs` | `ConnectionString`, `DatabaseName`; validator following `SpannerConfigValidator` |
| `src/BreakfastProvider.Api/Data/ClickHouse/IClickHouseConnectionFactory.cs` | `DbConnection CreateConnection()` |
| `src/BreakfastProvider.Api/Data/ClickHouse/ClickHouseConnectionFactory.cs` | returns `new ClickHouseConnection(connString)` |
| `src/BreakfastProvider.Api/Data/ClickHouse/NoOpClickHouseConnectionFactory.cs` | for empty config, mirroring `NoOpSpannerConnectionFactory` |
| `src/BreakfastProvider.Api/Services/<YourService>.cs` | whatever ClickHouse-backed feature you are actually adding |
| `src/BreakfastProvider.Api/Services/HealthChecks/ClickHouseHealthCheck.cs` | mirror `BigQueryHealthCheck` |
| `src/BreakfastProvider.Api/StartupExtensions.cs` | `AddClickHouse(configuration)` following `AddSpannerDatabase` (line 207) and `AddBigQuery` (line 272); call it from `Program.cs` beside the others (~line 238) |

Also add to the nested `Documentation.ServiceNames` class (`StartupExtensions.cs:309`):

```csharp
public const string ClickHouse = "ClickHouse";
```

**Design constraint that matters later:** the factory must return `DbConnection`, and the concrete
type must remain `ClickHouse.Client.ADO.ClickHouseConnection`. Do not wrap it in your own
`DbConnection` subclass — `IsClickHouseConnection` walks base types for the *name*
`ClickHouseConnection`, so a wrapper named anything else silently disables tracking.

**Keep the SQL boring.** Every ClickHouse-ism you use is one you must translate in Phase 1. Prefer
standard aggregate SQL; reach for `argMax`/`quantile`/`toStartOfHour` only when the feature genuinely
needs it.

---

## Phase 1 — Codec + engine, no HTTP

All under `tests/BreakfastProvider.Tests.Component.Shared/Fakes/ClickHouse/`, namespace
`InMemoryEmulator.ClickHouse`. Unit-tested in `tests/BreakfastProvider.Tests.Unit`.

### 1.0 The seams that make Stage 2 additive

Stage 2 reuses everything in this phase **except** §1.1–§1.3 (the RowBinary/TSV codec, which is
HTTP-specific). Three rules keep that true. They cost nothing now and are expensive to retrofit:

1. **The core never names a transport type.** `Core/` may not reference `HttpRequestMessage`,
   `HttpResponseMessage`, `HttpMessageHandler`, `Stream`, or `Socket`. Put the codec in `Http/` and the
   Stage 2 codec later in `Native/`.
2. **Results cross the boundary as data, not bytes.** A front-end receives `ClickHouseResultSet`
   (§1.4) — CLR values plus ClickHouse type strings — and encodes it however its wire format demands.
   Row-order for RowBinary, column-order for native blocks; both project cheaply from the same shape.
   Never let the engine hand back pre-encoded bytes.
3. **Type strings are the lingua franca.** The translator's DuckDB→ClickHouse type map (§1.5) emits
   canonical ClickHouse type strings (`UInt64`, `Nullable(String)`, `DateTime64(3)`). Stage 1 feeds
   them to its hand-written writers; Stage 2 feeds the *same strings* to
   `DefaultTypeInfoProvider.GetTypeInfo(...)`. One map, two consumers — so a type-mapping bug is
   fixed once.

Target layout, from the first commit:

```
Fakes/ClickHouse/
  Core/      ClickHouseColumn, ClickHouseResultSet, IClickHouseQueryEngine,
             DuckDbClickHouseQueryEngine, ClickHouseSqlTranslator, ClickHouseTypeMap,
             InMemoryClickHouseOptions, ClickHouseTableBuilder
  Http/      RowBinaryWriter, RowBinaryReader, TsvWriter, InMemoryClickHouseHandler
  Native/    (Stage 2 — empty for now)
```

A cheap enforcement that actually holds the line: one unit test asserting no type in the `Core`
namespace references `System.Net.Http` or `System.Net.Sockets`.

### 1.1 `RowBinaryWriter`

```csharp
internal sealed class RowBinaryWriter(Stream stream)
{
    public void WriteHeader(IReadOnlyList<ClickHouseColumn> columns);
    public void WriteRow(IReadOnlyList<object?> values, IReadOnlyList<ClickHouseColumn> columns);
}

internal readonly record struct ClickHouseColumn(string Name, string TypeString);
```

**TDD order** — one failing test per type, smallest first: `String` → `UInt64`/`Int32` → `Float64` →
`DateTime` → `Date` → `Nullable(T)` → `Array(T)` → `Decimal(P,S)`.

**The assertion that makes this trustworthy:** do not hand-write expected byte arrays. Write the
bytes, then feed them into a real `ClickHouseDataReader` obtained from a real `ClickHouseConnection`
wired to a stub handler that returns those bytes, and assert on the materialised CLR values. That
makes the client its own oracle and catches encoding drift on a package bump. Hand-written byte
arrays are worth it for exactly two cases — varint boundary (127/128) and the `Nullable` flag — where
you want to pin the layout itself.

### 1.2 `RowBinaryReader`

The inverse, for bulk-copy request bodies. Same round-trip discipline: encode with
`ClickHouseBulkCopy` against a capturing handler, decode with `RowBinaryReader`, assert equality.
Deferred to §2.5 — stub it to `throw new NotSupportedException` initially.

### 1.3 `TsvWriter`

Trivial, but needed for the §1.1 handshake and any `FORMAT TSV` query. Tab-separated, `\n`-terminated,
ClickHouse escaping (`\t`, `\n`, `\\` escaped; `\N` for null).

### 1.4 `IClickHouseQueryEngine`

```csharp
internal interface IClickHouseQueryEngine : IDisposable
{
    ClickHouseResultSet Execute(string sql);          // SELECT → columns + rows
    int ExecuteNonQuery(string sql);                  // INSERT/DDL → affected rows
    void BulkInsert(string table, IReadOnlyList<ClickHouseColumn> cols, IEnumerable<object?[]> rows);
}

internal sealed record ClickHouseResultSet(
    IReadOnlyList<ClickHouseColumn> Columns,
    IReadOnlyList<object?[]> Rows);
```

`DuckDbClickHouseQueryEngine` implements it over an in-memory DuckDB connection
(`DuckDBConnection("DataSource=:memory:")`).

### 1.5 `ClickHouseSqlTranslator`

The bit that will grow forever if you let it. Keep it a **narrow, explicit, test-per-rule** mapping:

| ClickHouse | DuckDB |
|---|---|
| `ENGINE = MergeTree() ORDER BY (…)` / `PARTITION BY` / `TTL` | strip from DDL |
| `Nullable(T)` in DDL | `T` (DuckDB is nullable by default) |
| `String` / `UInt64` / `Float64` / `DateTime` | `VARCHAR` / `UBIGINT` / `DOUBLE` / `TIMESTAMP` |
| `toStartOfHour(x)` / `toStartOfDay(x)` | `date_trunc('hour', x)` / `date_trunc('day', x)` |
| `uniqExact(x)` | `count(DISTINCT x)` |
| `argMax(a, b)` | `arg_max(a, b)` |
| `quantile(p)(x)` | `quantile_cont(x, p)` |
| `SELECT … FINAL` | strip `FINAL` |
| `now()` / `today()` | pass through |

Also needs the **type map back out**: DuckDB result column types → ClickHouse type strings for the
RowBinary header. That is a second table, and it is where mismatches will bite (`UBIGINT` → `UInt64`,
`TIMESTAMP` → `DateTime64(3)`, `DECIMAL(p,s)` → `Decimal(p,s)`).

**Rule:** every translator entry gets a unit test, and the translator throws
`NotSupportedException` with the offending fragment on anything it does not recognise — a loud
failure in a component test beats a silently wrong aggregate.

**Exit criteria:** unit tests green; no HTTP, no DI, no BreakfastProvider references.

---

## Phase 2 — The HTTP handler

### 2.1 `InMemoryClickHouseHandler : HttpMessageHandler`

```csharp
public sealed class InMemoryClickHouseHandler : HttpMessageHandler
{
    public InMemoryClickHouseHandler(InMemoryClickHouseOptions options);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage, CancellationToken);
}
```

`SendAsync` pipeline:

1. Extract SQL — POST body, else `query=` query-string param.
2. Extract and strip a trailing `FORMAT <name>` clause; fall back to `default_format`, then
   `RowBinaryWithNamesAndTypes`.
3. Short-circuit the handshake: `SELECT version(), timezone()` → canned TSV (§1.1).
4. Translate, dispatch to the engine, encode in the requested format.
5. On engine exception → ClickHouse-shaped error response (§1.4).

### 2.2 `InMemoryClickHouseOptions`

Follow the BigQuery emulator's fluent schema-seeding shape so the two read alike:

```csharp
public sealed class InMemoryClickHouseOptions
{
    public string Database { get; set; } = "default";
    public void AddTable(string name, Action<ClickHouseTableBuilder> configure);
    public void ExecuteDdl(string sql);   // escape hatch — mirrors FakeSpannerServer.Database.ExecuteDdl
}
```

`ExecuteDdl` matters: the Spanner fixture seeds schema with raw DDL
([`BaseFixture.cs:83`](tests/BreakfastProvider.Tests.Component.xUnit/Infrastructure/BaseFixture.cs#L83)),
and being able to paste your real `CREATE TABLE` straight from `src` is the cheapest possible fidelity
check.

### 2.3 Lifetime and isolation

The engine is a **process-wide singleton** held by the handler, matching the shared-cache SQLite
(`Cache=Shared` + keep-alive connection) and shared `FakeSpannerServer` precedents. Component tests
run in parallel and must not share mutable rows — so, exactly as the existing suites do, **every test
randomises its entity IDs** and asserts only on its own rows. Do not add a per-test database; it
would fight the `WebApplicationFactory` lifetime and diverge from every other backend here.

### 2.4 First integration test

`tests/BreakfastProvider.Tests.Unit`, no ASP.NET:

```
var handler = new InMemoryClickHouseHandler(opts);
using var conn = new ClickHouseConnection("Host=inmemory;Port=8123", new HttpClient(handler));
await conn.OpenAsync();                        // exercises the §1.1 handshake
// INSERT, then SELECT, then assert materialised values off ClickHouseDataReader
```

Add the deliberate-error case here to pin §1.4.

**Exit criteria:** a real `ClickHouseConnection` opens, inserts and reads back over the fake handler,
in-process, on Windows, with no Docker running.

### 2.5 Bulk copy

Only if `src` actually uses `ClickHouseBulkCopy`. Implement `RowBinaryReader` (§1.2) and the
`INSERT INTO t (cols) FORMAT RowBinary` preamble parser.

---

## Phase 3 — Lane wiring

### 3.1 Settings

`ComponentTestSettings.cs` — add beside `RunWithAnInMemoryBigQuery`:

```csharp
public bool RunWithAnInMemoryClickHouse { get; set; }
```

Add `"RunWithAnInMemoryClickHouse": true` to **all** `appsettings.componenttests.json` — the Shared
copy plus each framework project's copy — and to the four
`tests/BreakfastProvider.Tests.Component/Configure/switch-to-*` scripts.

### 3.2 Two extension methods

In `ServiceCollectionExtensions.cs`, directly modelled on `UseInMemorySpannerDatabase`
(line 701) since both swap a connection factory:

```csharp
public static IServiceCollection UseInMemoryClickHouse(
    this IServiceCollection services, Func<(string Name, string Id)> currentTestInfoFetcher)
{
    var options = new ClickHouseTrackingOptions
    {
        ServiceName = Documentation.ServiceNames.ClickHouse,
        CallerName  = Documentation.ServiceNames.BreakfastProvider,
        Verbosity   = SqlTrackingVerbosityLevel.Detailed,
        CurrentTestInfoFetcher = currentTestInfoFetcher,
    };

    var handler = SharedInMemoryClickHouse.Handler;   // process-wide, seeded once
    services.RemoveAll<IClickHouseConnectionFactory>();
    services.AddSingleton<IClickHouseConnectionFactory>(
        new InMemoryClickHouseConnectionFactory(handler, options));
    return services;
}

public static IServiceCollection UseTrackedClickHouse(
    this IServiceCollection services, Func<(string Name, string Id)> currentTestInfoFetcher)
{ /* same options; real ClickHouseConnection against the Docker container */ }

public static IServiceCollection ReplaceClickHouseHealthCheckWithNoOp(this IServiceCollection services)
{ /* mirror ReplaceBigQueryHealthCheckWithNoOp */ }
```

`InMemoryClickHouseConnectionFactory.CreateConnection()` returns

```csharp
new ClickHouseConnection(connString, new HttpClient(handler)).WithClickHouseTestTracking(options)
```

— real connection type underneath, tracking decorator on top. Note the factory's declared return type
stays `DbConnection`, so `TrackingClickHouseConnection` fits without changing `src`.

### 3.3 Six call sites

The mode switch is duplicated per framework. Add the same block to each:

| File | Anchor |
|---|---|
| `tests/BreakfastProvider.Tests.Component.xUnit/Infrastructure/BaseFixture.cs` | `ConfigureTestServices`, after the BigQuery block (~line 402) |
| `tests/BreakfastProvider.Tests.Component.NUnit/Infrastructure/BaseFixture.cs` | same |
| `tests/BreakfastProvider.Tests.Component.BDDfy/Infrastructure/BaseFixture.cs` | same |
| `tests/BreakfastProvider.Tests.Component.TUnit/Infrastructure/BaseFixture.cs` | same |
| `tests/BreakfastProvider.Tests.Component.LightBDD/Infrastructure/BaseFixture.cs` | same |
| `tests/BreakfastProvider.Tests.Component.ReqNRoll/Support/AppManager.cs` | `ConfigureTestServices` (~line 343) |

```csharp
if (Settings.RunWithAnInMemoryClickHouse)
{
    services.UseInMemoryClickHouse(CurrentTestInfo.Fetcher);
    services.ReplaceClickHouseHealthCheckWithNoOp();
}
else
{
    services.UseTrackedClickHouse(CurrentTestInfo.Fetcher);
}
```

> Six near-identical copies is a pre-existing smell, not one this plan creates. Worth noting as
> follow-up work — a shared `ConfigureBackends(services, settings)` in the Shared project would
> collapse all six — but **do not** attempt that refactor inside this change.

---

## Phase 4 — Docker lane parity

`docker/docker-compose-database.yml` — add beside `mongodb`:

```yaml
  clickhouse:
    container_name: clickhouse
    image: clickhouse/clickhouse-server:24-alpine
    hostname: clickhouse.local
    ports: ['8123:8123', '9000:9000']
    environment:
      - CLICKHOUSE_DB=BreakfastAnalytics
      - CLICKHOUSE_USER=default
      - CLICKHOUSE_PASSWORD=
      - CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT=1
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://localhost:8123/ping || exit 1"]
      interval: 5s
      timeout: 5s
      retries: 30
      start_period: 10s
    networks:
      localdev:
        aliases: [clickhouse.local]
```

Plus a `clickhouse-init` sidecar following the `bigquery-init` / `spanner-init` pattern, POSTing the
same `CREATE TABLE` DDL that seeds the in-memory options. **Keep the two DDL sources identical** —
ideally one shared const string the init script and `ExecuteDdl` both consume. Divergence here is the
most likely source of "passes InMemory, fails Docker".

Also: `docker-compose-sut.yml` needs `ClickHouseConfig__ConnectionString=Host=clickhouse.local;Port=8123;…`
for External SUT mode, and `appsettings.componenttests.json` needs the localhost `ClickHouseConfig`
block alongside `MongoDbConfig`.

---

## Phase 5 — The proving scenario

One component test feature exercising the ClickHouse-backed endpoint end to end, written per the
existing conventions (partial class, GIVEN/WHEN/THEN, `[FeatureDescription]`, AwesomeAssertions,
randomised IDs, no `[Collection]`).

**The acceptance bar is not "the test passes."** It is:

1. Green in the InMemory lane with Docker stopped.
2. Green in the Docker lane.
3. The generated `TestRunReport.html` shows a **ClickHouse participant with the same arrows in both
   lanes** — same operation classification, same SQL summary, same row-count/column response content.

(3) is what justifies the whole architecture. Diff the two reports; any divergence is a codec or
translator bug, not a reporting quirk. Start it in the xUnit project, then replicate across the other
five suites once it is stable.

---

## Phase 6 — Documentation

- `.claude/skills/component-tests/test-infrastructure.md` — add ClickHouse rows to the mode-comparison
  table and the settings-flags table, and a `RunWithAnInMemoryClickHouse` entry.
- `README.md` — ClickHouse in the dependency list.
- A short `Fakes/ClickHouse/README.md` recording the wire-protocol facts from Part 1 (the
  `SELECT version(), timezone() FORMAT TSV` handshake in particular) — that detail costs an hour to
  rediscover.
- Per `CLAUDE.md`: version bump across all packages, changelog entry, tag, push.

---

# Part 3 — Stage 2: the Octonica / native-TCP front-end

**Do not start this until Stage 1 is green through Phase 5.** Its entire value is proving the core is
transport-agnostic, and that proof is worthless if the core is still moving.

**Who this is for.** Teams already running `Octonica.ClickHouseClient` in production, for whom
"switch drivers to get an in-memory test lane" is not an acceptable answer. BreakfastProvider itself
stays on `ClickHouse.Client` — see the honest scoping note in §9.3.

## Part 3a — The native wire protocol

Same discipline as Part 1: `Octonica.ClickHouseClient` is the authoritative spec and it is on disk.
`ClickHouseBinaryProtocolReader` / `ClickHouseBinaryProtocolWriter`, `BlockHeader` and
`BlockFieldCodes` are all `internal` — **decompile them** rather than working from ClickHouse's docs.
Field order and revision gating are what will bite, and the reader is the ground truth.

### 3a.1 Message codes

Reflected from the internal enums (values confirmed):

| `ClientMessageCode` | | `ServerMessageCode` (subset a minimal server needs) | |
|---|---|---|---|
| `Hello` | 0 | `Hello` | 0 |
| `Query` | 1 | `Data` | 1 |
| `Data` | 2 | `Error` | 2 |
| `Cancel` | 3 | `Progress` | 3 |
| `Ping` | 4 | `Pong` | 4 |
| | | `EndOfStream` | 5 |
| | | `ProfileInfo` | 6 |

The other twelve server codes (`Totals`, `Extremes`, `TableColumns`, `PartUuids`, `ProfileEvents`,
`MergeTree*`, …) are either revision-gated above 54423 or only sent in response to features the
emulator will never advertise. **Implement seven, not nineteen.**

### 3a.2 Handshake

Client → `Hello`: client name, version major, version minor, protocol revision (varint), database,
user, password.

Server → `Hello`: server name, version major, version minor, **revision = 54423**, then the fields
gated *below* 54423 — server timezone, display name, version patch.

A pleasant consequence of Octonica's `MinSupportedRevision = 54423`: **everything gated at or below
that revision is unconditionally present**, because Octonica cannot talk to anything older. So there
is no conditional-field logic in the Hello at all — one fixed layout. Confirm the exact field order
against `ClickHouseBinaryProtocolReader`.

### 3a.3 Query and result flow

```
client → Query   : query id, client-info block, settings (→ empty-string terminator),
                   query stage, compression flag, query text
client → Data    : one empty block (signals "no external tables")
server → Data    : header block — columns with names + types, ZERO rows
server → Data    : block(s) carrying the actual rows
server → EndOfStream
```

`Progress` and `ProfileInfo` are optional; send neither until a test proves Octonica stalls without one.

**Block layout:** `BlockInfo` (field-code/value pairs, terminated by code 0), then `num_columns`
(varint), `num_rows` (varint), then per column — name string, type string, and the column data written
by Octonica's own `IClickHouseColumnWriter` (§0.2a).

### 3a.4 INSERT is a server-initiated schema handshake

This is the sharpest divergence from HTTP and the place to expect trouble:

```
client → Query   : "INSERT INTO t (a, b) VALUES"
server → Data    : EMPTY header block describing the target columns   ← server speaks first
client → Data    : block(s) of rows
client → Data    : empty block (terminator)
server → EndOfStream
```

The server must resolve the target table's column types from the engine's schema and send them
*before* the client will write anything. Read incoming column data with
`IClickHouseColumnTypeInfo.CreateColumnReader(int)`.

### 3a.5 Errors and ping

`Error` (code 2) carries: exception code (Int32), name, message, stack trace, `has_nested` flag. Map
engine exceptions onto plausible ClickHouse codes — `62` syntax error, `60` unknown table, `47`
unknown identifier. `Ping` (4) → `Pong` (4), with no query machinery involved.

---

## Phase 7 — `NativeProtocolServer`

Under `Fakes/ClickHouse/Native/`. Depends on `Core/` only — never on `Http/`.

```csharp
public sealed class InMemoryClickHouseNativeServer : IDisposable
{
    public InMemoryClickHouseNativeServer(IClickHouseQueryEngine engine);
    public void Start();                 // binds 127.0.0.1:0
    public int Port { get; }             // the OS-assigned port
    public string ConnectionString { get; }  // "Host=127.0.0.1;Port=…;Compress=false;User=default"
}
```

**Bind port 0, not a fixed port.** Nothing external needs to find this listener — the connection string
is handed straight to the factory — so let the OS assign an ephemeral port. That sidesteps the
`AssertPortIsNotInUse` contention the fixed-port HTTP fakes have to manage, and makes the server safe to
instantiate per-fixture. Mirror `FakeSpannerServer`'s `Start()` / `ConnectionString` shape so it reads
like the existing emulators.

**TDD order** — each step is a real Octonica connection against the server, asserting progressively more:

1. `Ping`/`Pong` only → `ClickHouseConnection.OpenAsync()` succeeds. *This is the milestone that
   proves the handshake, and it is where most of the protocol risk lives.*
2. `SELECT 1` → one Int32 column, one row.
3. Multi-column, multi-type SELECT — walk the same type list as §1.1, now via Octonica's writers.
4. Zero-row SELECT (header block, no data block, EndOfStream).
5. `INSERT … VALUES` → the §3a.4 handshake.
6. Deliberate bad SQL → `Error` packet surfaces as `ClickHouseServerException` with the right code.
7. Sequential commands on one open connection — the state machine must reset cleanly between queries.

**Exit criteria:** a real `Octonica.ClickHouseClient.ClickHouseConnection` opens, inserts and reads
back over loopback, in-process, no Docker.

---

## Phase 8 — Cross-stage parity

The test that justifies the whole two-front-end architecture.

**8.1 Same core, same answers.** Parameterise the Phase 1 engine/translator unit tests over both
front-ends and assert identical `ClickHouseResultSet` values reach the caller. Any divergence is a
codec bug in one front-end, isolated by construction.

**8.2 Same Kronikol arrows.** Run one representative scenario three ways — Stage 1 InMemory, Stage 2
InMemory, Docker — and diff the generated `TestRunReport.html`. Operation classification, SQL summary
and row-count/column response content must match. Since `Kronikol.Extensions.ClickHouse` sits above
both drivers and classifies SQL *text*, they should agree; where they do not, the interesting question
is whether the divergence is in the emulator or in Kronikol's driver detection — and either answer is
worth having.

**8.3 Wiring.** Add a `ClickHouseDriver` enum (`Http` | `Native`) to `ComponentTestSettings`, defaulting
to `Http`, consumed inside `UseInMemoryClickHouse` so the six call sites in §3.3 stay untouched.

---

## Phase 9 — Extraction and packaging

**9.1** Split into the three packages from §0.5: `InMemoryEmulator.ClickHouse` (core),
`.Http`, `.Native`. The §1.0 namespace layout makes this a project-file exercise if the seams held —
and if they did not, this is where you find out.

**9.2** Version, changelog, tag, push per `CLAUDE.md`. Document both front-ends in the package README,
including the Part 1 / Part 3a wire-protocol notes — those cost hours to rediscover.

**9.3 Honest scoping.** BreakfastProvider will not exercise Stage 2 end-to-end, because its `src` uses
`ClickHouse.Client`. Phase 7's tests are protocol-level and Phase 8.2 needs an Octonica-backed
scenario that this repo has no reason to own. **Stage 2's real acceptance tests belong in the extracted
emulator repo**, with a small sample app on Octonica. Either accept that Stage 2 ships here with
protocol-level coverage only and gains end-to-end coverage after extraction, or extract *before*
Phase 8 and do the parity work there. The second is cleaner; it just front-loads the packaging work.

---

# Part 4 — Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| RowBinary encoding drift on a `ClickHouse.Client` upgrade | Medium | Round-trip tests use the client as oracle (§1.1) — a bump that changes encoding fails loudly |
| Translator gaps discovered late, mid-feature | **High** | Throw `NotSupportedException` naming the fragment; keep `src` SQL boring (Phase 0) |
| DuckDB ↔ ClickHouse type-map mismatches surfacing as wrong diagram content | Medium | Phase 5 acceptance bar (3) catches these by construction |
| In-memory and Docker DDL drifting apart | Medium | Single shared DDL const (Phase 4) |
| Bulk-copy `RowBinary` decode is harder than expected | Low | Deferred to §2.5; not on the critical path |
| Six duplicated wiring sites drift | Low | Same block, added in one commit; refactor deferred |

**Stage 2 only:**

| Risk | Likelihood | Mitigation |
|---|---|---|
| Handshake field order wrong — nothing works and the error is opaque | **High** | Decompile `ClickHouseBinaryProtocolReader` rather than guessing; Phase 7 step 1 (`Ping`/`Pong` alone) isolates it before any query logic exists |
| Octonica's client stalls waiting for a packet we never send | Medium | Start with the seven codes in §3a.1; add `Progress`/`ProfileInfo` only when a hanging test demands it |
| Revision 54423 turns out to still gate something we assumed away | Medium | Verify the §0.2c settings-serialization caveat by test in Phase 7 step 2, not by reading |
| Core/front-end seams leak, making Phase 9's split a rewrite | Medium | The §1.0 namespace-reference test, enforced from Stage 1's first commit |
| Octonica bumps its protocol revision and the fake's fixed 54423 stops being accepted | Low | `MinSupportedRevision` is a public constant — read it at runtime instead of hard-coding 54423 |
| Stage 2 ships with no end-to-end coverage in this repo | **High** | Accepted and documented in §9.3 — decide extraction timing deliberately, don't drift into it |

# Part 5 — Order of work

**Stage 1:**

```
Phase 0 (src) ─┬─ Phase 1 (codec + engine, unit-tested)
               │        └─ Phase 2 (handler; first real ClickHouseConnection round-trip)
               │                 └─ Phase 3 (lane wiring)
               └─ Phase 4 (docker) ──────────┴─ Phase 5 (proving scenario, both lanes) ─ Phase 6 (docs)
```

Phases 1–2 are the bulk of the effort and are pure, dependency-free, unit-testable code — they can
start the moment Phase 0 fixes the table schemas. Phase 4 is independent of 1–3 and can run in
parallel.

**Stage 2** — begins only after Phase 5 is green:

```
Phase 5 green ─ Phase 7 (native server) ─ Phase 8 (cross-stage parity) ─ Phase 9 (extract + package)
```

Phase 7 reuses `Core/` untouched; its work is entirely in `Native/`. If Phase 7 turns out to need
changes inside `Core/`, that is the §1.0 seams having failed — stop and fix the seam rather than
threading a transport concern through the core.

Per §9.3, consider running Phase 9 *before* Phase 8 so the parity work happens in the extracted repo
where an Octonica sample app can live.
