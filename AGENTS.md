# FastGeography

Hosted Blazor WASM geography game: ASP.NET Core server serves the Client and JSON APIs. .NET 8. Solution: `FastGeography.sln`.

## Layout

| Project | Role |
|---|---|
| `src/FastGeography.Client` | Blazor WASM UI (Razor pages, cookie auth, localization) |
| `src/FastGeography.Server` | Host, Identity cookies, controllers, SignalR `GameHub`, EF Core |
| `src/FastGeography.Shared` | DTOs, game rules, alphabet, language helpers — shared by Client and Server |
| `src/FastGeography.AppHost` | Aspire host (PostgreSQL). Optional for local UI work |
| `tst/FastGeography.Tests.Unit` | Domain/service unit tests |
| `tst/FastGeography.Tests.Integration` | `WebApplicationFactory` API/hub tests (fakes, in-memory DB) |
| `tst/FastGeography.Tests.E2E` | Playwright; tests are skipped in CI and need a running server |

Run the app from the **Server** (it hosts WASM). Default URLs: `https://localhost:7002` and `http://localhost:5261`.

```
dotnet run --project src/FastGeography.Server
```

Without a Postgres connection string the Server uses InMemory EF. Auth and ranked data do not persist across restarts. For Postgres, run Aspire AppHost.

Do not run the Client project as the app host.

## Tests

CI (`.github/workflows/ci.yml`) runs Unit + Integration only.

```
dotnet test tst/FastGeography.Tests.Unit
dotnet test tst/FastGeography.Tests.Integration
dotnet test tst/FastGeography.Tests.Integration --filter FullyQualifiedName~AuthTests
```

Pick the slice that matches the change:

- Auth / cookies / email → `AuthTests`
- SignalR / rooms → `GameHubTests`
- Ranked solo → `RankedSoloTests`
- Leaderboard → `LeaderboardTests`
- Geocoding / catalog → `CatalogGeocodingServiceTests`, `GeocodingAdapterTests`, `WellKnownToponymsCatalogTests`
- Destination stories → `DestinationStoriesTests` and unit tests under `tst/FastGeography.Tests.Unit`

Integration tests use `TestAppFixture`: in-memory DB, `FakeGeocodingService`, `FakeEmailSender`, fake stories/images. Do not call real geocoding, SMTP, or LLM APIs from tests.

E2E facts in `tst/FastGeography.Tests.E2E` are skipped. For UI verification, use the Playwright MCP against a running Server at `https://localhost:7002`. Wait for Blazor to hydrate before clicking.

## Product constraints

- Cookie Identity (`SameSite=Strict`). `/api` and `/hubs` return 401/403 instead of redirecting.
- Client `HttpClient` uses `BaseAddress` of the host and sends cookies (`CookieHandler`).
- Request/response contracts live in `FastGeography.Shared/Dtos`. Change Client, Server, and Shared together.
- UI copy: `src/FastGeography.Client/Resources/UiStrings.resx` (en) and `UiStrings.mk.resx` (mk). Add both. Language is stored as `fg_lang` in localStorage (`en` / `mk`).
- SignalR hub: `/hubs/game`.
- Routes: `/` and `/fastgeography` (game), `/login`, `/register`, `/forgot-password`, `/reset-password`, `/how-to-play`, `/ranked`, `/multiplayer`, `/multiplayer/{RoomCode}`, `/scoreboard`, `/scoreboard/me`.

## Agent workflow

Parent Agent is the orchestrator. The human chooses the model and when to stop.

- `/developer` implements. Do not claim done.
- `/qa` or `/verify-feature` runs tests and browser checks, then reports evidence.
- `/debugger` only for failures (red tests, stack traces, UI repros).
- Do not unbounded-loop implement → test → fix. After verification, report and wait.
- Do not commit secrets (`appsettings` API keys, SMTP passwords, user secrets).
