# InMemoryEmulator.ClickHouse

An in-process ClickHouse emulator for component tests. It is a DuckDB-backed query engine behind an
`HttpMessageHandler` that speaks the ClickHouse HTTP interface exactly as
[`ClickHouse.Client`](https://github.com/DarkWanderer/ClickHouse.Client) uses it — no ports, no
lifecycle, no Docker. Hand the handler to an `HttpClient`, hand that to the driver, and the
application code cannot tell the difference.

```csharp
using var server = new InMemoryClickHouseServer(o =>
{
    o.Database = "kitchen_analytics";
    o.ExecuteDdlFile("docker/clickhouse/init/001-kitchen-analytics.sql"); // the same DDL that seeds Docker
});

await using var connection = server.CreateConnection(); // new ClickHouseConnection(server.ConnectionString, server.CreateHttpClient())
await connection.OpenAsync();
```

The project references nothing from BreakfastProvider, so extracting it to its own package is a
directory move.

## Layout

| Path | What |
|---|---|
| `InMemoryClickHouseServer` | Facade: options → engine → handler. `Handler`, `ConnectionString`, `CreateHttpClient()`, `CreateConnection()`. |
| `InMemoryClickHouseOptions` | `Database` (default `default`), `ExecuteDdl(sql)`, `ExecuteDdlFile(path)`. |
| `Core/` | Transport-agnostic: `IClickHouseQueryEngine`, the DuckDB engine, the SQL translator, the type map, the parameter binder, `ClickHouseResultSet` (CLR values + ClickHouse type strings). **Never references `System.Net.Http`** (a unit test enforces this) so a native-TCP front-end could be added without a rewrite. |
| `Http/` | The HTTP front-end: request parsing, `RowBinaryWithNamesAndTypes` and TSV encoders, the handler. |

## What the driver sends, and what the emulator answers

Captured from `ClickHouse.Client` 7.14.0 and ClickHouse 25.8.33.6; every decision below rests on it.

- **Handshake.** On `Open()` the driver POSTs `SELECT version(), timezone() FORMAT TSV` with
  `Content-Type: text/plain`. The emulator answers `25.8.33.6\tUTC\n` — the Docker image's version and
  timezone, so `ServerVersion` reads the same in every lane. Get this wrong and every test fails inside
  `OpenAsync`.
- **Queries.** `POST /?enable_http_compression=<bool>&default_format=RowBinaryWithNamesAndTypes&database=<db>[&param_x=…]`
  with the SQL as the body (`Content-Type: text/sql`). With the driver's default `Compression=true` the
  body is gzip-encoded; the handler decompresses it. Responses are never compressed.
- **Parameters.** `{name:Type}` placeholders stay in the SQL verbatim; values travel as `param_<name>=<text>`
  query-string entries. `DateTime` values are formatted `yyyy-MM-ddTHH:mm:ss` with **no zone and no conversion**,
  so the application must supply UTC values (`DateTime.UtcNow`); the server (`TZ=UTC`) and the emulator then agree.
  The binder rewrites placeholders to DuckDB `$name` parameters and coerces the text by the declared type.
- **Result sets** are encoded as `RowBinaryWithNamesAndTypes` (the driver never appends a `FORMAT` clause
  itself): varint column count; names then type strings as varint-length UTF-8; then per row, per column —
  `String` varint+UTF-8, `UInt8` 1 byte, `Int32`/`UInt32` 4 bytes LE, `Int64`/`UInt64` 8 bytes LE,
  `Float64` IEEE-754 LE, `DateTime` UInt32 unix seconds, `Nullable(T)` one flag byte (`0` = value follows,
  `1` = null). A column whose values include a NULL is reported as `Nullable(T)`; the driver cannot read a
  null in a non-Nullable column.
- **Non-queries** (INSERT / DELETE) return `200` with an empty body and an `X-ClickHouse-Summary` header.
  The driver's `ExecuteNonQuery` returns **0** for both — it does against a real server too — so Kronikol
  shows `0 rows affected` in every lane.
- **Errors** use the server's own status mapping: `SYNTAX_ERROR` (62) → 400, `UNKNOWN_TABLE` (60) and
  `UNKNOWN_IDENTIFIER` (47) → 404, `NOT_IMPLEMENTED` (48, anything outside the supported dialect) → 501,
  everything else → 500; header `X-ClickHouse-Exception-Code`; body
  `Code: <code>. DB::Exception: <message>. (<NAME>) (version 25.8.33.6 (official build))`. The driver
  raises `ClickHouseServerException` whose `Message` is that body. DuckDB errors are mapped by prefix:
  `Parser Error` → 62, `Catalog Error` → 60, `Binder Error` → 47.
- Every response carries `X-ClickHouse-Query-Id`, `X-ClickHouse-Timezone: UTC` and
  `X-ClickHouse-Server-Display-Name: inmemory`.

## The supported dialect — keep the SQL boring

The translator implements exactly this rule list. Anything else throws, naming the fragment; a loud
failure in a component test beats a silently wrong aggregate.

| # | ClickHouse | DuckDB |
|---|---|---|
| 1 | `CREATE DATABASE …` | no-op (one database per emulator) |
| 2 | `ENGINE = MergeTree() ORDER BY (…)` (also `PARTITION BY`, `TTL`, `SETTINGS`) | stripped from DDL |
| 3 | `<Database>.` table qualifier | stripped |
| 4 | DDL types `String` / `Float64` / `Float32` / `DateTime` / `Bool` / `UInt8…UInt64` / `Int8…Int64` / `Nullable(T)` | `VARCHAR` / `DOUBLE` / `FLOAT` / `TIMESTAMP` / `BOOLEAN` / `UTINYINT…UBIGINT` / `TINYINT…BIGINT` / `T` |
| 5 | `{name:Type}` | `$name` + a typed parameter |
| 6 | `quantile(p)(x)` | `quantile_cont(x, p)` |
| 7 | `count()` | `CAST(count(*) AS UBIGINT)` |
| 8 | `SELECT 1` | `SELECT CAST(1 AS UTINYINT) AS "1"` (the server types it `UInt8`) |
| 9 | `DELETE FROM … WHERE …`, `INSERT … VALUES`, `SELECT … GROUP BY … ORDER BY`, `avg()` | pass through |
| 10 | trailing `FORMAT <x>` | stripped by the handler before translation |

Rules of thumb that keep the emulator and the server in agreement:

- **Numbers:** use `Float64` columns; read `count()` with `Convert.ToInt32`; never `sum()` an integer column
  (DuckDB returns a 128-bit `HUGEINT`, which the emulator refuses rather than mis-encodes).
- **Dates:** `DateTime` columns, UTC end to end, no `now()` (DuckDB's is `TIMESTAMPTZ`, also refused).
- **Deletes:** lightweight `DELETE FROM … WHERE …`. With ClickHouse 25.8 defaults the row is gone from the
  next `SELECT`; `ALTER TABLE … DELETE` is an asynchronous mutation and is not supported here.
- **Statements the emulator refuses on purpose:** `FINAL`, `PREWHERE`, `SAMPLE`, `ARRAY JOIN`, `WITH TOTALS`,
  `LIMIT n BY`, `SETTINGS`, `ALTER`/`OPTIMIZE`/`UPDATE`, `quantile*` variants other than `quantile(p)(x)`,
  `to*()` conversion functions, `now()`, `uniq*()`, `groupArray*()`, `argMax()/argMin()`.

## Conformance

`tests/BreakfastProvider.Tests.Unit/Emulator/ClickHouseConformanceTests.cs` runs the same driver-level
tests against the emulator and, when `CLICKHOUSE_CONFORMANCE_CONNECTION_STRING` is set, against a real
ClickHouse (`docker compose -f docker/docker-compose-database.yml up -d clickhouse`, then
`Host=localhost;Port=8123;Database=kitchen_analytics`). CI sets the variable in the Docker-lane jobs.
Any drift — a type string, an error code, a null — fails there, at the driver level.

## Concurrency

One in-memory `DuckDBConnection` behind a `SemaphoreSlim(1,1)`. Tests run in parallel and serialising
sub-millisecond queries is the simplest correct answer. Share one server per process and isolate tests
with randomised keys.
