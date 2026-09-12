---
name: verify-feature
description: Verifies a FastGeography feature with the matching unit/integration tests and a Playwright browser pass. Use when the user asks to test, verify, or check a feature after implementation, or invokes /verify-feature.
disable-model-invocation: true
---

# Verify a FastGeography feature

Run this after implementation. Report evidence. Do not start architecture analysis. Do not keep looping fixes unless the user asks.

Prefer the `qa` subagent for the test + browser pass (`readonly`). Use `debugger` only if the user asks to fix a failure from this run.

## Checklist

```
Verify-feature:
- [ ] Map the change to a test slice
- [ ] Run that slice
- [ ] Browser-exercise the user-visible path (if UI)
- [ ] Report passed / failed / untested
```

## 1. Map the change

| Area | Tests | UI route |
|---|---|---|
| Auth, cookies, reset email | `AuthTests` | `/login`, `/register`, `/forgot-password`, `/reset-password` |
| Solo game table | `GameTests`, `AlphabetTests` | `/` or `/fastgeography` |
| Ranked | `RankedSoloTests` | `/ranked` |
| Multiplayer / SignalR | `GameHubTests` | `/multiplayer`, `/multiplayer/{RoomCode}` |
| Leaderboard | `LeaderboardTests` | `/scoreboard`, `/scoreboard/me` |
| How to play / copy | (UI only) | `/how-to-play` plus both `.resx` files |
| Geocoding / catalog | `CatalogGeocodingServiceTests`, `GeocodingAdapterTests` | game answer flow |
| Destination stories | `DestinationStoriesTests` | game table stories UI |

If the change is Server-only with no page, skip the browser step and say so.

## 2. Run tests

```
dotnet test tst/FastGeography.Tests.Unit --filter FullyQualifiedName~NameHere
dotnet test tst/FastGeography.Tests.Integration --filter FullyQualifiedName~AuthTests
```

Use the matching project. Do not run the full solution unless the change is cross-cutting.

Integration tests already fake geocoding, email, stories, and images via `TestAppFixture`. Do not require live API keys.

`tst/FastGeography.Tests.E2E` is skipped in CI and locally by default. Do not run it as the UI check.

## 3. Browser pass (UI)

App: `https://localhost:7002` (Server hosts WASM). If it is down:

```
dotnet run --project src/FastGeography.Server
```

Use Playwright MCP. Wait for Blazor to hydrate (buttons/inputs present) before clicking.

Exercise the changed flow end to end the way a user would. One screenshot is not verification. Check a nearby path that shares state (nav, login redirect, `fg_lang`) when the change could affect it.

## 4. Report

```
Passed:
- {command or flow} — {evidence}

Failed:
- {command or flow} — {what broke}

Untested:
- {gap and why}
```

Stop. If anything failed, do not implement a fix unless the user asks. Cap any requested fix cycle at 3 retries, then report remaining failures.
