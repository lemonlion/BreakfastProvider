# Octonica migration plan — switch ClickHouse driver from ClickHouse.Client to Octonica.ClickHouseClient

> **Status: SUPERSEDED (2026-09-03, same day) — Path B chosen and executed.** After reviewing §0's
> landscape finding, the decision was to migrate to `ClickHouse.Driver` (the official client)
> instead of Octonica: `Kronikol.Extensions.ClickHouse.Driver` was added in Kronikol 3.0.75 and
> BreakfastProvider switched src/tests/emulator to `ClickHouse.Driver` 1.4.0. Path A below is
> retained for reference should native TCP ever become a requirement; its VERIFIED facts remain
> valid. Originally written 2026-09-03. Every claim marked
> VERIFIED below was live-probed on that date against ClickHouse 25.8 in Docker with
> Octonica.ClickHouseClient 3.1.10 and Kronikol 3.0.74; TO-VERIFY items are the residual risk.
>
> **Requirement driving this plan:** after the switch, Kronikol must track **the same amount of
> information** as today — diagram arrows, note payloads, rows-affected counts, URIs, parameters
> and flame charts must not lose content.

---

## 0. The premise, checked first

The stated rationale for the switch is that Octonica appears to be "the modern preferred"
ClickHouse client. The investigation surfaced a fact that complicates that premise:

- **In March 2026 ClickHouse released `ClickHouse.Driver` 1.0.0 — the first *official* .NET
  client** ([announcement](https://clickhouse.com/blog/clickhouse-driver-1_0_0-official-dotnet-client),
  [repo](https://github.com/clickhouse/clickhouse-cs)). It is ClickHouse Inc's adoption and
  evolution of the community `ClickHouse.Client` (DarkWanderer) — **the exact driver
  BreakfastProvider uses today**: ADO.NET, HTTP transport, plus first-party OpenTelemetry
  integration and a semver-stable API.
- **Octonica.ClickHouseClient remains a community/third-party client.** Its genuine advantages:
  native TCP protocol (faster for bulk workloads), fully typed columnar API, strict type
  handling. Its disadvantages for this codebase: no ActivitySource/OpenTelemetry emission
  (VERIFIED — see §1.5), community maintenance cadence, and it obsoletes the in-memory
  emulator's HTTP front-end (the dominant cost in this plan, §4 Phase 2).

**Decision point before green-lighting:** if "modern preferred" is the goal, the officially
preferred successor is `ClickHouse.Driver`, and that migration is close to a rename (§7, Path B,
small effort, flame charts *improve*). This plan fully specifies the Octonica switch (Path A) for
the case where native TCP / the typed API is the actual goal. Both paths keep tracking parity;
they differ enormously in cost.

---

## 1. Verified facts

| # | Fact | Status |
|---|---|---|
| 1.1 | **`{name:Type}` placeholder SQL works unchanged on Octonica.** `INSERT … VALUES ({id:String}, {n:Float64})` executed and bound parameters correctly via the native driver. Consequence: none of the ~8 SQL statements in the three services change, and diagram SQL text stays byte-identical. | VERIFIED |
| 1.2 | **`connection.DataSource` returns `host:port`** (`localhost:19000`), so Kronikol diagram URIs keep their full authority: `clickhouse://localhost:19000/kitchen_analytics/order_timings`. (Port value changes 8123→9000 — an *expected*, information-preserving diff.) | VERIFIED |
| 1.3 | **`ExecuteNonQuery` returns real written-row counts natively** (single-row INSERT → 1, three-row VALUES INSERT → 3). The Kronikol 3.0.74 `Kronikol.Extensions.ClickHouse.Octonica` pairing adapter passes them through. | VERIFIED |
| 1.4 | **Kronikol-tracked Octonica commands produce same-shape notes** using the services' exact SQL: request note = SQL text, response notes = `1 rows affected` and FullRows JSON (`[{"id":"oct-x","n":3}]`), method labels `INSERT INTO kron_oct` / `SELECT FROM kron_oct`. | VERIFIED |
| 1.5 | **Octonica emits no Activity spans.** An all-sources ActivityListener over open+insert+select observed only .NET's experimental DNS/socket activities. ClickHouse.Client emits `OpenAsync`/`PostSqlQueryAsync` spans that render today as the `ClickHouse.Client` flame-chart swimlane. **This is the one real information loss** — mitigated in Phase 0. | VERIFIED |
| 1.6 | The Kronikol pairing package `Kronikol.Extensions.ClickHouse.Octonica` (3.0.74) exists with `WithOctonicaClickHouseTestTracking` / `AddOctonicaClickHouseTestTracking`; type-name connection detection matches both drivers. | VERIFIED |
| 1.7 | Local-kind (`DateTime.Now`) parameters are accepted on INSERT. Read-back timezone semantics are a separate question (§5.2). | VERIFIED (write side only) |
| 1.8 | `docker/docker-compose-database.yml` already maps both `8123` and `9000` for the ClickHouse container. | VERIFIED |
| 1.9 | A lightweight `DELETE FROM` over HTTP hung for 120 s against a *vanilla* 25.8 container in the probe (sync-setting dependent; the project's compose config evidently avoids this since the Docker lane passes). Behaviour under the native protocol: | TO-VERIFY (V1) |
| 1.10 | Where Octonica derives non-query counts (client-side row counting vs server `Progress` packets). Decides whether the native emulator front-end must implement `Progress` (§4 Phase 2 amendment ii). | TO-VERIFY (V2) |

---

## 2. Tracking-parity matrix

The requirement, channel by channel. "Same" means same *kind and amount* of information; values
that legitimately change (port numbers, GUIDs) are called out.

| Channel | Today (ClickHouse.Client + `.Client` pairing) | After (Octonica + `.Octonica` pairing) | Parity |
|---|---|---|---|
| Arrow method label | `INSERT INTO order_timings` etc. (classifier over SQL text) | identical — SQL text unchanged (1.1) | ✅ |
| Request URI | `clickhouse://host:8123/kitchen_analytics/<table>` | `clickhouse://host:9000/…` (1.2) | ✅ (port value differs, expected) |
| Request note (SQL text) | full SQL with `{name:Type}` placeholders | byte-identical (1.1) | ✅ |
| Parameters (`LogParameters`) | `name=value` list | identical (same `DbParameterCollection` path) | ✅ |
| INSERT response | `1 rows affected` (via `QueryStats` adapter) | `1 rows affected` (native count, 1.3/1.4) | ✅ |
| DELETE response | count from `QueryStats.WrittenRows` | native count — value | ⚠️ TO-VERIFY (V1/V2) |
| SELECT response (FullRows) | JSON row preview | identical shape (1.4); numeric/DateTime formatting spot-checked in the Phase 3 gate | ✅ |
| Scalar (health `SELECT 1`) | `1` | `1` | ✅ |
| Flame chart | `ClickHouse.Client` swimlane with `OpenAsync` / `PostSqlQueryAsync` spans | **nothing** from the driver (1.5) | ❌ → Phase 0 mitigation mandatory |
| OTLP export | derived from Kronikol capture logs | unchanged | ✅ |

---

## 3. Blast-radius inventory

**src (small):**
- `Data/ClickHouse/ClickHouseConnectionFactory.cs` — one line: `new Octonica.ClickHouseClient.ClickHouseConnection(connectionString)`.
- `BreakfastProvider.Api.csproj` — swap `ClickHouse.Client` → `Octonica.ClickHouseClient`.
- `Configuration/ClickHouseConfig.cs` — doc-comment example string (port 9000; Octonica keys `Host/Port/User/Password/Database` are compatible with the current string shape).
- `appsettings.json` ClickHouseConfig connection string.
- Untouched: all three services' SQL (1.1), `ClickHouseCommandExtensions` (pure `DbCommand`), `ClickHouseHealthCheck`, `NoOpClickHouseConnectionFactory`.

**tests (moderate):**
- `Tests.Component.Shared.csproj` — `Kronikol.Extensions.ClickHouse.Client` → `Kronikol.Extensions.ClickHouse.Octonica`; `ClickHouse.Client` package reference retained only by the emulator's HTTP front-end.
- `ServiceCollectionExtensions.NewClickHouseTrackingOptions` — `DriverAdapter = OctonicaClickHouseDriverAdapter.Instance`.
- `UseTrackedClickHouse` — construct the Octonica connection.
- `UseInMemoryClickHouse` — route to the native front-end (Phase 2).
- Unit pin inversion: `InMemoryClickHouseServerEndToEndTests` pins *"the driver reports 0 rows affected, as it does against a real server"* — under Octonica the driver reports **real** counts, so the pin flips (and the native emulator must make that true, V2).

**config/CI (mechanical):**
- `.github/workflows/_tests.yml` and `_tests-tunit.yml`: `ClickHouseConfig__ConnectionString=Host=localhost;Port=8123;…` → `Port=9000`.
- `docker/docker-compose-sut.yml`: `Port=8123` → `9000` (container-internal; both ports already exposed, 1.8).
- `appsettings.componenttests.json` × 7 (ClickHouseConfig blocks).
- Post-deployment test configuration if it carries a ClickHouse endpoint.

**emulator (dominant):** the in-memory lane's emulator speaks ClickHouse's **HTTP** protocol via an
injected `HttpMessageHandler`; a native-TCP driver cannot use it. The remedy is already designed:
**`CLICKHOUSE_INMEMORY_PLAN.md` Part 3** (Phases 7–8) specifies the native front-end over the same
DuckDB core, with the protocol pre-reflected from Octonica 3.1.10 internals (message codes,
handshake at pinned revision 54423, public column writers making the codec "nearly a library
call", `Compress=false` so no LZ4, and the INSERT server-first schema handshake flagged as the
sharpest edge). Part 3's §9.3 scoping note assumed BreakfastProvider stays on ClickHouse.Client —
this plan **inverts that**: BreakfastProvider becomes the end-to-end consumer, so the Phase 8.2
three-way parity diff runs in this repo, not a hypothetical extracted one.

---

## 4. Phases

### Phase 0 — Flame-chart parity (Kronikol prerequisite; do this first)

Without mitigation, every report loses the ClickHouse driver swimlane (1.5). Options:

- **(a) RECOMMENDED — Kronikol emits the spans.** Add opt-in Activity emission to the wrapping
  extensions (`TrackingClickHouseCommand` starts an Activity per execution, named by the
  classifier label, source e.g. `Kronikol.ClickHouse`). Benefits: driver-independent (works for
  Sqlite/Npgsql wrappers too, none of which have driver spans), uniform naming, and the swimlane
  becomes *richer* than today (per-statement names instead of `PostSqlQueryAsync`). Small
  TDD'd Kronikol release (3.0.7x) before the switch.
- (b) App-level `DbCommand` decorator in `src` emitting activities — duplicates Kronikol's
  wrapper for one repo; rejected.
- (c) Accept the loss — violates the parity requirement; rejected.

**Gate:** a Docker-lane report on the *current* driver shows the new Kronikol swimlane alongside
`ClickHouse.Client`'s own; then the switch may remove the latter without net loss.

### Phase 1 — Docker + external lanes switch

1. Resolve **V1/V2** with a live probe against the project's own compose config (DELETE counts,
   `lightweight_deletes_sync` behaviour over native TCP).
2. Apply the §3 src + config + pairing changes.
3. Green: all six component frameworks in Docker mode; post-deployment tests.
4. **Gate:** decode the ReqNRoll Docker report's `puml-N` diagram map and diff every ClickHouse
   arrow/note against the published 3.0.74 baseline. Allowed diffs *only*: URI port, flame
   swimlane provenance (Phase 0), DELETE count value if V1 resolves to a different-but-correct
   number.

### Phase 2 — In-memory lane: execute CLICKHOUSE_INMEMORY_PLAN Part 3

Follow Part 3 as written (Phase 7 `InMemoryClickHouseNativeServer` with its 7-step TDD ladder,
Phase 8 parity, 8.3 `ClickHouseDriver` enum wiring — default flips to `Native` here), with two
amendments from this investigation:

- **(i) Non-query counts:** if V2 shows counts come from server `Progress` packets, Part 3's
  "send neither `Progress` nor `ProfileInfo` until a test proves Octonica stalls" is amended:
  the server MUST send `Progress` with written-rows for count parity (the flipped §3 unit pin is
  the red test).
- **(ii) Parameter transport at revision 54423:** the pinned Hello revision predates
  `MinRevisionWithParameters` (54459). 1.1 proves `{name:Type}` works against a *real* 25.8
  server at Octonica's negotiated revision; TDD step 5 must prove the same against the emulator's
  54423 Hello — if Octonica switches parameter strategy by negotiated revision, either advertise
  54459 and implement the parameter block, or pin whatever client-side binding it falls back to.

**Gate:** all six frameworks green in-memory with `ClickHouseDriver=Native`; the HTTP front-end
stays compilable and unit-covered (it is the extracted emulator package's other consumer surface).

### Phase 3 — The "same amount of information" proof

The three-way diff Part 3 Phase 8.2 promised, run for the three ClickHouse features (order
timings, equipment readings **including the DELETE**, service times):

1. In-memory (native) vs Docker (Octonica) vs the published 3.0.74 baseline (ClickHouse.Client).
2. Per-arrow comparison of: method label, URI shape, SQL note, parameters, response note
   (rows-affected value / FullRows keys and value formatting — Float64, DateTime rendering),
   flame swimlanes.
3. Spot-check DateTime round-trip (§5.2) and `count()`/UInt64 read-back through the services'
   `Convert.ToInt32(reader["timing_count"])` shape.

### Phase 4 — Cleanup and docs

Remove `ClickHouse.Client` from `src`; README (driver + emulator description), plan files'
execution records, `CLICKHOUSE_FEATURE_PLAN`/`CLICKHOUSE_INMEMORY_PLAN` cross-notes; decide the
HTTP front-end's future (keep: it is the extracted package's value for ClickHouse.Client/
ClickHouse.Driver users).

---

## 5. Risks

1. **Native protocol emulator effort** — the bulk of the plan, but bounded and pre-specified
   (Part 3's reflection work: 7 message codes, one fixed Hello layout, Octonica's public column
   writers, no compression). The INSERT server-first handshake and the V2/amendment-ii unknowns
   are where schedule risk lives.
2. **DateTime semantics** — Octonica is stricter/timezone-aware; `recorded_at`/`served_at`
   read-back could shift vs the HTTP driver. Phase 3 spot-check; if it shifts, normalize in the
   services (explicit UTC) *before* switching so both drivers agree.
3. **DELETE behaviour** (V1) — count value and `lightweight_deletes_sync` interaction over native
   TCP; a vanilla-container probe already showed the HTTP path can block on server settings.
4. **Maintenance posture** — Octonica is community-maintained while ClickHouse now ships an
   official client; if upstream stalls, this repo owns a native-protocol emulator front-end for a
   third-party driver. Weigh against Path B before green-lighting.

## 6. Effort

| Phase | Size |
|---|---|
| 0 — Kronikol activity emission | S–M (one Kronikol patch release) |
| 1 — Docker/external switch | S |
| 2 — Native emulator front-end | **L** (dominant; Part 3 pre-spec'd) |
| 3 — Parity gates | M |
| 4 — Cleanup | S |

## 7. Path B — the official `ClickHouse.Driver` instead (surfaced by this investigation)

Near-drop-in alternative satisfying "modern preferred" at a fraction of the cost: same
DarkWanderer lineage and HTTP protocol (the emulator's HTTP front-end keeps working), ADO.NET
surface, first-party OpenTelemetry spans (**flame chart improves rather than regresses**).
Work: package/namespace rename in src + a small `Kronikol.Extensions.ClickHouse.Driver` pairing
package mirroring `.Client`'s `QueryStats` adapter (TO-VERIFY V3: confirm the official client
still exposes `QueryStats`/written-rows and the `{name:Type}` binding — expected, same lineage),
config untouched (still port 8123), emulator untouched or near-touched. Total: S–M.

## 8. Consolidated TO-VERIFY list

- **V1** — DELETE rows-affected value + lightweight-delete sync behaviour, Octonica vs today, under the project's compose config.
- **V2** — Source of Octonica's non-query counts (client-side vs `Progress` packets); drives emulator `Progress` support.
- **V3** — (Path B only) `ClickHouse.Driver` QueryStats/parameter-binding compatibility.
- **V4** — DateTime round-trip equality between drivers for `recorded_at`/`served_at`.
- **V5** — Octonica `{name:Type}` behaviour when the negotiated revision is 54423 (emulator Hello), per Phase 2 amendment ii.

---

*Sources for §0: [ClickHouse.Driver 1.0.0 announcement](https://clickhouse.com/blog/clickhouse-driver-1_0_0-official-dotnet-client), [ClickHouse/clickhouse-cs](https://github.com/clickhouse/clickhouse-cs), [ClickHouse C# integration docs](https://clickhouse.com/docs/integrations/csharp), [Octonica/ClickHouseClient](https://github.com/Octonica/ClickHouseClient).*
