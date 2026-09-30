# gRPC and GraphQL contracts plan — published as JSON and as a UI

**Status:** proposed (2026-09-30). Nothing here is implemented. Every decision was checked against a spike run on
this checkout: a standalone app on the same package versions, then a scratch copy of the real API and its xUnit
suite (Appendix A holds the results, Appendix B reproduces them). A second, independent research pass checked the same
ground with the network switched off and against upstream sources; what it added is folded in, and its sources are
Appendix C.

**Goal:** publish BreakfastProvider's **gRPC contract** and **GraphQL contract** the way its OpenAPI and AsyncAPI
contracts are published today:

1. the running service serves each contract **as a JSON document and as a UI page**, in every environment and every
   lane (in memory, dependencies in Docker, SUT in Docker, post-deployment);
2. the component tests fetch each document from the service, check it, write it into `docs/` and attach it to the
   Kronikol report — with **the same scenarios, in the same words, in all six suites** (xUnit, NUnit, TUnit, BDDfy,
   LightBDD, ReqNRoll);
3. GitHub Pages publishes each committed document beside a UI page, linked from the landing page.

**No product feature has to be built first.** The service already offers a gRPC service and a GraphQL endpoint (§0.4).
What they lack is a published document, descriptions worth publishing, runtime discovery for gRPC tools, and tests.

**Size:** nine new scenarios per suite (one of them closes a gap in today's AsyncAPI coverage), one small `Contracts/`
folder in `src`, one Shared step class per protocol, two CI steps, and a Pages build script with a link check.

This document is self-contained. §0 orients a reader new to the repo, §1 is the target, §2 the decisions, §3 the exact
contracts, §4–§11 the phases in order, §12 the order of work and acceptance, §13 the traps, §14 the alternatives that
were rejected and why, §15 what is deliberately left out.

---

## 0. Orientation for a cold start

**What this repo is.** A reference .NET 10 API (`src/BreakfastProvider.Api`) whose point is its component-testing
approach: the same scenarios run in three CI lanes (all dependencies in-process; dependencies in Docker; everything in
Docker) from six test frameworks, and every run writes a Kronikol report with sequence diagrams. There is no
`CLAUDE.md`; conventions live in `.claude/skills/component-tests/` (start with `SKILL.md`) and
`.github/copilot-instructions.md`. Plans live at the repo root (`CLICKHOUSE_FEATURE_PLAN.md` and this file).

### 0.1 What is published today

| | OpenAPI | AsyncAPI | gRPC | GraphQL |
|---|---|---|---|---|
| **JSON served by the service** | `GET /openapi/v1.json` (`MapOpenApi`, `Program.cs:345`) | `GET /asyncapi/v1.json` (`MapAsyncApi`, `:369`) | — | — (POST introspection works only in Development, A.4.3) |
| **UI served by the service** | `GET /scalar/v1` (`MapScalarApiReference`, `:346`) | `GET /asyncapi` (`MapAsyncApiUi`, `:370`) | — | Nitro at `GET /graphql/` — on by default in HotChocolate 15, **proxied from ChilliCream's CDN** (so it fails offline, A.4.6), never tested or documented |
| **Written to `docs/` by the tests** | `docs/openapi.json` | `docs/asyncapi.json` | — | — |
| **On GitHub Pages** | `api/openapi.json` + `api/openapi.html` (Scalar) — the landing page links only the page | `api/asyncapi.json` + `api/asyncapi.html` (AsyncAPI React) — likewise | — | — |
| **Scenarios, six suites** | document + UI page | document only — **the UI page is untested** | calls only (3 features) | queries only (5 features) |

All four existing endpoints are always on: none sits behind `IsDevelopment()`.

### 0.2 Where things are

| Path | What |
|---|---|
| `src/BreakfastProvider.Api/Program.cs` | Composition root. `AddOpenApi` :108; `AddAsyncApi` :223 (→ `StartupExtensions.AddAsyncApi` :141); `AddGrpc` :244; `MapOpenApi` / `MapScalarApiReference` :345-346; `MapGrpcService<BreakfastGrpcService>` :354-362, **constrained to POST** because the gRPC package's catch-all route for unimplemented services would otherwise swallow REST routes such as `GET /inventory/{id}`; `MapGraphQL` :363; `MapAsyncApi` / `MapAsyncApiUi` :369-370. |
| `src/BreakfastProvider.Api/Protos/breakfast.proto` | The gRPC service BreakfastProvider **offers**: `breakfast.BreakfastGrpc` — `GetRecipeSummary` and `GetOrderStatus` (unary), `StreamOrderUpdates` (server streaming). `GrpcServices="Both"`. No comments. |
| `src/BreakfastProvider.Api/Protos/notifications.proto` | The downstream Notification Service's contract, which BreakfastProvider **consumes** (`GrpcServices="Client"`). Not ours to publish. |
| `src/BreakfastProvider.Api/Grpc/BreakfastGrpcService.cs` | The gRPC implementation. |
| `src/BreakfastProvider.Api/Reporting/ReportingQuery.cs`, `StartupExtensions.AddReporting` :182 | The GraphQL schema: `AddGraphQLServer().AddQueryType<ReportingQuery>()…`. Root type **`ReportingQuery`** (not `Query`), seven fields. No XML doc comments, so no descriptions. |
| `tests/BreakfastProvider.Tests.Component.Shared/Constants/Endpoints.cs` | Route constants (`Endpoints.Swagger`, `Endpoints.AsyncApi`, `Endpoints.GraphQL`). |
| `…Shared/Constants/OpenApiSpecs.cs`, `AsyncApiSpecs.cs` | The `docs/` path (`../../../../../docs/`, from `bin/Debug/net10.0`) and file names. |
| `…Shared/Infrastructure/SourceControlledDocsHelper.cs` | Teardown copy of `Reports/attachments/{openapi,asyncapi}.json` over `docs/`. |
| `…Shared/Common/Grpc/GrpcBreakfastSteps.cs` | The gRPC step class. `Initialize(factory, fetcher)` gives a Kronikol-tracked client over the TestServer; `InitializeExternal(url)` a plain channel to `ExternalGrpcUrl`. |
| `…Shared/Common/RequestContext.cs` | `RequestContext(Func<HttpClient>, requestId)`, what HTTP step classes take. |
| `tests/…{xUnit,NUnit,TUnit,BDDfy}/Scenarios/Specifications/`, `…LightBDD/Scenarios/Specifications/`, `…ReqNRoll/Features/Specifications/` + `StepDefinitions/OpenApi/OpenApiSteps.cs` | Today's specification scenarios (OpenAPI, Scalar UI, AsyncAPI). |
| `tests/…/Infrastructure/BaseFixture.cs` (five suites, e.g. xUnit :109-165), `…ReqNRoll/Support/DependencyInjectionSetup.cs` | Step-class registration (`services.AddTransient<…Steps>()`). |
| `…{xUnit,NUnit,TUnit,BDDfy}/Infrastructure/GlobalTestSetup.cs`, `…LightBDD/Infrastructure/ConfiguredLightBddScopeAttribute.cs:63-64`, `…ReqNRoll/Hooks/TestRunHooks.cs:61-62` | Where the teardown copies into `docs/` are called. |
| `.github/workflows/_tests.yml`, `_tests-tunit.yml` | One lane (a suite × memory / docker / external-sut). |
| `.github/workflows/ci-main.yml` job `deploy-pages` (:593-964) | Builds and deploys the Pages site: `site/api/` at :743-789, the landing page at :791-955 ("API Specifications" cards at :920-932). |
| `docker/docker-compose-sut.yml` :46-50 | The SUT in Docker has two Kestrel endpoints: `:8080` HTTP/1.1 (→ `localhost:5080`) and `:8081` HTTP/2 without TLS (→ `localhost:5081`, `ExternalGrpcUrl`). |

### 0.3 How a contract travels today, end to end

1. The service generates the document at request time.
2. A Specifications scenario GETs it, checks a few properties, writes the body to `docs/<name>` (**writer A**:
   `File.WriteAllTextAsync(path, body, Encoding.UTF8)`, which writes a BOM) and attaches the file to the report
   (`Track.Attachment`; LightBDD `StepExecution.Current.AttachFile`; ReqNRoll `outputHelper.AddAttachment`). Kronikol
   copies it to `Reports/attachments/<name>`.
3. At teardown `SourceControlledDocsHelper.CopyApiSpecificationFilesToDocsFolder()` copies
   `Reports/attachments/<name>` back over `docs/<name>` (**writer B**: CRLF → LF, no BOM). Every suite does this in
   every lane, so the last writer wins.
4. A developer commits `docs/` by hand. **Nothing in CI compares the committed files with what the service serves**
   (the only comparison, for `Specifications.yml` in `_tests.yml` :409-434, computes `matching` and nothing reads it).
5. `deploy-pages` copies the **committed** `docs/openapi.json` and `docs/asyncapi.json` to `site/api/`, downloads the
   Scalar and AsyncAPI renderers unpinned with `curl -sL` (no `-f`, so a failed download publishes a broken page),
   writes two HTML shells and the landing page, and deploys https://lemonlion.github.io/BreakfastProvider/. The two
   JSON documents are on the site, but no page links to them: a reader who wants the JSON has to guess its URL.

### 0.4 Nothing needs building first

The request allowed for building a feature first wherever no contract could exist yet. Neither protocol needs one:

- **gRPC.** `breakfast.BreakfastGrpc` is live (`Program.cs:354`), exercised by three features in every suite, and
  reachable in the external-SUT lane on the HTTP/2 endpoint.
- **GraphQL.** HotChocolate 15.1.15 serves `/graphql` with seven root fields, exercised by five reporting features.

What is missing is (a) a published document, (b) descriptions — the proto has no comments and `ReportingQuery` has no
XML docs, so a published contract today would be bare names — (c) runtime discovery for gRPC tools (there is no
server reflection), and (d) tests.

### 0.5 Baseline

This checkout, Linux, .NET SDK 10.0.401, in memory:

| Suite | Result |
|---|---|
| xUnit | 203/203 (21 s) |
| NUnit | 203/203 (24 s) |
| TUnit | 203/203 (25 s) |
| BDDfy | 203/203 (23 s) |
| LightBDD | 178/178 (15 s) — a tabular scenario counts once |
| ReqNRoll | 205/205 (16 s) |

Every suite leaves `docs/openapi.json` changed by one line (A.3) — the cross-OS drift Phase 0 removes — and
`docs/Specifications.yml` rewritten in its own YAML dialect.

---

## 1. The target

| | gRPC | GraphQL |
|---|---|---|
| **JSON served** | `GET /grpc/v1.json` — the protobuf `FileDescriptorSet` of `breakfast.proto` in protobuf's JSON mapping, comments included | `GET /graphql/schema.json` — the response to the standard introspection query |
| **Also served** | `GET /grpc/protos/breakfast.proto` (the source contract); gRPC **server reflection** v1 and v1alpha on the gRPC endpoint | `GET /graphql/schema.graphql` (SDL; HotChocolate's own route, made explicit) |
| **UI served** | `GET /grpc/` — a documentation page the service renders from the same descriptor set (`/grpc` redirects to it) | `GET /graphql/` — Nitro, HotChocolate's IDE, made explicit and served from the package instead of proxied from ChilliCream's CDN |
| **`docs/`** | `docs/grpc.json`, `docs/grpc.html` | `docs/graphql.json`, `docs/schema.graphql` |
| **Pages** | `api/grpc/` (the page), `api/grpc.json`, `api/grpc/protos/breakfast.proto` | `api/graphql.html` (GraphQL Voyager over `graphql.json`), `api/graphql.json`, `api/schema.graphql` |
| **Scenarios per suite** | 5: contract document, proto file, UI page, reflection lists the service, reflection describes it | 3: schema document, schema definition, UI page |

On Pages every contract's JSON — OpenAPI and AsyncAPI included — is one click from the landing page and one click from
its own UI page (D13).

With Phase 0's AsyncAPI UI scenario that makes **9 new scenarios per suite**. Every suite's total rises by exactly 9:
xUnit, NUnit, TUnit and BDDfy 203 → 212, LightBDD 178 → 187, ReqNRoll 205 → 214.

---

## 2. Decisions

Each is backed by a verified fact in Appendix A.

**D1. The gRPC JSON document is a `FileDescriptorSet` in protobuf's canonical JSON mapping.** gRPC has no
OpenAPI/AsyncAPI-style JSON standard; the descriptor set is the standard machine-readable form of a proto contract —
the message `protoc --descriptor_set_out` writes and grpcurl's `-protoset` reads, both in binary. Its JSON form is
protobuf's own JSON mapping of that message, so any protobuf library parses it back. It is
produced **by protoc at build time** with `--include_source_info`, so the proto's comments are in it; the runtime
`BreakfastReflection.Descriptor` has had them stripped (A.5.1). It is embedded in the assembly, trimmed to the source
locations that carry a comment (73 → 4 locations, 20 KB → 5.5 KB, A.5.3) and serialised with `Google.Protobuf`'s
`JsonFormatter`, indented. It is byte-identical across restarts and environments (A.5.4) and parses back into a
`FileDescriptorSet` with `JsonParser`, which rejects unknown fields (A.5.10) — so the tests can check that the
document is a valid descriptor set, not just valid JSON.

**D2. The `.proto` is published too**, at `GET /grpc/protos/breakfast.proto`, because it is what a client generates
code from — a .NET consumer can point `dotnet grpc add-url` at it. `ProtoRoot="Protos"` makes the file `breakfast.proto` everywhere — descriptor, reflection and URL (A.5.5);
today it is `Protos/breakfast.proto`. `notifications.proto` is not published: BreakfastProvider consumes it.

**D3. gRPC server reflection, `grpc.reflection.v1` and `v1alpha`.** It lets grpcurl, Postman, Kreya, Insomnia and
grpcui discover and call the service from the running endpoint — the gRPC equivalent of fetching
`/openapi/v1.json`. `Grpc.AspNetCore.Server.Reflection` 2.71.0 serves only `v1alpha`; `v1` arrived in `Grpc.Reflection`
2.80.0 (A.5.6). So `Grpc.AspNetCore` moves 2.71.0 → 2.84.0 with it, which also gives the API a `net10.0` build of the
gRPC server (2.71.0 stops at `net9.0`, A.11); the test projects already use `Grpc.Net.Client` 2.84.0. All 203 existing
xUnit scenarios pass on 2.84.0 with reflection mapped (A.5.8). Reflection serves the runtime descriptors, so it carries no
comments; the JSON document does. Microsoft's gRPC tooling docs suggest mapping reflection in Development only; here it
is always on, like `/openapi/v1.json`, because it discloses nothing `/grpc/v1.json` does not already publish. An
environment that must not expose it can put `MapGrpcReflectionService()` behind a setting, at the price of S5 and S6
there.

**D4. The gRPC UI is a documentation page the service renders from the descriptor set**, at `GET /grpc/`: services,
methods and their streaming kind, messages, fields and enums, each with its comment, linking `v1.json` and
`protos/breakfast.proto`. It is static HTML with no script and no external request, so the same bytes are served
in-app, committed as `docs/grpc.html` and published on Pages, and all six suites can assert **what the page says**,
not just that it is HTML. Like the AsyncAPI UI it documents rather than calls; calling is what reflection (D3) opens
to grpcurl, Postman and Kreya. The alternatives (GrpcBrowser, Kaya.GrpcExplorer, grpcui, Scalar, JSON transcoding +
OpenAPI, protoc-gen-doc, buf) are rejected in §14, each with evidence; grpcui stays on offer as a local Docker tool
(§15).

**D5. The GraphQL JSON document is the standard introspection response**, served at `GET /graphql/schema.json` by
running the introspection query **inside** the service with `AllowIntrospection()` on that one request. HotChocolate 15
refuses client introspection outside Development (`HC0046`, A.4.3), so a client-side POST would publish nothing from a
production deployment. The server-side request works in every environment (A.4.4) and leaves HotChocolate's secure
default alone. The query is graphql-js's `getIntrospectionQuery` with descriptions, `specifiedByURL`,
`isRepeatable`, schema description and input-value deprecation — all accepted by HotChocolate 15.1.15 (A.4.4). The
body is the whole response, `{"data":{"__schema":…}}`, exactly what a client running the query itself would receive.
GraphQL Voyager reads it as is (A.9); graphql-js's `buildClientSchema` takes its `data`.

**D6. The GraphQL SDL is HotChocolate's own `GET /graphql/schema.graphql`** (also `?sdl`), served in every
environment (A.4.2) and made explicit with `EnableSchemaRequests = true`. It is the form a reviewer reads in a diff:
`docs/schema.graphql`.

**D7. The GraphQL UI is Nitro, served from the package's embedded files.** HotChocolate 15 already serves Nitro at
`/graphql/`, but by default (`ServeMode = Latest`) the service **reverse-proxies it from ChilliCream's CDN** on every
request: the responses carry `Server: cloudflare` and `cf-ray`, and with no network `GET /graphql/` is a `502` — in the
in-memory lane too (A.4.6). That breaks the repo's first rule, that tests just work on pull and build with no
internet. `ServeMode = GraphQLToolServeMode.Embedded` serves the copy inside `ChilliCream.Nitro.App` instead: `200` with
no network, assets from the service (A.4.6). It works in Production too, because it loads the schema from the SDL route
rather than by introspection (A.4.6). The plan makes all of this explicit (`Tool.Enable = true`, embedded), tests it —
including that the page no longer comes from the CDN — and documents it. Two facts go into the README: Nitro is under the
ChilliCream License 1.0 (source-available, not OSI), and the page calls `https://api.chillicream.com/status` from the
browser even with `DisableTelemetry = true` (A.4.7).

**D8. Descriptions are part of the contract.** Phase 1 comments `breakfast.proto` (the service, every rpc, message and
field). Phase 4 adds XML doc comments to `ReportingQuery` and the reporting types, which HotChocolate turns into GraphQL
descriptions (A.4.8) — the XML comments already feed the OpenAPI summaries. The scenarios hold the line: every rpc and
every root query field must carry a description.

**D9. The Pages UIs.** gRPC: `docs/grpc.html` as is — no renderer needed. GraphQL: GraphQL Voyager 2.1.0's standalone
bundle over `graphql.json` (verified to render HotChocolate's introspection response, A.9), downloaded and pinned like
the Scalar and AsyncAPI renderers. Nitro cannot be the Pages UI: it needs a live endpoint.

**D10. The contract endpoints are always on**, like OpenAPI and AsyncAPI, and **left out of the OpenAPI document**
(`.ExcludeFromDescription()`): without it the new GETs appear in `docs/openapi.json` (A.5.9). They are mapped with
`Map…` extension methods in `src/BreakfastProvider.Api/Contracts/`, the way `MapAsyncApiUi` and `MapGraphQL` are — not
as MVC controllers, despite the controllers convention, because they are documentation infrastructure, not API.

**D11. One writer, identical bytes.** A Shared `ContractDocs.WriteAsync` writes every contract file as UTF-8 without a
BOM, with LF line ends and with any `\r\n` escape inside a JSON string folded to `\n`; the teardown copy uses the same
normalisation and the same file list. All six suites then write byte-identical files on every OS, which D12 checks.

**D12. CI checks what Pages publishes.** Each in-memory lane, in all six suites, fails when a contract file in `docs/`
differs from what the service now serves, is missing, or is untracked. That makes "identical output from six suites"
a checked property and means Pages never publishes a stale contract. The docker and external-SUT lanes are not gated: the
OpenAPI document's `servers` entry names the host it was fetched from, `http://localhost/` under the TestServer (the
committed value) but the published port against the external SUT.

**D13. On Pages, every document is one click away.** Publishing a JSON document only helps if a reader can get to it.
Today the landing page links the OpenAPI and AsyncAPI pages but not their JSON. The landing page's specification cards
will carry two kinds of link, the UI page and the documents behind it, and every UI page will open with a bar linking
its documents. Each contract's JSON lives at a predictable URL, `api/<contract>.json`, and opens in the browser. A link
check fails the Pages build when any link on the site points at a missing file. The prototype site was checked in
headless Chromium (A.12).

---

## 3. The contracts, exactly

### 3.1 gRPC

| Request | Response |
|---|---|
| `GET /grpc/v1.json` | `200`, `application/json; charset=utf-8`. The descriptor set, indented two spaces, LF, no BOM. |
| `GET /grpc/protos/breakfast.proto` | `200`, `text/plain; charset=utf-8`. The embedded source file, line ends normalised to LF. |
| `GET /grpc/` | `200`, `text/html; charset=utf-8`. The documentation page (§7.2). |
| `GET /grpc` | `301` to `/grpc/`, so the page's relative links resolve (one `MapGet` serves both, A.5.11). |
| gRPC `grpc.reflection.v1.ServerReflection/ServerReflectionInfo` (and `v1alpha`) | On the gRPC endpoint: the TestServer in memory; `:8081` h2c in Docker (`localhost:5081`); under `dotnet run`, TLS `:7270` with the `https` launch profile (not exercised in the spike: no development certificate in the container). |

The document's shape (abridged from the real API in the spike, A.5.8):

```json
{
  "file": [
    {
      "name": "breakfast.proto",
      "package": "breakfast",
      "messageType": [
        {
          "name": "RecipeSummaryRequest",
          "field": [
            {
              "name": "recipe_type",
              "number": 1,
              "label": "LABEL_OPTIONAL",
              "type": "TYPE_STRING",
              "jsonName": "recipeType"
            }
          ]
        }
      ],
      "service": [
        {
          "name": "BreakfastGrpc",
          "method": [
            { "name": "GetRecipeSummary", "inputType": ".breakfast.RecipeSummaryRequest", "outputType": ".breakfast.RecipeSummaryReply" },
            { "name": "GetOrderStatus", "inputType": ".breakfast.OrderStatusRequest", "outputType": ".breakfast.OrderStatusReply" },
            { "name": "StreamOrderUpdates", "inputType": ".breakfast.OrderStatusRequest", "outputType": ".breakfast.OrderStatusReply", "serverStreaming": true }
          ]
        }
      ],
      "options": { "csharpNamespace": "BreakfastProvider.Api.Grpc" },
      "sourceCodeInfo": {
        "location": [
          { "path": [ 6, 0 ], "span": [ 7, 0, 14, 1 ], "leadingComments": " Breakfast Provider's gRPC API: …\n" }
        ]
      },
      "syntax": "proto3"
    }
  ]
}
```

A `sourceCodeInfo` path addresses the element it documents: `[6, s]` is service *s*, `[6, s, 2, m]` its method *m*,
`[4, n]` message *n*, `[4, n, 2, f]` its field *f*, `[5, e]` enum *e* (field numbers of `FileDescriptorProto`,
`ServiceDescriptorProto` and `DescriptorProto` in `descriptor.proto`). The page (§7) and the scenarios (§9) use them.

### 3.2 GraphQL

| Request | Response |
|---|---|
| `GET /graphql/schema.json` | `200`, `application/json; charset=utf-8`. `{"data":{"__schema":{"description":null,"queryType":{"name":"ReportingQuery"},…}}}` — 123,283 bytes today, before descriptions. |
| `GET /graphql/schema.graphql` (and `?sdl`, `/graphql/schema`) | `200`, `application/graphql; charset=utf-8`, `Content-Disposition: attachment; filename="schema.graphql"`, `Cache-Control: public, max-age=3600`, `ETag`. Begins `schema {\n  query: ReportingQuery\n}`. |
| `GET /graphql/` | `200`, `text/html`. Nitro from the package's embedded files (`<title>Nitro</title>`, scripts under `/graphql/static/js/`), served by the service with no network. Today's default instead proxies ChilliCream's CDN (`<title>Nitro IDE</title>`, `/graphql/assets/…`, `Server: cloudflare`). |
| `GET /graphql` | `301` to `/graphql/` (HotChocolate). |
| `POST /graphql` with an introspection query | Unchanged: answered in Development, `400 HC0046` elsewhere (HotChocolate's default). |

---

## 4. Phase 0 — Put right what the research found

These land first so the new work starts from green, deterministic ground. One commit, all six suites green.

**0.1 Contract files come out byte-identical from every suite on every OS.** Today writer A writes a BOM and writer B
strips it; the committed `docs/openapi.json` carries a `\r\n` escape inside the `/webhooks/eventgrid` summary because it
was generated from a Windows checkout, so every Linux run changes it (A.3). Add
`tests/BreakfastProvider.Tests.Component.Shared/Util/ContractDocs.cs`:

```csharp
namespace BreakfastProvider.Tests.Component.Shared.Util;

/// <summary>
/// The contract documents the tests write into docs/ and the Pages site publishes. Every suite writes the same
/// bytes on every OS: UTF-8 without a BOM, LF line ends, and a CRLF that an XML doc comment carried into a JSON
/// string from a Windows checkout folded to LF.
/// </summary>
public static class ContractDocs
{
    public const string FolderPath = "../../../../../docs/";

    public static readonly IReadOnlyList<string> FileNames =
        [OpenApiSpecs.JsonFileName, AsyncApiSpecs.JsonFileName,
         GrpcSpecs.JsonFileName, GrpcSpecs.HtmlFileName,
         GraphQLSpecs.JsonFileName, GraphQLSpecs.SdlFileName];

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public static string Normalise(string content) =>
        content.Replace("\r\n", "\n").Replace(@"\r\n", @"\n");

    /// <summary>Writes the document into docs/ and returns the full path, for the suite to attach.</summary>
    public static async Task<string> WriteAsync(string fileName, string content)
    {
        var path = Path.GetFullPath($"{FolderPath}{fileName}");
        const int maxRetries = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, Normalise(content), Utf8WithoutBom);
                return path;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }
}
```

The twelve existing OpenAPI and AsyncAPI write-to-disk steps (two per suite) call it instead of their own retry loops;
`SourceControlledDocsHelper.CopyApiSpecificationFilesToDocsFolder()` loops over `ContractDocs.FileNames` with the same
`Normalise` and encoding. `GrpcSpecs` and `GraphQLSpecs` (§9.2) arrive now with their file names: the copy already
skips a file the run did not produce, so listing the four new files before they exist is harmless. `OpenApiSpecs` and
`AsyncApiSpecs` keep their file names; their `SpecificationsFolderPath` gives way to `ContractDocs.FolderPath`.
Regenerate `docs/openapi.json` (the one-line change). `.gitattributes` gains `docs/*.html eol=lf`,
`docs/*.graphql eol=lf` and `*.proto text eol=lf` (protoc copies comments verbatim, so a CRLF checkout would put `\r`
into the descriptor). The `\r\n`-escape fold is safe for these documents: it could only misfire on a JSON string
holding a literal backslash-r-backslash-n, which none does.

**0.2 ReqNRoll's shared "the response should be valid" passes when nothing was requested.** In
`StepDefinitions/OpenApi/OpenApiSteps.cs` it is `if (_swaggerResponse != null) … else if (_asyncApiResponse != null) …`
with no `else`. Replace the per-document fields with a scenario-scoped `SpecificationDocumentContext` (injected by
ReqNRoll's context injection into every specification binding) that each "When the … endpoint is called" fills and the
shared Then asserts on — failing when nothing was fetched. The new contract features reuse that Then.

**0.3 Stale text.**
- `ReqNRoll/Features/Specifications/SpecificationsOpenApi.feature:2` says `/swagger/v1/swagger.json` → `/openapi/v1.json`;
  `SpecificationsAsyncApi.feature:2` says `/asyncapi/asyncapi.json` → `/asyncapi/v1.json`. Rebuild to regenerate the
  committed `.feature.cs`.
- `src/BreakfastProvider.Api/Properties/launchSettings.json` `"launchUrl": "swagger"` (both profiles) opens a 404 →
  `"scalar/v1"`.
- `README.md`: "Swagger UI: `http://localhost:5239/swagger`" and "Swagger/OpenAPI available at `/swagger` in
  Development" (it is `/scalar/v1`, always on); the "Specifications & Documentation" table names
  `ComponentSpecifications.yml`, which is `docs/Specifications.yml`.
- `.github/copilot-instructions.md` "Swagger/OpenAPI via Swashbuckle" → Microsoft.AspNetCore.OpenApi + Scalar.

**0.4 The AsyncAPI UI has no scenario in any suite.** Add S1 (§9) to all six. It passes on first run — a
characterisation test for a page that already works — so its red step is to point it at a wrong route and see it fail.

**0.5 `Microsoft.AspNetCore.OpenApi` 10.0.7 pulls `Microsoft.OpenApi` 2.0.0**, which `dotnet restore` flags as a high
severity vulnerability (NU1903, GHSA-v5pm-xwqc-g5wc, CVE-2026-49451; every `Microsoft.OpenApi` up to 2.7.4 is
affected). 10.0.11 requires `Microsoft.OpenApi` 2.7.5 or later, 10.0.12 `[2.12.0, 3.0.0)` (A.10). Bump it, regenerate `docs/openapi.json` and review the diff in the same commit — the newer serialiser may change
the document. This item is separable if its diff turns out large.

**0.6 `post-deployment-tests.yml` runs `tests/BreakfastProvider.Tests.Component/…`**, a project that no longer exists
(it became six suites). Point its three paths at `BreakfastProvider.Tests.Component.LightBDD` — `_tests.yml` already
treats LightBDD as the default report. It runs on self-hosted runners, so the commit must say it was not exercised.

**Found and not fixed here (§15):** `Bielu.AspNetCore.AsyncApi.UI` is deprecated (`[Obsolete]`: "render the AsyncAPI
document with Scalar instead"; Scalar.AspNetCore 2.16.1 and later can, 2.14.4 cannot, A.7) and CI's `-p:WarningLevel=0` hides the
CS0618; `docs/Specifications.yml` comes out in a different YAML dialect from each suite, so its content depends on which
suite ran last.

---

## 5. Phase 1 — The gRPC contract as a document and a proto file (S2, S3)

**Red first.** Add S2 and S3 (§9) to all six suites; both fail with `404`.

**1.1 Comment `breakfast.proto`.** Comments only: no name, number or type changes, so the wire contract is untouched.
Every comment must say what the code does today — including that the recipe figures are fixed sample values and that
the stream sends one message:

```proto
syntax = "proto3";

option csharp_namespace = "BreakfastProvider.Api.Grpc";

package breakfast;

// Breakfast Provider's gRPC API: recipe summaries and order status for kitchen systems.
service BreakfastGrpc {
  // Summarises a recipe type. Pancakes and Waffles return fixed sample figures;
  // any other recipe type returns zero batches and no ingredients.
  rpc GetRecipeSummary (RecipeSummaryRequest) returns (RecipeSummaryReply);

  // Returns an order's current status. NOT_FOUND when no order has the id.
  rpc GetOrderStatus (OrderStatusRequest) returns (OrderStatusReply);

  // Streams an order's status: today one message, the current status, then the stream completes.
  // NOT_FOUND when no order has the id.
  rpc StreamOrderUpdates (OrderStatusRequest) returns (stream OrderStatusReply);
}

// Which recipe type to summarise.
message RecipeSummaryRequest {
  // The recipe type, e.g. "Pancakes" or "Waffles".
  string recipe_type = 1;
}

// A recipe type's summary.
message RecipeSummaryReply {
  // The recipe type that was asked for.
  string recipe_type = 1;
  // How many batches have been prepared.
  int32 total_batches = 2;
  // The ingredients the recipe is usually made with.
  repeated string common_ingredients = 3;
  // When the summary was produced, ISO 8601 round-trip format.
  string last_prepared_at = 4;
}

// Which order to look up.
message OrderStatusRequest {
  // The order's id, as returned by POST /orders.
  string order_id = 1;
}

// An order's current status.
message OrderStatusReply {
  // The order's id.
  string order_id = 1;
  // Created, Preparing, Ready, Completed or Cancelled.
  string status = 2;
  // The customer the order is for.
  string customer_name = 3;
  // How many items the order has.
  int32 item_count = 4;
  // When the order was created, ISO 8601 round-trip format.
  string created_at = 5;
}
```

**1.2 `BreakfastProvider.Api.csproj`.** Give `breakfast.proto` a proto root and ask protoc for the descriptor set; embed
it and the proto (verified in a clean build and after an incremental edit, A.5.2):

```xml
<ItemGroup>
  <Protobuf Include="Protos\breakfast.proto" GrpcServices="Both" ProtoRoot="Protos"
            AdditionalProtocArguments="--include_source_info;--descriptor_set_out=$(IntermediateOutputPath)breakfast.protoset" />
  <Protobuf Include="Protos\notifications.proto" GrpcServices="Client" />
</ItemGroup>

<ItemGroup>
  <!-- The source contract, served at /grpc/protos/breakfast.proto. -->
  <EmbeddedResource Include="Protos\breakfast.proto" LogicalName="Contracts.breakfast.proto" />
</ItemGroup>

<!-- protoc's descriptor set, comments included, is the published gRPC contract (/grpc/v1.json, /grpc/).
     It only exists once Protobuf_Compile has run, which is after PrepareResources would collect it. -->
<Target Name="EmbedGrpcContract" BeforeTargets="PrepareResources" DependsOnTargets="Protobuf_Compile">
  <ItemGroup>
    <EmbeddedResource Include="$(IntermediateOutputPath)breakfast.protoset" LogicalName="Contracts.breakfast.protoset"
                      WithCulture="false" Type="Non-Resx" />
  </ItemGroup>
</Target>
```

The Docker build (`dotnet build` inside the SDK image, `.dockerignore` keeps `Protos/`) runs the same targets.

**1.3 `src/BreakfastProvider.Api/Contracts/GrpcContract.cs`.** Loads the embedded set once, trims the source info, and
exposes the three published forms (`Html` and the `GrpcContractPage` it calls arrive in Phase 3):

```csharp
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace BreakfastProvider.Api.Contracts;

/// <summary>
/// The gRPC contract this service offers: the descriptor set protoc wrote for breakfast.proto at build time, comments
/// included, and the proto file itself. Both are embedded by the EmbedGrpcContract target in the project file.
/// </summary>
public static class GrpcContract
{
    private static readonly Lazy<FileDescriptorSet> Descriptors = new(LoadDescriptorSet);
    private static readonly Lazy<string> JsonText = new(() =>
        new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation("  ")).Format(Descriptors.Value));
    private static readonly Lazy<string> ProtoText = new(() =>
        ReadResource("Contracts.breakfast.proto").ReplaceLineEndings("\n"));
    private static readonly Lazy<string> PageHtml = new(() => GrpcContractPage.Render(Descriptors.Value));

    public static string Json => JsonText.Value;
    public static string Proto => ProtoText.Value;
    public static string Html => PageHtml.Value;

    private static FileDescriptorSet LoadDescriptorSet()
    {
        using var stream = OpenResource("Contracts.breakfast.protoset");
        var set = FileDescriptorSet.Parser.ParseFrom(stream);
        foreach (var file in set.File.Where(f => f.SourceCodeInfo is not null))
        {
            // protoc records a location for every token; a reader needs only those that carry a comment.
            var commented = file.SourceCodeInfo.Location
                .Where(l => l.HasLeadingComments || l.HasTrailingComments || l.LeadingDetachedComments.Count > 0)
                .ToList();
            file.SourceCodeInfo.Location.Clear();
            file.SourceCodeInfo.Location.Add(commented);
        }
        return set;
    }

    private static Stream OpenResource(string name) =>
        typeof(GrpcContract).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"{name} is not embedded; see the EmbedGrpcContract target.");

    private static string ReadResource(string name)
    {
        using var reader = new StreamReader(OpenResource(name));
        return reader.ReadToEnd();
    }
}
```

**1.4 `src/BreakfastProvider.Api/Contracts/ContractEndpointRouteBuilderExtensions.cs`** and `Program.cs`:

```csharp
public static IEndpointRouteBuilder MapGrpcContract(this IEndpointRouteBuilder app)
{
    app.MapGet(Documentation.Contracts.GrpcJson, () => Results.Text(GrpcContract.Json, MediaTypeNames.Application.Json))
        .ExcludeFromDescription();
    app.MapGet(Documentation.Contracts.GrpcProto, () => Results.Text(GrpcContract.Proto, MediaTypeNames.Text.Plain))
        .ExcludeFromDescription();
    return app;
}
```

`Documentation.Contracts` (beside `Documentation.ServiceNames` in `StartupExtensions.cs`) holds the routes:
`GrpcJson = "/grpc/v1.json"`, `GrpcProto = "/grpc/protos/breakfast.proto"`, `GrpcUi = "/grpc"`,
`GraphQLSchemaJson = "/graphql/schema.json"`. `Program.cs` calls `app.MapGrpcContract();` right after
`MapGrpcService<…>()`. The literal routes win over the gRPC catch-all, which is POST-only anyway (A.5.9).

**1.5 Green.** Run all six suites; commit `docs/grpc.json`.

---

## 6. Phase 2 — gRPC server reflection (S5, S6)

**Red first.** Add S5 and S6 in all six suites; both fail with `StatusCode.Unimplemented`.

**2.1 Packages.** `Grpc.AspNetCore` 2.71.0 → **2.84.0**; add `Grpc.AspNetCore.Server.Reflection` **2.84.0**. Move
`fakes/Dependencies.Fakes.NotificationService` to `Grpc.AspNetCore` 2.84.0 in the same commit, so the family is one
version everywhere: the test projects reference both, so in memory the fake already resolves to 2.84.0 — which is what
the spike ran — and only its Docker image would otherwise stay on 2.71.0. The tests already use `Grpc.Net.Client`
2.84.0.

**2.2 `Program.cs`.**

```csharp
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
…
app.MapGrpcService<Grpc.BreakfastGrpcService>()
    .Add(b => b.Metadata.Add(new HttpMethodMetadata(["POST"])));
app.MapGrpcReflectionService()
    .Add(b => b.Metadata.Add(new HttpMethodMetadata(["POST"])));   // same reason as the service above
app.MapGrpcContract();
```

**2.3 Shared step class** `tests/…Shared/Common/Grpc/GrpcReflectionSteps.cs`, shaped like `GrpcBreakfastSteps`
(verified against the TestServer through Kronikol's tracking interceptor, A.5.8):

```csharp
using BreakfastProvider.Api;
using Google.Protobuf.Reflection;
using Grpc.Net.Client;
using Grpc.Reflection.V1;
using Kronikol.Extensions.Grpc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BreakfastProvider.Tests.Component.Shared.Common.Grpc;

public class GrpcReflectionSteps
{
    private ServerReflection.ServerReflectionClient? _client;

    public IReadOnlyList<string> ListedServices { get; private set; } = [];
    public IReadOnlyList<FileDescriptorProto> DescribedFiles { get; private set; } = [];

    public void Initialize<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory,
        Func<(string Name, string Id)> currentTestInfoFetcher) where TEntryPoint : class
    {
        _client = factory.CreateTestTrackingGrpcClient<TEntryPoint, ServerReflection.ServerReflectionClient>(
            new GrpcTrackingOptions
            {
                ServiceName = Documentation.ServiceNames.BreakfastProvider,
                Verbosity = GrpcTrackingVerbosity.Detailed,
                CurrentTestInfoFetcher = currentTestInfoFetcher
            });
    }

    public void InitializeExternal(string baseUrl) =>
        _client = new ServerReflection.ServerReflectionClient(GrpcChannel.ForAddress(baseUrl));

    public async Task ListServices()
    {
        var response = await Ask(new ServerReflectionRequest { ListServices = string.Empty });
        ListedServices = response.ListServicesResponse.Service.Select(s => s.Name).ToList();
    }

    public async Task DescribeSymbol(string fullyQualifiedName)
    {
        var response = await Ask(new ServerReflectionRequest { FileContainingSymbol = fullyQualifiedName });
        DescribedFiles = response.FileDescriptorResponse.FileDescriptorProto
            .Select(FileDescriptorProto.Parser.ParseFrom).ToList();
    }

    private async Task<ServerReflectionResponse> Ask(ServerReflectionRequest request)
    {
        using var call = _client!.ServerReflectionInfo();
        await call.RequestStream.WriteAsync(request);
        await call.RequestStream.CompleteAsync();
        await call.ResponseStream.MoveNext(CancellationToken.None);
        return call.ResponseStream.Current;
    }
}
```

Register it in the five `BaseFixture`s (`services.AddTransient<GrpcReflectionSteps>();`) and use it from ReqNRoll the way
`GrpcRecipeSummarySteps` uses `GrpcBreakfastSteps` (`EnsureGrpcClient`). Initialise it exactly as the existing gRPC
features do: `InitializeExternal(Settings.ExternalGrpcUrl ?? Settings.ExternalServiceUnderTestUrl!)` when
`RunAgainstExternalServiceUnderTest`, otherwise `Initialize(AppFactory, CurrentTestInfo.Fetcher)`.

In the Kronikol diagrams the call appears as one `ServerReflectionInfo` arrow without message bodies:
`Kronikol.Extensions.Grpc` logs duplex calls as a request/response pair with no content (§15).

---

## 7. Phase 3 — The gRPC UI (S4)

**Red first.** Add S4 in all six suites; it fails with `404`.

### 7.1 Route

`Documentation.Contracts.GrpcUi = "/grpc"`. One `MapGet` matches `/grpc` and `/grpc/` (A.5.11); the handler serves the
page on the trailing-slash form and redirects the other, so relative links resolve the same way in-app
(`/grpc/` → `/grpc/v1.json`) and on Pages (`api/grpc/` → `api/grpc/v1.json`):

```csharp
app.MapGet(Documentation.Contracts.GrpcUi, (HttpContext context) =>
        context.Request.Path.Value!.EndsWith('/')
            ? Results.Text(GrpcContract.Html, MediaTypeNames.Text.Html)
            : Results.Redirect($"{context.Request.PathBase}{context.Request.Path}/", permanent: true))
    .ExcludeFromDescription();
```

### 7.2 The page — `src/BreakfastProvider.Api/Contracts/GrpcContractPage.cs`

`public static string Render(FileDescriptorSet set)`, pure and deterministic (no clock, no GUID, descriptor order).
Contents, top to bottom:

1. `<title>Breakfast Provider — gRPC API</title>` and, first on the page, a `<nav class="contract-bar">` with the
   documents as buttons, styled like the Pages bar (§10.2): `v1.json` (the descriptor set) and
   `protos/breakfast.proto`. On Pages the build prepends a link back to the landing page. Then a heading and one line:
   *gRPC over HTTP/2. Server reflection is enabled (`grpc.reflection.v1`), so grpcurl, Postman and Kreya can discover
   and call the service.* No host names or ports: the page is also published on Pages.
2. Per service: `<h2 id="breakfast.BreakfastGrpc">` with its comment, then a table *Method | Request | Response | Kind |
   Description*. Kind is `unary`, `server streaming`, `client streaming` or `bidirectional streaming`. Request and
   response types link to their message anchors.
3. Per message: `<h3 id="breakfast.RecipeSummaryRequest">` with its comment, then *Field | Number | Type | JSON name |
   Description*. Scalar types are written the proto way (`string`, `int32`), `repeated` / `optional` prefixed; message and
   enum types link to their anchors.
4. Per enum (none today; the renderer handles them): its values and comments.
5. A footer: *Generated from breakfast.proto by Breakfast Provider.*

Everything that comes from the descriptor is HTML-encoded (`WebUtility.HtmlEncode`); comments are trimmed and blank-line
paragraphs become `<p>`s. A little inline CSS, with a `prefers-color-scheme: dark` block; no script, fonts or CDN. The
spike's twenty-line version of this page rendered the service, its comment and "server streaming" for the real API
(A.5.8).

### 7.3 Green

Run all six suites; commit `docs/grpc.html` (the page as served, written by S4).

---

## 8. Phase 4 — The GraphQL contract: document, definition and UI (S7, S8, S9)

**Red first.** Add S7, S8 and S9 in all six suites. S7 fails with `404`, and its description assertion keeps it red
until 8.1 lands. S9 fails on its last step — the page comes from ChilliCream's CDN — until 8.2 embeds Nitro. S8 passes
on first run, since HotChocolate already serves the SDL, so like S1 its red step is to point it at a wrong route and
watch it fail; from here on it guards `EnableSchemaRequests`.

**8.1 Describe the schema.** XML doc comments on `ReportingQuery` and each of its seven methods, and on the reporting
types it returns (`OrderSummary`, `RecipeReport`, `IngredientUsage`, `RecipeTypeCount`, `BatchCompletionRecord`,
`IngredientShipment`, `EquipmentAlert`) and their properties. One sentence each: what it is and which flow fills it,
checked against the ingesters (`ReportingIngester`, the Kafka / Pub/Sub / Event Hub consumers, the EventGrid webhook).
`GenerateDocumentationFile` is already on, and HotChocolate reads `BreakfastProvider.Api.xml` at runtime (A.4.8); the
external-SUT lane proves the file is in the Docker image, because S7 fails without descriptions.

**8.2 Make HotChocolate's publishing explicit, and serve Nitro from the package** (`Program.cs:363`) — explicit so a
later HotChocolate upgrade that changes a default fails a scenario instead of silently unpublishing something:

```csharp
app.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    EnableSchemaRequests = true,                          // GET /graphql/schema.graphql
    Tool =
    {
        Enable = true,                                    // Nitro at GET /graphql/
        ServeMode = GraphQLToolServeMode.Embedded         // from the package, not proxied from ChilliCream's CDN
    }
});
app.MapGraphQLSchemaJson();                               // GET /graphql/schema.json
```

`WithOptions` replaces the whole options object; every other property keeps `GraphQLServerOptions`' default, which is
what `MapGraphQL()` uses today (verified: Nitro and SDL served, and the existing GraphQL features pass, A.4.9). The
serve mode is the one real change: without it the in-memory lane needs the internet to pass S9 (A.4.6).

**8.3 `src/BreakfastProvider.Api/Contracts/GraphQLContract.cs`** and the endpoint:

```csharp
using HotChocolate.Execution;

namespace BreakfastProvider.Api.Contracts;

/// <summary>The GraphQL contract as JSON: the standard introspection response, produced inside the service.</summary>
public static class GraphQLContract
{
    /// <summary>
    /// graphql-js's getIntrospectionQuery with descriptions, specifiedByUrl, directiveIsRepeatable,
    /// schemaDescription and inputValueDeprecation on.
    /// </summary>
    public const string IntrospectionQuery = """
        query IntrospectionQuery {
          __schema {
            description
            queryType { name }
            mutationType { name }
            subscriptionType { name }
            types { ...FullType }
            directives { name description isRepeatable locations args(includeDeprecated: true) { ...InputValue } }
          }
        }
        fragment FullType on __Type {
          kind name description specifiedByURL
          fields(includeDeprecated: true) {
            name description args(includeDeprecated: true) { ...InputValue } type { ...TypeRef } isDeprecated deprecationReason
          }
          inputFields(includeDeprecated: true) { ...InputValue }
          interfaces { ...TypeRef }
          enumValues(includeDeprecated: true) { name description isDeprecated deprecationReason }
          possibleTypes { ...TypeRef }
        }
        fragment InputValue on __InputValue {
          name description type { ...TypeRef } defaultValue isDeprecated deprecationReason
        }
        fragment TypeRef on __Type {
          kind name
          ofType { kind name ofType { kind name ofType { kind name ofType { kind name
            ofType { kind name ofType { kind name ofType { kind name ofType { kind name } } } } } } } }
        }
        """;

    public static async Task<string> IntrospectAsync(IRequestExecutorResolver executors, CancellationToken cancellationToken)
    {
        var executor = await executors.GetRequestExecutorAsync(cancellationToken: cancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(IntrospectionQuery)
            .AllowIntrospection()   // this request only: client introspection keeps HotChocolate's default
            .Build();
        var result = await executor.ExecuteAsync(request, cancellationToken);
        return result.ToJson();
    }
}
```

```csharp
public static IEndpointRouteBuilder MapGraphQLSchemaJson(this IEndpointRouteBuilder app)
{
    app.MapGet(Documentation.Contracts.GraphQLSchemaJson, async (IRequestExecutorResolver executors, CancellationToken ct) =>
            Results.Text(await GraphQLContract.IntrospectAsync(executors, ct), MediaTypeNames.Application.Json))
        .ExcludeFromDescription();
    return app;
}
```

Introspection costs a few milliseconds and needs no database, so there is no caching. On HotChocolate 16 the executor
comes from `IRequestExecutorProvider` (in 16.6.7's abstractions) rather than 15's `IRequestExecutorResolver` (§13).

**8.4 Green.** Run all six suites; commit `docs/graphql.json` and `docs/schema.graphql`.

---

## 9. The scenarios — the same in all six suites

### 9.1 The list

Feature and scenario names follow `.claude/skills/component-tests/naming-conventions.md`: LightBDD features are
`Specifications__…_Feature` with Title_Case scenarios; xUnit, NUnit, TUnit and BDDfy classes are
`Specifications_…_Tests` with Sentence_case methods; ReqNRoll has one `.feature` per class. Each happy path is
`[HappyPath]` (`@happy-path`), each document-producing scenario carries the suite's "Produces" tag
(`[Trait("Produces", "grpc.json")]`, NUnit `[Category("Produces: grpc.json")]`, TUnit `[Property("Produces", "grpc.json")]`).

| # | Feature (LightBDD) | Scenario | Steps | Phase |
|---|---|---|---|---|
| S1 | `Specifications__Async_Api_UI_Feature` — "/asyncapi - Serving the AsyncAPI documentation UI" | The AsyncApi UI endpoint should return a valid page | **When** the asyncapi ui endpoint is called · **Then** the response should be a valid asyncapi page *(status OK; `<html`; renders `AsyncApiStandalone` from `/asyncapi/v1.json`)* | 0 |
| S2 | `Specifications__Grpc_Contract_Feature` — "/grpc/v1.json; /grpc/protos/breakfast.proto - Serving the gRPC contract as a descriptor set and as its proto file" | The Grpc contract endpoint should return a valid specification | **When** the grpc contract endpoint is called · **Then** the response should be valid *(status OK; the body parses as a `FileDescriptorSet`)* · **And** the grpc contract should describe the breakfast service *(file `breakfast.proto`, package `breakfast`, service `BreakfastGrpc` with its three methods, `StreamOrderUpdates` server streaming)* · **And** every breakfast method should be documented *(a leading comment at `[6, 0, 2, m]` for each method)* · **And** the grpc contract is written to disk *(`docs/grpc.json`, attached)* | 1 |
| S3 | same feature | The Grpc proto endpoint should return the proto file | **When** the grpc proto endpoint is called · **Then** the response should be a plain text proto file *(status OK; `text/plain`)* · **And** the proto file should declare the breakfast service *(`package breakfast;`, `service BreakfastGrpc`)* | 1 |
| S4 | `Specifications__Grpc_UI_Feature` — "/grpc/ - Serving the gRPC documentation page" | The Grpc UI endpoint should return a page describing the service | **When** the grpc ui endpoint is called · **Then** the response should be a valid grpc documentation page *(status OK; `<html`)* · **And** the page should describe every breakfast method *(each method name; `server streaming` for `StreamOrderUpdates`; the service comment)* · **And** the page should link to the grpc contract *(`href="v1.json"`, `href="protos/breakfast.proto"`)* · **And** the grpc ui page is written to disk *(`docs/grpc.html`, attached)* | 3 |
| S5 | `Specifications__Grpc_Reflection_Feature` — "gRPC server reflection - Letting gRPC tools discover the breakfast service" | Grpc server reflection should list the breakfast service | **When** the services are listed through grpc server reflection · **Then** the breakfast service should be listed · **And** the reflection service should be listed *(`grpc.reflection.v1.ServerReflection`)* | 2 |
| S6 | same feature | Grpc server reflection should describe the breakfast service | **When** the breakfast service is described through grpc server reflection · **Then** the description should be the breakfast proto file · **And** the description should contain every breakfast method | 2 |
| S7 | `Specifications__GraphQL_Schema_Feature` — "/graphql/schema.json; /graphql/schema.graphql - Serving the GraphQL schema as introspection JSON and as a schema definition" | The GraphQL schema endpoint should return a valid specification | **When** the graphql schema endpoint is called · **Then** the response should be valid *(status OK; valid JSON)* · **And** the schema should contain all the reporting queries *(`queryType` `ReportingQuery`; its seven fields)* · **And** the reporting queries should be documented *(the query type and each of its fields has a description)* · **And** the graphql schema is written to disk *(`docs/graphql.json`, attached)* | 4 |
| S8 | same feature | The GraphQL schema definition endpoint should return the schema definition | **When** the graphql schema definition endpoint is called · **Then** the response should be a graphql schema definition *(status OK; `application/graphql`)* · **And** the schema definition should declare all the reporting queries *(`type ReportingQuery {` and the seven field names)* · **And** the graphql schema definition is written to disk *(`docs/schema.graphql`, attached)* | 4 |
| S9 | `Specifications__GraphQL_UI_Feature` — "/graphql/ - Serving the Nitro GraphQL IDE" | The GraphQL UI endpoint should return a valid page | **When** the graphql ui endpoint is called *(`Accept: text/html`)* · **Then** the response should be a valid nitro page *(status OK; `<html`; `Nitro`)* · **And** the page should be served by the service itself *(no `cf-ray` header: the embedded Nitro, not ChilliCream's CDN)* | 4 |

The class names in the other suites: `Specifications_Async_Api_UI_Tests`, `Specifications_Grpc_Contract_Tests`,
`Specifications_Grpc_UI_Tests`, `Specifications_Grpc_Reflection_Tests`, `Specifications_GraphQL_Schema_Tests`,
`Specifications_GraphQL_UI_Tests`. ReqNRoll: `SpecificationsAsyncApiUI.feature`, `SpecificationsGrpcContract.feature`,
`SpecificationsGrpcUI.feature`, `SpecificationsGrpcReflection.feature`, `SpecificationsGraphQLSchema.feature`,
`SpecificationsGraphQLUI.feature`.

No scenario needs `[IgnoreIf]` or `[SkipStepIf]`: every one talks to the SUT over HTTP or gRPC, so all run in memory,
in Docker, against the external SUT and post-deployment. The reflection scenarios use `ExternalGrpcUrl` outside
memory, as the existing gRPC features do.

A scenario that proves `/graphql/schema.json` works where client introspection is refused would need a Production
host, which no suite can build per scenario today; A.4.4 covers it instead (§15).

### 9.2 Shared pieces (once, in `tests/BreakfastProvider.Tests.Component.Shared`)

| File | Content |
|---|---|
| `Constants/Endpoints.cs` | `Endpoints.AsyncApi.AsyncApiUI = BasePath` (`/asyncapi`); new `Endpoints.GrpcContract { ContractJson = "grpc/v1.json", ProtoFile = "grpc/protos/breakfast.proto", UI = "grpc/" }`; new `Endpoints.GraphQLContract { SchemaJson = "graphql/schema.json", SchemaDefinition = "graphql/schema.graphql", UI = "graphql/" }`. |
| `Constants/GrpcSpecs.cs`, `Constants/GraphQLSpecs.cs` | File names, like `OpenApiSpecs`: `grpc.json`, `grpc.html`; `graphql.json`, `schema.graphql`. |
| `Constants/GrpcContractDefaults.cs` | `ProtoFileName = "breakfast.proto"`, `Package = "breakfast"`, `ServiceName = "BreakfastGrpc"`, `ServiceFullName = "breakfast.BreakfastGrpc"`, `ReflectionServiceFullName = "grpc.reflection.v1.ServerReflection"`, the three method names, `ServerStreamingKind = "server streaming"`. |
| `Constants/GraphQLSchemaDefaults.cs` | `QueryTypeName = "ReportingQuery"`, the seven root field names, `NitroMarker = "Nitro"`. |
| `Constants/ContentTypes.cs` (or beside `CustomHeaders`) | `GraphQL = "application/graphql"` — the one media type `MediaTypeNames` lacks. |
| `Util/ContractDocs.cs` | Phase 0.1. |
| `Common/Specifications/SpecificationDocumentSteps.cs` | `RequestContext`-based: `Retrieve(path, accept = null)` → `ResponseMessage`, `Body`, `Json` (`JsonDocument?`); `DescriptorSet` (`JsonParser.Default.Parse<FileDescriptorSet>(Body)`, null when it does not parse); `WriteToDocs(fileName)` → path for the suite to attach. |
| `Common/Grpc/GrpcReflectionSteps.cs` | §6, item 2.3. |
| `Infrastructure/SourceControlledDocsHelper.cs` | Loops over `ContractDocs.FileNames`. |

The component-test skill lets specification features keep their logic inline ("self-contained feature exception").
With six suites the fetching, parsing and writing go into the two Shared step classes instead, so each suite keeps
only its step wiring and its assertions — TUnit asserts in its own dialect (`await x.Should().BeEqualTo(…)`).

### 9.3 Files per suite

| Suite | New files | Edited |
|---|---|---|
| xUnit, NUnit, TUnit, BDDfy | `Scenarios/Specifications/Specifications_{Async_Api_UI,Grpc_Contract,Grpc_UI,Grpc_Reflection,GraphQL_Schema,GraphQL_UI}_Tests.cs` | `Infrastructure/BaseFixture.cs` (register `SpecificationDocumentSteps`, `GrpcReflectionSteps`); `Specifications_Open_Api_Tests.cs`, `Specifications_Async_Api_Tests.cs` (Phase 0.1) |
| LightBDD | `Scenarios/Specifications/Specifications__{…}_Feature.cs` + `.steps.cs` (six pairs) | the same two registrations and Phase 0.1 edits |
| ReqNRoll | `Features/Specifications/Specifications{AsyncApiUI,GrpcContract,GrpcUI,GrpcReflection,GraphQLSchema,GraphQLUI}.feature` (+ regenerated `.feature.cs`, committed like the others); `StepDefinitions/Specifications/ContractSpecificationSteps.cs`, `GrpcReflectionSpecificationSteps.cs`, `SpecificationDocumentContext.cs` | `StepDefinitions/OpenApi/OpenApiSteps.cs` (Phase 0.2); `Support/DependencyInjectionSetup.cs` |

### 9.4 One scenario in all six dialects — S2

What "identical" means in practice. The other eight follow the same shape.

**LightBDD** — `Specifications__Grpc_Contract_Feature.cs`:

```csharp
[FeatureDescription($"/{Endpoints.GrpcContract.ContractJson}; /{Endpoints.GrpcContract.ProtoFile} - Serving the gRPC contract as a descriptor set and as its proto file")]
public partial class Specifications__Grpc_Contract_Feature
{
    [HappyPath]
    [Scenario]
    [Trait("Produces", "grpc.json")]
    public async Task The_Grpc_Contract_Endpoint_Should_Return_A_Valid_Specification()
    {
        await Runner.RunScenarioAsync(
            when => The_grpc_contract_endpoint_is_called(),
            then => The_response_should_be_valid(),
            and => The_grpc_contract_should_describe_the_breakfast_service(),
            and => Every_breakfast_method_should_be_documented(),
            and => The_grpc_contract_is_written_to_disk());
    }
}
```

`.steps.cs` (abridged):

```csharp
#pragma warning disable CS1998
public partial class Specifications__Grpc_Contract_Feature : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications__Grpc_Contract_Feature() => _documentSteps = Get<SpecificationDocumentSteps>();

    #region Given
    #endregion

    #region When
    private async Task The_grpc_contract_endpoint_is_called()
        => await _documentSteps.Retrieve(Endpoints.GrpcContract.ContractJson);
    #endregion

    #region Then
    private async Task<CompositeStep> The_response_should_be_valid() => Sub.Steps(
        _ => The_response_status_should_be_ok(),
        _ => The_response_should_be_a_valid_descriptor_set());

    private async Task The_response_status_should_be_ok()
        => _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_response_should_be_a_valid_descriptor_set()
        => _documentSteps.DescriptorSet.Should().NotBeNull();

    private async Task<CompositeStep> The_grpc_contract_should_describe_the_breakfast_service() => Sub.Steps(
        _ => The_contract_should_be_for_the_FILE_file(GrpcContractDefaults.ProtoFileName),
        _ => The_contract_should_declare_the_SERVICE_service(GrpcContractDefaults.ServiceName),
        _ => The_breakfast_service_should_offer_the_METHOD_method(GrpcContractDefaults.GetRecipeSummary),
        _ => The_breakfast_service_should_offer_the_METHOD_method(GrpcContractDefaults.GetOrderStatus),
        _ => The_breakfast_service_should_offer_the_METHOD_method(GrpcContractDefaults.StreamOrderUpdates),
        _ => The_METHOD_method_should_stream_its_replies(GrpcContractDefaults.StreamOrderUpdates));
    // … one-line assertions over _documentSteps.DescriptorSet, and:

    private async Task The_grpc_contract_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.JsonFileName);
        await StepExecution.Current.AttachFile(m => m.CreateFromFile(GrpcSpecs.JsonFileName, path));
    }
    #endregion
}
```

**xUnit** — `Specifications_Grpc_Contract_Tests.cs` (NUnit: `[Test]`, `[Category("Produces: grpc.json")]`; TUnit:
`[Test]`, `[Property("Produces", "grpc.json")]`, `await …Should().BeEqualTo(…)`):

```csharp
public class Specifications_Grpc_Contract_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Grpc_Contract_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Fact]
    [HappyPath]
    [Trait("Produces", "grpc.json")]
    public async Task The_Grpc_contract_endpoint_should_return_a_valid_specification()
    {
        // When the grpc contract endpoint is called
        await _documentSteps.Retrieve(Endpoints.GrpcContract.ContractJson);

        // Then the response should be valid
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        var contract = _documentSteps.DescriptorSet;
        contract.Should().NotBeNull();

        // And the grpc contract should describe the breakfast service
        var file = contract!.File.Should().ContainSingle().Which;
        file.Name.Should().Be(GrpcContractDefaults.ProtoFileName);
        var service = file.Service.Should().ContainSingle(s => s.Name == GrpcContractDefaults.ServiceName).Which;
        service.Method.Select(m => m.Name).Should().Equal(
            GrpcContractDefaults.GetRecipeSummary, GrpcContractDefaults.GetOrderStatus, GrpcContractDefaults.StreamOrderUpdates);
        service.Method.Single(m => m.Name == GrpcContractDefaults.StreamOrderUpdates).ServerStreaming.Should().BeTrue();

        // And every breakfast method should be documented
        for (var m = 0; m < service.Method.Count; m++)
            file.SourceCodeInfo.Location.Should().Contain(l => l.Path.SequenceEqual(new[] { 6, 0, 2, m }) && l.HasLeadingComments);

        // And the grpc contract is written to disk
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.JsonFileName);
        Track.Attachment(path, GrpcSpecs.JsonFileName);
    }
}
```

**BDDfy** — the same steps as private methods in a `this.When(…).Then(…).And(…).BDDfy()` chain, as
`Specifications_Async_Api_Tests.cs` does today.

**ReqNRoll** — `SpecificationsGrpcContract.feature`:

```gherkin
Feature: Specifications Grpc Contract
    /grpc/v1.json; /grpc/protos/breakfast.proto - Serving the gRPC contract as a descriptor set and as its proto file

    @happy-path
    Scenario: The Grpc contract endpoint should return a valid specification
        When the grpc contract endpoint is called
        Then the response should be valid
        And the grpc contract should describe the breakfast service
        And every breakfast method should be documented
        And the grpc contract is written to disk

    @happy-path
    Scenario: The Grpc proto endpoint should return the proto file
        When the grpc proto endpoint is called
        Then the response should be a plain text proto file
        And the proto file should declare the breakfast service
```

The bindings live in `StepDefinitions/Specifications/ContractSpecificationSteps.cs`; "the response should be valid" is
the shared Then from Phase 0.2, which validates according to what the When fetched (a descriptor set here, JSON for
OpenAPI, AsyncAPI and GraphQL).

### 9.5 The check that coverage is identical

After each phase: every suite's total rises by the number of scenarios the phase added (Phase 0 +1, Phase 1 +2,
Phase 2 +2, Phase 3 +1, Phase 4 +3), and running the six suites one after another leaves `git status docs/` clean —
the six suites wrote the same bytes. The scenario and step wording is compared across the six by reading the table in
§9.1 against each suite's file.

---

## 10. Phase 5 — CI and GitHub Pages

### 10.1 The drift gate (D12)

As the **last** step of the job in `_tests.yml` (after "Check that the checked in Component Specifications document
matches…") and in `_tests-tunit.yml` (after "Upload report artifact", condition `inputs.test-run-type == 'memory'`).
Last, because a failed step skips every later step whose `if:` has no status function — in `_tests.yml` the two
`Specifications.yml` steps — and a stale contract should fail the lane without changing what else it does:

```yaml
      # Pages publishes the contracts committed in docs/. Every in-memory lane has just rewritten them from the
      # service, and all six suites write the same bytes, so a difference means docs/ is stale.
      - name: Check the committed contracts match the service
        if: ${{ !cancelled() && inputs.test-type == 'component' && inputs.test-run-type == 'memory' }}
        run: |
          contracts="docs/openapi.json docs/asyncapi.json docs/grpc.json docs/grpc.html docs/graphql.json docs/schema.graphql"
          changed=$(git status --porcelain -- $contracts)
          if [ -n "$changed" ]; then
            echo "$changed"
            git --no-pager diff --stat -- $contracts
            echo "::error title=Stale contracts::docs/ does not match what the service serves. Run any component suite in memory and commit docs/."
            exit 1
          fi
```

`git status --porcelain` rather than `git diff`, so a contract file generated but never committed (untracked) fails
the lane too. `docs/Specifications.yml` stays out: each suite writes its own dialect.

### 10.2 The Pages site — every contract as a page and as a document

On Pages every contract's JSON is **one click from the landing page and one click from its own UI page**, at a
predictable URL, `api/<contract>.json` (D13):

| Contract | UI page | JSON document | Also |
|---|---|---|---|
| OpenAPI | `api/openapi.html` (Scalar) | `api/openapi.json` | — |
| AsyncAPI | `api/asyncapi.html` (AsyncAPI React) | `api/asyncapi.json` | — |
| gRPC | `api/grpc/` (the service's own page) | `api/grpc.json` | `api/grpc/protos/breakfast.proto`; `api/grpc/v1.json`, the same file where the page's relative link expects it |
| GraphQL | `api/graphql.html` (GraphQL Voyager) | `api/graphql.json` | `api/schema.graphql` |

**Move the inline heredocs into `.github/scripts/build-api-pages.sh <site-dir>`**, so the site can be built and looked
at locally. Move the landing page out of its heredoc too (`ci-main.yml:791-955` → `.github/pages/index.html`, copied
by the script), so the link check below covers it. The script:

1. **pins every renderer** and downloads it with `curl -fsSL`, so a failed download fails the job instead of
   publishing a broken page: `@scalar/api-reference@1.72.2/dist/browser/standalone.js` (the file today's unpinned
   `https://cdn.jsdelivr.net/npm/@scalar/api-reference` resolves to, the package's `browser` entry),
   `@asyncapi/react-component@3.2.1` (`browser/standalone/index.js`, `styles/default.min.css`) and
   `graphql-voyager@2.1.0` (`dist/voyager.standalone.js`, `dist/voyager.css`). These were the npm latest on 2026-09-30;
   re-check when implementing.
2. **copies the documents** to the URLs in the table: `docs/openapi.json`, `docs/asyncapi.json`, `docs/graphql.json`
   and `docs/schema.graphql` into `api/`; `docs/grpc.json` to both `api/grpc.json` and `api/grpc/v1.json`;
   `docs/grpc.html` to `api/grpc/index.html`; `src/BreakfastProvider.Api/Protos/breakfast.proto` to
   `api/grpc/protos/breakfast.proto`.
3. **opens every UI page with the same bar**: the way back to the landing page, the page's name, and its documents as
   buttons. `contract-bar.css`:

   ```css
   /* The bar every contract page opens with: the way back, and the documents behind the page. */
   .contract-bar { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem 1rem; padding: .5rem 1rem;
     font: 14px/1.4 -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #4f46e5; color: #fff; }
   .contract-bar a { color: #fff; text-decoration: none; }
   .contract-bar a:hover { text-decoration: underline; }
   .contract-bar .title { font-weight: 600; margin-right: auto; }
   .contract-bar .doc { border: 1px solid rgba(255,255,255,.5); border-radius: 6px; padding: .1rem .5rem;
     font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 13px; }
   ```

   The OpenAPI shell is today's plus the bar (the AsyncAPI shell likewise, with `asyncapi.json`):

   ```html
   <!DOCTYPE html>
   <html lang="en">
   <head>
     <meta charset="utf-8">
     <meta name="viewport" content="width=device-width, initial-scale=1">
     <title>Breakfast Provider — OpenAPI</title>
     <link rel="stylesheet" href="./contract-bar.css">
   </head>
   <body style="margin:0">
     <nav class="contract-bar">
       <a href="../">← Breakfast Provider</a>
       <span class="title">OpenAPI</span>
       <a class="doc" href="./openapi.json">openapi.json</a>
     </nav>
     <script id="api-reference" data-url="./openapi.json"></script>
     <script src="./scalar.js"></script>
   </body>
   </html>
   ```

   The GraphQL shell (verified, A.9 and A.12) gives Voyager the height the bar leaves:

   ```html
   <!DOCTYPE html>
   <html lang="en">
   <head>
     <meta charset="utf-8">
     <meta name="viewport" content="width=device-width, initial-scale=1">
     <title>Breakfast Provider — GraphQL</title>
     <link rel="stylesheet" href="./voyager.css">
     <link rel="stylesheet" href="./contract-bar.css">
     <style>html, body { height: 100%; margin: 0; } body { display: flex; flex-direction: column; } #voyager { flex: 1; min-height: 0; }</style>
   </head>
   <body>
     <nav class="contract-bar">
       <a href="../">← Breakfast Provider</a>
       <span class="title">GraphQL</span>
       <a class="doc" href="./graphql.json">graphql.json</a>
       <a class="doc" href="./schema.graphql">schema.graphql</a>
     </nav>
     <div id="voyager">Loading…</div>
     <script src="./voyager.standalone.js"></script>
     <script>
       GraphQLVoyager.renderVoyager(document.getElementById('voyager'), {
         introspection: fetch('./graphql.json').then(response => response.json()),
         displayOptions: { rootType: 'ReportingQuery' }
       });
     </script>
   </body>
   </html>
   ```

   The gRPC page draws its own bar with its documents (§7.2), because the service serves the same page in-app, where
   a link back to the landing page would mean nothing. The script adds that link when it copies the page to Pages:
   `sed 's#<nav class="contract-bar">#&<a href="../../">← Breakfast Provider</a>#'`.
4. **checks every contract link**: `.github/scripts/check-site-links.py site` resolves the landing page's links into
   `api/` and every relative link on each page under `api/` against the files in `site/`. It fails the job, naming
   page and link, when one is missing — verified: with `schema.graphql` removed it failed on both pages that link it,
   and with `grpc.json` removed on the landing page (A.12). It leaves the test-report links alone, so it also runs
   locally, where no reports have been downloaded.

   ```python
   #!/usr/bin/env python3
   """Every link to a contract on the Pages site must reach a file the site contains: the landing page's links into
   api/, and every relative link on the pages under api/. usage: check-site-links.py <site-dir>"""
   import pathlib, re, sys

   site = pathlib.Path(sys.argv[1]).resolve()
   checks = [(site / "index.html", lambda href: href.startswith("api/"))]
   checks += [(page, lambda href: True) for page in sorted((site / "api").rglob("*.html"))]
   missing = []
   for page, wanted in checks:
       for href in re.findall(r'href="([^"#?]+)', page.read_text(encoding="utf-8")):
           if re.match(r"^[a-z]+:|^//", href) or not wanted(href):   # absolute URLs, and links that are not ours
               continue
           target = (page.parent / href).resolve()
           if target.is_dir():
               target = target / "index.html"
           if not target.is_file():
               missing.append(f"{page.relative_to(site)} -> {href}")
   print("\n".join(missing) or f"all contract links resolve ({len(checks)} pages)")
   sys.exit(1 if missing else 0)
   ```

`deploy-pages` then runs the two scripts; its sparse checkout adds the four new `docs/` files, `breakfast.proto`,
`.github/pages/` and the scripts.

**Landing page** ("API Specifications", `ci-main.yml:920-932`). Today each card is a single `<a>`, and a link cannot sit
inside a link, so the specification cards become containers holding two kinds of link: the page, and the documents
behind it (verified render, A.12):

```html
<div class="card">
  <div class="card-icon">🔌</div>
  <h3>OpenAPI</h3>
  <p>REST endpoints</p>
  <div class="spec-links">
    <a class="ui" href="api/openapi.html">Open in Scalar</a>
    <a class="doc" href="api/openapi.json">openapi.json</a>
  </div>
</div>
<!-- AsyncAPI: api/asyncapi.html + api/asyncapi.json
     gRPC:     api/grpc/ + api/grpc.json + api/grpc/protos/breakfast.proto
     GraphQL:  api/graphql.html + api/graphql.json + api/schema.graphql -->
```

```css
/* A specification card holds two kinds of link: the page, and the documents behind it. */
.spec-links { display: flex; flex-wrap: wrap; gap: .5rem; margin-top: .9rem; }
.spec-links a { font-size: .8rem; font-weight: 600; text-decoration: none; border-radius: 6px; padding: .3rem .65rem;
  border: 1px solid var(--primary); color: var(--primary); }
.spec-links a.ui { background: var(--primary); color: #fff; }
.spec-links a.doc { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-weight: 500; }
.spec-links a:hover { background: var(--primary-dark); border-color: var(--primary-dark); color: #fff; }
```

The JSON documents open in the browser: a `.json` file is served as `application/json`. A plain static server sent the
`.graphql` and `.proto` files as `application/octet-stream`, so expect Pages to offer those two for download, which is
fine for source files. Check both after the first deploy.

### 10.3 Checking it

The Pages job only runs on `main`. Before merging:

```bash
.github/scripts/build-api-pages.sh site && .github/scripts/check-site-links.py site && python3 -m http.server -d site 8000
```

Then open the landing page, each of the four pages, and each document from both places. The spike did this with a
prototype of the site in headless Chromium (A.12). After merging, do the same on
https://lemonlion.github.io/BreakfastProvider/.

---

## 11. Phase 6 — Documentation

- **README.md.** A "Contracts" section replacing "Specifications & Documentation": the §1 table (service routes,
  `docs/` files, and on Pages each UI page beside its JSON URL); how to reach gRPC — `grpcurl -plaintext localhost:5081 list` against the Docker SUT, and
  `grpcurl -insecure localhost:7270 list` under the `https` launch profile, because the `http` profile's cleartext port
  5239 cannot carry HTTP/2 (A.5.7; try the TLS command when writing the README, the spike had no development
  certificate); the Nitro licence and status call (D7). "API Endpoints" gains the six routes; "Tech Stack" gains server
  reflection and Nitro; "Resources" links the new documents.
- **`.github/copilot-instructions.md`** — Tech Stack and API Conventions, the same facts.
- **`.claude/skills/component-tests/`** — `SKILL.md`'s self-contained feature exception names the new specification
  features and says their fetching lives in `SpecificationDocumentSteps`; `test-infrastructure.md`'s "Generated
  Artifacts" table lists the six contract files and the drift gate.
- **This file** — status "implemented", with anything that turned out differently.

---

## 12. Order of work, commits and acceptance

One commit per phase, each green in all six suites in memory before it is pushed, each red first (TDD, §4–§8):

| # | Commit | Suites' totals after (xUnit / LightBDD / ReqNRoll) |
|---|---|---|
| 1 | Phase 0 — deterministic contract files, ReqNRoll shared-step fix, stale routes, AsyncAPI UI scenario, `Microsoft.AspNetCore.OpenApi` 10.0.12, post-deployment paths | 204 / 179 / 206 |
| 2 | Phase 1 — gRPC contract document and proto file | 206 / 181 / 208 |
| 3 | Phase 2 — gRPC server reflection, gRPC 2.84.0 | 208 / 183 / 210 |
| 4 | Phase 3 — gRPC documentation page | 209 / 184 / 211 |
| 5 | Phase 4 — GraphQL schema document, definition and Nitro | 212 / 187 / 214 |
| 6 | Phase 5 — drift gate; Pages with every JSON one click away and a link check | 212 / 187 / 214 |
| 7 | Phase 6 — documentation | — |

NUnit, TUnit and BDDfy move with xUnit. Commit messages follow the repo's style (what changed, what was measured —
test counts per suite, and the `TestRunReport.json` size before and after, since the two new JSON bodies land in the
report).

**Acceptance.**

- [ ] All six suites green in memory; each total exactly 9 above §0.5.
- [ ] One suite green in memory **with the network off** (e.g. `unshare -rn` on Linux, as in Appendix B): no contract
      page or document needs the internet.
- [ ] Running the six suites one after another leaves `git status` clean: six identical writers.
- [ ] `docs/` holds `openapi.json`, `asyncapi.json`, `grpc.json`, `grpc.html`, `graphql.json`, `schema.graphql`, all LF
      without a BOM; `docs/openapi.json` lists no `/grpc…` or `/graphql/schema.json` path.
- [ ] CI: all eighteen component lanes green — the docker lanes and the external-SUT lanes prove reflection over h2c on
      `:8081` and the XML documentation inside the published image; the drift gate passes in the six memory lanes.
- [ ] Pages: each of the four specification cards opens its UI page and its JSON (and the SDL and proto); each UI page
      opens with a bar linking its documents; every JSON opens in the browser; `check-site-links.py` passes; each page
      renders its contract.
- [ ] README, copilot instructions and the component-test skill updated; this plan marked implemented.

---

## 13. Risks and traps

| Trap | Why it bites | Guard |
|---|---|---|
| A new GET lands in `docs/openapi.json` | Minimal API endpoints are in the OpenAPI document by default (A.5.9) | `.ExcludeFromDescription()` on every contract endpoint; the drift gate shows the diff |
| The gRPC catch-all route swallows a GET under `/grpc/` | The package maps `/{service}/{method}` for unimplemented services | It is POST-only (`Program.cs:354-362`), literal routes win anyway; S2-S4 would fail |
| `/grpc` served without its trailing slash | The page's relative links would resolve against `/` | One `MapGet` redirects the slashless form (A.5.11); S4 fetches `grpc/` |
| The embedded descriptor set is missing or stale | `Protobuf_Compile` runs after `PrepareResources` | `EmbedGrpcContract` depends on `Protobuf_Compile` (clean and incremental builds verified, A.5.2); `GrpcContract` throws a message naming the target |
| Comments carry `\r` from a Windows checkout | protoc copies comments verbatim | `*.proto text eol=lf`; `ContractDocs.Normalise` |
| Tests assert `Query` | The root type is `ReportingQuery` (the spike's first attempt made this mistake, A.4.5) | `GraphQLSchemaDefaults.QueryTypeName` |
| Client introspection "fixed" by switching it on | HotChocolate refuses it outside Development on purpose | Leave the default; `schema.json` uses `AllowIntrospection()` on its own request only (D5) |
| HotChocolate 16 | The executor comes from `IRequestExecutorProvider` instead of `IRequestExecutorResolver` (A.11); `GraphQLToolOptions` becomes `NitroAppOptions` and `GraphQLToolServeMode` becomes `ServeMode`, and `schema.graphql` loses internal directives such as `@cost` (its migration guide, C) — so the upgrade edits §8's code and diffs `docs/schema.graphql` | Explicit `WithOptions` (§8, item 8.2); S7-S9 fail on any change; the drift gate shows the SDL diff |
| gRPC 2.71 → 2.84 | A minor-version jump across the server stack | 203/203 xUnit in memory on 2.84.0 with reflection mapped (A.5.8); the docker and external-SUT lanes cover h2c |
| Report size | The GraphQL document is ~120 KB and becomes a response body in the report | Kronikol 4.0 compresses large bodies; record the size before and after (§12) |
| Nitro licence and status call | ChilliCream License 1.0; the browser calls `api.chillicream.com/status` | Documented (D7); GraphiQL is the drop-in alternative if the licence is unacceptable (§14) |
| Nitro fetched from the CDN | HotChocolate 15's default `ServeMode.Latest` proxies `cdn.chillicream.com`; offline, `GET /graphql/` is `502`, in memory too (A.4.6) | `ServeMode = GraphQLToolServeMode.Embedded` (§8, item 8.2); S9's last step fails if the page comes from the CDN again |
| Custom options vanish from the JSON | `Google.Protobuf`'s `JsonFormatter` writes an extension option such as `(google.api.http)` as `"options": {}` (research pass, C) | None needed today — `breakfast.proto` has none. If it gains one, the `.proto` (D2) carries it; say so on the page |
| Well-known types in the published set | If `breakfast.proto` ever imports `google/protobuf/*.proto`, their descriptors change with `Google.Protobuf` releases and would churn `docs/grpc.json` | Keep `--include_imports` off, as §5 item 1.2 has it: the set holds `breakfast.proto` only |
| Six suites writing `docs/` at once | Parallel lanes on one machine share the workspace | Identical bytes, and `ContractDocs.WriteAsync` keeps the `IOException` retries |
| A Pages page loses its link to the JSON, or links a file that is not there | The shells and the landing page are hand-written HTML | `check-site-links.py` fails the Pages job on any relative link to a missing file (§10.2); the acceptance list clicks through every document |
| Two attachments with one file name | Kronikol copies an attachment into `Reports/attachments/` under its file name and renames a clash (`ReportGenerator`, `GetUniqueFileName`); the teardown copy looks files up by name, so it would publish the wrong one | Six distinct names (`openapi.json`, `asyncapi.json`, `grpc.json`, `grpc.html`, `graphql.json`, `schema.graphql`) — never a second `schema.json` or `v1.json` |
| Kestrel cleartext `Http1AndHttp2` | Does not accept HTTP/2 without TLS (A.5.7) | The Docker SUT keeps the separate `Http2` endpoint on `:8081`; the README says how to reach gRPC locally |

---

## 14. Rejected alternatives

| Alternative | For | Against (evidence) |
|---|---|---|
| **GrpcBrowser** (in-app Swagger-like gRPC UI) | Interactive, served by the app | 1.3.4 (2025-10-17) still targets `netcoreapp3.1` with MudBlazor 2.0.7, Fluxor 4.2.1 and `protobuf-net.Grpc`; on net10 its page is blank until the project sets `RequiresAspNetWebAssets` and maps static files; it pulls `Newtonsoft.Json` 12.0.3 (NU1903 high); it ships `appsettings*.json` as content files; it lists every method twice (sync and async client methods); it loads Google Fonts from a CDN; in headless Chromium, clicks on an operation were intercepted by overlapping elements; it calls the service through the app's first bound address, which in Docker is the HTTP/1.1-only `:8080` — the research pass saw calls fail over plain HTTP — and keeps its services in static lists, which parallel suites share (C); and as a Blazor Server app it cannot be a Pages page (A.6) |
| **grpcui** (fullstorydev) | The best-known interactive gRPC UI | A Go binary: a sidecar container, not a page of the service; absent from the in-memory lane and from Pages. Reflection (D3) is what it needs — an optional compose service is listed in §15 |
| **Scalar** for gRPC | Already the OpenAPI UI | Scalar.AspNetCore 2.17.11 knows two document types, OpenAPI and AsyncAPI (A.7) |
| **JSON transcoding + `Microsoft.AspNetCore.Grpc.Swagger`** | gRPC in Scalar via an OpenAPI view | Changes the contract (`google.api.http` annotations, new REST routes); the package is **deprecated** — 0.10.11 is its last release and it has left the aspnetcore repo (C) — and is Swashbuckle-based (`Swashbuckle.AspNetCore` 6.6.2, A.11); registered beside the built-in generator it made `/openapi/v1.json` answer `500` (research pass, C); the built-in generator does not describe transcoded routes at all (C); and an HTTP/JSON view is not the gRPC contract (no field numbers, streaming semantics or status codes). If a REST view is ever wanted, the research pass got a separate OpenAPI 3.1 document out of `protoc-gen-connect-openapi` run by Grpc.Tools' protoc, or out of `Community.Grpc.SwaggerGen` with the gRPC routes filtered from the built-in document (C) |
| **protoc-gen-doc** (HTML/JSON from protos) | Mature static docs; runs under Grpc.Tools' own protoc | A prebuilt plugin binary per platform to vendor or download (v1.5.1, last release 2022-02-18) — a download breaks "tests just work on pull and build, offline" — and a JSON format of its own instead of the standard descriptor set |
| **buf** (`buf build -o x.json --as-file-descriptor-set`) | Deterministic JSON with comments and custom options | Another binary in the build for what protoc and `JsonFormatter` already give |
| **Kaya.GrpcExplorer** 1.2.0 (in-app, net10) | Interactive, served by the app at `/grpc-explorer` | Maps its own reflection service — beside ours, an `AmbiguousMatchException` (`500`) — and calls through the app's own address, so on the HTTP/1.1-only port it lists nothing; eleven stars, first released 2026 (research pass, C). `Kuestenlogik.Bowire` 2.8.0 did not build |
| **Descriptor JSON built at runtime** from `BreakfastReflection.Descriptor` | No build step | Comments are gone (A.5.1) — a contract without descriptions |
| **GraphQL JSON by client POST from the tests** | No new endpoint | Works only where client introspection is allowed (Development); no GET URL to publish (A.4.3) |
| **Switch client introspection on everywhere** | Simplest | Weakens HotChocolate's secure default for no gain: the published document does not need it (D5) |
| **GraphiQL / Voyager inside the app** (`GraphQL.Server.Ui.*` 8.3.3) | MIT-licensed | Nitro is already there and richer; both introspect by POST, which Production refuses; and both load their app from a CDN at view time (`graphiql@3.2.0` from unpkg, `graphql-voyager@1.3.0` from jsDelivr, C). GraphiQL remains the fallback if Nitro's licence is unacceptable |
| **SpectaQL / Magidoc** for the GraphQL Pages page | Reference-style static docs | A Node build step; Voyager needs only a pinned file, like Scalar and AsyncAPI today |

---

## 15. Out of scope, and follow-ups

- **An interactive gRPC runner.** An optional compose service for local Docker work; no lane needs it:

  ```yaml
  grpcui:                                       # beside breakfast-provider-api in docker-compose-sut.yml; not run
    image: ghcr.io/fullstorydev/grpcui:v1.5.4   # Docker Hub's fullstorydev/grpcui:latest is older (v1.5.2)
    command: ["-plaintext", "breakfast-provider-api:8081"]   # the entrypoint already binds 0.0.0.0:8080
    ports: ["5082:8080"]
    networks: [localdev]
    depends_on: [breakfast-provider-api]
  ```

  The research pass ran the grpcui v1.5.4 binary (not the image) against the service's reflection: it found
  `breakfast.BreakfastGrpc` and invoked the streaming method, with its assets served offline (C). `Tool.Grpc.UI`
  packages grpcui as a `dotnet tool`, for use without Docker (reported, not tried).
- **The AsyncAPI UI on Scalar.** `Bielu.AspNetCore.AsyncApi.UI` is deprecated in favour of Scalar, which reads AsyncAPI
  from 2.16.1 (A.7). Moving means upgrading Scalar.AspNetCore 2.14.4 and changing S1's marker.
- **Breaking-change checks** on pull requests: `buf breaking` for the proto, `graphql-inspector diff` for the schema,
  `oasdiff` for OpenAPI — the natural next step once the contracts are committed and gated.
- **Kronikol:** `Kronikol.Extensions.Grpc` logs client-streaming and duplex calls, and server-streaming replies, without
  message bodies, so the reflection call is a bare arrow. Capturing streamed messages would be a Kronikol feature
  (minor bump there); nothing here depends on it.
- **A Production-host scenario** for `/graphql/schema.json` would need per-scenario environment control in six
  `BaseFixture`s; A.4.4 covers the behaviour.
- **HotChocolate 16, Scalar 2.17, a conventional `Query` root name** — separate upgrades, each a contract diff of its
  own.
- **`notifications.proto`** — a consumed contract; publishing it is the Notification Service's business.

---

## Appendix A — Verified facts (spike, 2026-09-30)

Environment: this container, Linux x64, .NET SDK 10.0.401, packages from nuget.org, headless Chromium from Playwright
1.56.1, grpcurl 1.9.3. "Standalone" is a minimal web app on the repo's package versions; "real API" is a scratch copy
of this repo with the §5-§8 changes prototyped and a scratch xUnit class, run under `WebApplicationFactory`
(environment Development).

**A.1 Today's publishing** (read from the code and workflows). Routes and packages as in §0.1-§0.2; nothing gated to
Development; writers A and B as in §0.3; `deploy-pages` publishes committed docs with unpinned renderers; no drift check
of the JSON documents anywhere; the `.github/actions/create-pull-request-on-developer-portal` action is used by no
workflow.

**A.2 Baseline** — §0.5.

**A.3 Local drift.** After any suite runs, `docs/openapi.json` differs at one line — the committed
`"…Supports subscription\r\nvalidation…"` becomes `"…subscription\nvalidation…"` — and `docs/Specifications.yml` is
rewritten in the running suite's dialect. `docs/asyncapi.json` came out byte-identical.

**A.4 HotChocolate 15.1.15.**
1. `GET /graphql?sdl`, `/graphql/schema.graphql` and `/graphql/schema` return identical SDL, `200 application/graphql;
   charset=utf-8`, `Content-Disposition: attachment; filename="schema.graphql"`, `Cache-Control: public,
   max-age=3600, must-revalidate`, an `ETag`. `/graphql/sdl` is `404`.
2. With `ASPNETCORE_ENVIRONMENT=Production` the SDL routes and Nitro still return `200`.
3. POST of the introspection query: Development `200 application/graphql-response+json`; Production `400`,
   `"Introspection is not allowed for the current request."`, `HC0046`.
4. Server-side: `IRequestExecutorResolver.GetRequestExecutorAsync()` + `OperationRequestBuilder.New()
   .SetDocument(query).AllowIntrospection().Build()` + `ExecuteAsync` + `ToJson()` returns `200` in Production,
   indented; semantically equal to the Development POST; byte-identical across restarts. The §8.3 query (with
   `specifiedByURL`, `isRepeatable`, `includeDeprecated` on arguments and input fields, schema `description`) raised no
   error. `executor.Schema.ToString()` equals the HTTP SDL.
5. Real API: root type `ReportingQuery` (`schema { query: ReportingQuery }`); the document is 123,283 bytes with
   seven root fields. `GET /graphql` → `301` to `/graphql/`; `GET /graphql/` → `200 text/html` with or without
   `Accept: text/html`.
6. Nitro (`ChilliCream.Nitro.App` 28.0.7, a dependency of `HotChocolate.AspNetCore`). With the default
   `ServeMode.Latest` the page (`<title>Nitro IDE</title>`) and its assets (`/graphql/assets/main….js`, 2,867,475
   bytes) come back with `Server: cloudflare` and `cf-ray` headers — the service proxies `cdn.chillicream.com` — and in
   a network namespace with loopback only, `GET /graphql/` answered `502`. With
   `ServeMode = GraphQLToolServeMode.Embedded`, in the same namespace, it answered `200` from Kestrel
   (`<title>Nitro</title>`) and its scripts (`/graphql/static/js/*.66754811.js`, the package's build) `200`. In
   Production both loaded the schema the same way: after "Create Document" the page fetched
   `GET /graphql/schema.graphql` and showed "Schema available"; its feature probe
   (`GET …?query=…__type(name: "__SearchResult")…`) got `400 HC0046`, harmlessly. The same `502` happens under an in-memory
   `WebApplicationFactory` (A.4.9; the research pass found it too, C).
7. The browser requested `https://api.chillicream.com/status` with and without `Tool.DisableTelemetry = true`.
   `Tool.Title` did not change the HTML `<title>`. Licence file: ChilliCream License 1.0.
8. XML doc comments become descriptions: a `<summary>` on `ReportingQuery` and on `GetOrderSummaries` appeared as
   `"Reporting queries over the business-intelligence database."` above `type ReportingQuery` and
   `"One summary per ingested order."` above `orderSummaries` (`BreakfastProvider.Api.xml` in the output).
9. `MapGraphQL().WithOptions(new GraphQLServerOptions { EnableSchemaRequests = true, Tool = { Enable = true, … } })`
   builds and serves SDL and Nitro. On the real API with exactly the §8 (8.2) options, embedded Nitro included, the
   full xUnit suite passed — 211 of 211: the 203 existing scenarios (five GraphQL reporting features among them) and
   eight scratch ones, the Nitro one asserting there is no `cf-ray` header — both online and in a network namespace
   with loopback only. With the default serve mode and no network, the scratch Nitro check got `502 Bad Gateway`
   under `WebApplicationFactory`. Present in the XML docs: `GraphQLServerOptions.EnableSchemaRequests` ("Defines if
   the GraphQL schema SDL can be downloaded"), `.Tool.Enable` ("Defines if Nitro is enabled"), `.Tool.DisableTelemetry`,
   `.Tool.Document`, `.Tool.Title`, `.Tool.ServeMode`; `MapGraphQLSchema(pattern, schemaName)`,
   `MapNitroApp(toolPath, relativeRequestPath)`.

**A.5 gRPC.**
1. `BreakfastReflection.Descriptor.ToProto()` in a `FileDescriptorSet` through `JsonFormatter`: 3,194 bytes, no
   `sourceCodeInfo`, no `jsonName` — the embedded descriptor carries no comments.
2. Grpc.Tools 2.71.0 with `AdditionalProtocArguments="--include_source_info;--descriptor_set_out=$(IntermediateOutputPath)breakfast.protoset"`
   writes `obj/Debug/net10.0/breakfast.protoset`. The `EmbedGrpcContract` target (§5, item 1.2) embedded it in a clean
   build, and an edited comment was in the DLL after an incremental build. The real API with two protos built with
   the arguments on `breakfast.proto` only.
3. With source info: 73 locations, 4 with comments; JSON 20,170 bytes. Trimmed to commented locations, the real API's
   document was 5,532 bytes, with `jsonName` on every field (protoc fills it).
4. The document was byte-identical across restarts and between Development and Production. The research pass found
   the service's file byte-identical under Google.Protobuf 3.31.1 and 3.35.1 as well (C).
5. `ProtoRoot="Protos"`: the descriptor and reflection name the file `breakfast.proto` (without it: `Protos/breakfast.proto`).
6. Reflection: `Grpc.AspNetCore.Server.Reflection` 2.71.0 lists only `grpc.reflection.v1alpha.ServerReflection`;
   2.84.0 lists `v1` and `v1alpha`. The `v1` service is absent from `Grpc.Reflection` 2.76.0 and present from 2.80.0.
7. grpcurl 1.9.3 against a Kestrel `Http2` cleartext endpoint: `list`, `describe` and a unary call worked; against a
   cleartext `Http1AndHttp2` endpoint: "context deadline exceeded".
8. Real API on `Grpc.AspNetCore` 2.84.0 with reflection and the contract endpoints: the full xUnit suite, 203 existing
   scenarios, passed in memory. Through `CreateTestTrackingGrpcClient<Program, ServerReflection.ServerReflectionClient>`
   over the TestServer, one duplex call answered `ListServices` (`breakfast.BreakfastGrpc`,
   `grpc.reflection.v1alpha.ServerReflection`, `grpc.reflection.v1.ServerReflection`) and `FileContainingSymbol`
   (`breakfast.proto`, three methods, no source info). `GET /grpc/v1.json` → `200 application/json`;
   `GET /grpc/protos/breakfast.proto` → `200 text/plain; charset=utf-8`; `GET /grpc` → `200 text/html` from the
   prototype page, showing the service comment and "server streaming".
9. Without `.ExcludeFromDescription()`, `docs/openapi.json` gained `/grpc/v1.json`, `/grpc/protos/breakfast.proto`,
   `/grpc` and `/graphql/schema.json` (tag `BreakfastProvider.Api`). The literal GET routes were not shadowed by the
   gRPC catch-all.
10. `JsonParser.Default.Parse<FileDescriptorSet>(json)` round-trips the document (equal to the original) and rejects
    an unknown field (`Unknown field: nam`).
11. One `MapGet("/grpc", …)` matched `/grpc` and `/grpc/`; the handler in §7.1 answered `301 → /grpc/` and `200`;
    `/grpc/v1.json` beside it answered `200`.

**A.6 GrpcBrowser 1.3.4** (MIT, published 2025-10-17, dependency group `.NETCoreApp3.1`: AutoFixture 4.17.0,
BlazorDownloadFile 2.3.1.1, Fluxor.Blazor.Web 4.2.1, Grpc.AspNetCore.Server 2.40.0, MudBlazor 2.0.7,
protobuf-net.Grpc 1.0.152, …; content files `appsettings.json`, `appsettings.Development.json`). Routes `/grpc`,
`/grpc/_blazor`, `/grpc/{*path:nonfile}`. On net10: builds (NU1903 for `Newtonsoft.Json` 12.0.3); the page is empty
because `/_framework/blazor.server.js` is `404` until the project sets `RequiresAspNetWebAssets` and maps static
files; then it lists the methods, each twice (`GetOrderStatus`, `GetOrderStatusAsync`, …); Playwright clicks were
intercepted by an overlay; its log printed `Base URL: http://localhost:5901` — the first, HTTP/1.1-only, address.

**A.7 Scalar.AspNetCore.** 2.14.4 (the repo's) has no AsyncAPI, GraphQL or gRPC support; 2.17.11 has
`DocumentType.OpenApi` and `DocumentType.AsyncApi` and `AddAsyncApiDocument`, and nothing for GraphQL or gRPC
(`AddAsyncApiDocument` first appears in 2.16.1 per the research pass; gRPC is an open Scalar discussion, C).
`Bielu.AspNetCore.AsyncApi.UI`'s `MapAsyncApiUi` is `[Obsolete]`, pointing to Scalar.

**A.8 AsyncAPI UI.** `GET /asyncapi` → `200 text/html; charset=utf-8`, title "v1 AsyncAPI Documentation", renders
`AsyncApiStandalone` from `url: '/asyncapi/v1.json'`, assets at `http(s)://<request host>/asyncapi/…`.

**A.9 GraphQL Voyager 2.1.0** (`dist/voyager.standalone.js`, `dist/voyager.css`, React bundled): served statically
with the real API's `/graphql/schema.json` body (123,370 bytes, with the A.4.8 descriptions) as `graphql.json` and the
§10.2 shell, Chromium drew `ReportingQuery` with its seven fields and the types they return, listed the description
"Reporting queries over the business-intelligence database." in the type list, and raised no page error. It accepts
the `{"data":…}` envelope as is.

**A.10 Packages.** `Microsoft.AspNetCore.OpenApi` 10.0.7 → `Microsoft.OpenApi` 2.0.0 (restore: NU1903
GHSA-v5pm-xwqc-g5wc); 10.0.12 → `Microsoft.OpenApi` `[2.12.0, 3.0.0)`.

**A.11 Versions on nuget.org / npm, 2026-09-30.** `Grpc.AspNetCore` and `Grpc.AspNetCore.Server.Reflection` 2.84.0
(`Grpc.AspNetCore.Server` 2.71.0 ships `net6.0`-`net9.0` only; 2.76.0 and 2.84.0 add `net10.0`);
`HotChocolate.AspNetCore` 15.1.15 ships `net8.0` and `net9.0`;
`HotChocolate.AspNetCore` 16.6.7 stable (repo: 15.1.15) — `HotChocolate.Execution.Abstractions` 16.6.7 declares
`IRequestExecutorProvider` and `IRequestExecutorManager`, where 15.1.15's `HotChocolate.Execution` declares
`IRequestExecutorResolver`; `Scalar.AspNetCore` 2.17.11 (repo: 2.14.4);
`Microsoft.AspNetCore.Grpc.JsonTranscoding` 10.0.12; `Microsoft.AspNetCore.Grpc.Swagger` 0.10.11 (net10.0 depends on
JsonTranscoding 10.0.11 and `Swashbuckle.AspNetCore` 6.6.2); `GrpcBrowser` 1.3.4; `GraphQL.Server.Ui.GraphiQL` and
`.Voyager` 8.3.3; npm `graphql-voyager` 2.1.0, `@asyncapi/react-component` 3.2.1, `@scalar/api-reference` 1.72.2,
`graphiql` 5.4.0, `spectaql` 3.0.9.

**A.12 The Pages prototype** (§10.2). The prototype site used:
- the pinned renderers from npm: Scalar 1.72.2's `dist/browser/standalone.js` (4,359,078 bytes; the file the package's
  `browser` field names), AsyncAPI React 3.2.1's standalone bundle and stylesheet, and GraphQL Voyager 2.1.0;
- today's `docs/openapi.json` and `docs/asyncapi.json`, and the real API's `graphql.json`;
- the §10.2 contract bar and landing-page cards;
- a plain `python3 -m http.server`.

In headless Chromium at 1280×800:
- **The pages.** Each UI page drew its bar at the top, 39 px high, and rendered its contract below it: Scalar with its
  sidebar, AsyncAPI, and Voyager on `ReportingQuery`. No page errors, and no failed requests except Scalar's fonts from
  `fonts.scalar.com`, which this sandbox blocks.
- **The JSON links.** Clicking a bar's JSON link opened the raw document in the browser each time: `openapi.json`
  begins `{"openapi": "3.1.1"`, `asyncapi.json` `{"asyncapi": "3.1.0"`, `graphql.json` `{"data": {"__schema"`.
- **The landing page.** It showed each card's "Open …" button beside its document buttons.
- **Content types.** The server sent `.json` as `application/json`, and `.graphql` and `.proto` as
  `application/octet-stream`.
- **The link check.** `check-site-links.py` passed over five pages, ignoring a test-report link that had no file
  locally. With `schema.graphql` removed it failed, naming `index.html -> api/schema.graphql` and
  `api/graphql.html -> ./schema.graphql`. With `grpc.json` removed it failed on `index.html -> api/grpc.json`.

## Appendix B — Reproducing the spike

```bash
# .NET 10 SDK
curl -sSL -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh && bash dotnet-install.sh --channel 10.0 --install-dir ~/.dotnet
export PATH=~/.dotnet:$PATH

# Standalone app: Grpc.AspNetCore + Grpc.AspNetCore.Server.Reflection 2.71.0 (then 2.84.0), HotChocolate.AspNetCore
# and HotChocolate.Data 15.1.15, breakfast.proto with comments and the protoc arguments of §5 item 1.2; Kestrel on 5901 (Http1),
# 5902 (Http2), 5903 (Http1AndHttp2); MapGrpcService, MapGrpcReflectionService, MapGraphQL, and GETs for
# /grpc/v1.json and /graphql/schema.json as in §5 and §8.
ASPNETCORE_ENVIRONMENT=Production dotnet bin/Debug/net10.0/Spike.dll &
curl -s -o /dev/null -w "%{http_code} %{content_type}\n" http://localhost:5901/graphql/schema.graphql
curl -s -H "Content-Type: application/json" --data @introspection.json http://localhost:5901/graphql/   # 400 HC0046
curl -s http://localhost:5901/graphql/schema.json | head -c 300                                             # 200
grpcurl -plaintext localhost:5902 list

# Nitro with no network: loopback only (lo brought up with a SIOCSIFFLAGS ioctl — `ip` was not installed here)
unshare -rn bash -c 'python3 lo-up.py; ASPNETCORE_ENVIRONMENT=Development dotnet bin/Debug/net10.0/Spike.dll & sleep 8;
  curl --noproxy "*" -s -o /dev/null -w "%{http_code}\n" -H "Accept: text/html" http://127.0.0.1:5901/graphql/'
# → 502 with the default ServeMode.Latest; 200 with Tool.ServeMode = GraphQLToolServeMode.Embedded

# GraphQL Voyager over the static document
npm pack graphql-voyager@2.1.0 && tar xzf graphql-voyager-2.1.0.tgz
mkdir -p site/api && cp package/dist/voyager.standalone.js package/dist/voyager.css site/api/
curl -s http://localhost:5901/graphql/schema.json > site/api/graphql.json   # + the §10.2 shell as site/api/graphql.html
python3 -m http.server -d site 5999   # open http://localhost:5999/api/graphql.html

# Real API: a scratch clone with the §5-§8 changes, then
dotnet test --project tests/BreakfastProvider.Tests.Component.xUnit/BreakfastProvider.Tests.Component.xUnit.csproj
```

## Appendix C — Sources from the second research pass

An independent pass, run with the network switched off where it mattered, against the same package versions. It
agreed with every fact in Appendix A and added the Nitro serve-mode finding (verified again here, A.4.6) and the
following. Links are as it reported them.

- HotChocolate 15.1.15 source: `HttpGetSchemaMiddleware.cs` (serves `?sdl`, `/schema`, `/schema.graphql`,
  `?types=`), `Extensions/EndpointRouteBuilderExtensions.cs` (`MapGraphQLSchema(pattern = "/graphql/sdl")`),
  `GraphQLToolOptions.cs` (`ServeMode = Latest`, `Enable = true`), and
  `Extensions/HotChocolateAspNetCoreServiceCollectionExtensions.cs` (introspection off unless `IsDevelopment()`) —
  https://github.com/ChilliCream/graphql-platform/tree/15.1.15/src/HotChocolate/AspNetCore/src/AspNetCore
- HotChocolate docs: https://chillicream.com/docs/hotchocolate/v15/server/endpoints,
  https://chillicream.com/docs/hotchocolate/v15/server/introspection (still shows `AllowIntrospection(false)`, obsolete
  in 15.x), https://chillicream.com/docs/nitro/integrations/hot-chocolate (serve modes), and the 15 → 16 migration
  guide, https://github.com/ChilliCream/graphql-platform/blob/16.6.7/website/content/docs/hotchocolate/migrating/migrate-from-15-to-16.md
- `WebApplicationFactory` runs in Development by default:
  https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
- gRPC reflection v1: https://github.com/grpc/grpc-dotnet/pull/2704 (released in 2.80.0) and
  https://github.com/grpc/grpc-dotnet/blob/v2.84.0/src/Grpc.AspNetCore.Server.Reflection/GrpcReflectionEndpointRouteBuilderExtensions.cs
  (maps v1alpha and v1); Microsoft's advice to map reflection in Development:
  https://learn.microsoft.com/en-us/aspnet/core/grpc/test-tools?view=aspnetcore-10.0
- Grpc.Tools `AdditionalProtocArguments` and `ProtoRoot`: https://github.com/grpc/grpc/blob/master/src/csharp/BUILD-INTEGRATION.md.
  `FileDescriptor.ToProto()` exists from Google.Protobuf 3.20.0, `JsonFormatter.Settings.WithIndentation()` from 3.22.0.
  `JsonFormatter` writes an extension option such as `(google.api.http)` as `"options": {}`.
- `dotnet grpc add-url`: https://learn.microsoft.com/en-us/aspnet/core/grpc/dotnet-grpc?view=aspnetcore-10.0
- `Microsoft.AspNetCore.Grpc.Swagger` deprecated: https://github.com/dotnet/aspnetcore/issues/67134,
  https://github.com/dotnet/aspnetcore/pull/67919, https://learn.microsoft.com/en-us/aspnet/core/grpc/json-transcoding-openapi?view=aspnetcore-10.0;
  the package conflict with the built-in generator: https://github.com/dotnet/aspnetcore/issues/65228. With transcoding
  on, the built-in 10.0.7 document lists none of the transcoded routes. Alternatives it ran:
  `Community.Grpc.SwaggerGen` 10.0.0 (https://github.com/adam8797/Community.Grpc.OpenApi) and
  `protoc-gen-connect-openapi` v0.27.3 (https://github.com/sudorandom/protoc-gen-connect-openapi).
- GrpcBrowser: https://github.com/thomaswormald/grpc-browser (issues #11 and #12 unanswered); the .NET 10 Blazor script
  change: https://github.com/dotnet/aspnetcore/issues/66059. It also found invocation fails over plain HTTP and that the
  package keeps services in static lists.
- Scalar: gRPC not supported (https://github.com/scalar/scalar/discussions/7794); AsyncAPI support
  (https://github.com/scalar/scalar/issues/7080).
- grpcui: https://github.com/fullstorydev/grpcui (v1.5.4, 2026-09-02; the image on `ghcr.io/fullstorydev/grpcui`).
- `GraphQL.Server.Ui.GraphiQL` / `.Voyager` 8.3.3 load `graphiql@3.2.0` from unpkg and `graphql-voyager@1.3.0` from
  jsDelivr.
- Microsoft.OpenApi advisory: https://github.com/advisories/GHSA-v5pm-xwqc-g5wc (CVE-2026-49451).
- No gRPC binding in AsyncAPI: https://github.com/asyncapi/bindings.
