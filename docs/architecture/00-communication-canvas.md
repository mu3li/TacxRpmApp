# 📐 Architecture Communication Canvas

<!-- PRIMARY quick-overview document. Keep this to ONE page. Update it as the system evolves. -->

| | |
|---|---|
| **System** | TacxRpmApp — Android app providing three one-tap resistance presets for a Tacx NEO 2T during indoor RPM/spin classes |
| **Owner** | mu3li (solo developer and sole user) |
| **Last Updated** | 2026-08-31 |

---

| Business Context | Quality Goals |
|---|---|
| RPM/spin classes require instant switching between uphill, cruise and downhill resistance while riding. No commercial trainer app offers a three-level one-tap interface, so the rider cannot follow class intensity cues on a Tacx NEO 2T. The app runs on a single Android phone mounted on the handlebars. | 1. No recorded ride is ever lost — to a crash, a BLE dropout, or Android backgrounding |
| | 2. The trainer link survives a continuous 60-minute class |
| | 3. Protocol decoding is verifiable off-device, without the trainer |

| Key Stakeholders | Solution Strategy |
|---|---|
| Rider / Owner — mu3li — presets must apply instantly mid-class without looking at the screen | • Talk to the trainer directly over ANT+ FE-C tunnelled through BLE; do not rely on FTMS |
| | • The app owns the trainer connection for the whole session |
| | • No accounts, no cloud, no network calls — data leaves the phone as a file the user shares |

| Key Building Blocks | Key Interfaces |
|---|---|
| `TacxRpmApp.Core` (net8.0, Android-free) — FE-C and HR decoding, `TrainerService`, `HeartRateMonitor`, `RpmSettings`, ViewModels | Tacx FE-C over BLE (`fec1`/`fec2`/`fec3`) — bidirectional |
| `ITrainerTransport` / `IHeartRateTransport` — the BLE boundary Core owns; real implementations in the app, fakes in tests | BLE Heart Rate profile (`0x180D`/`0x2A37`) — inbound |
| `TacxRpmApp` (net8.0-android) — XAML pages bound to ViewModels, `AndroidTrainerTransport`, `AndroidHeartRateTransport`, `MauiPreferenceStore` | Standard FTMS (`0x1826`) — detected during discovery, **not used for control** |
| `TacxRpmApp.Tests` (net8.0, xUnit) — 36 tests over protocol, settings, service and view models | See `04-interfaces.md` |

| Key Architecture Decisions | Key Risks & Technical Debt |
|---|---|
| `DCAR-002` — Android-free core with a fakeable BLE transport. **Draft**, pending sign-off. | **No CI** — nothing prevents a `using Android.*` from re-entering Core; the only guard is a local `dotnet test` |
| | `HeartRateMonitor` is implemented and DI-registered, but no page surfaces heart rate |
| | Trainer telemetry (FE-C pages `0x10`, `0x19`) arrives and is discarded; only `0x36` and `0x47` are parsed |
| | No foreground service — a session cannot survive screen-off or backgrounding |
| | No disconnect or reconnect lifecycle; a dropped link requires restarting the flow |
| | Behaviour parity after the restructure is verified by a manual ride, not by an automated check |
| | Single hardware target — verified only against one NEO 2T on one phone |

| Technology Stack |
|---|
| **Runtime:** .NET 8, C# with nullable enabled. Three projects: `TacxRpmApp.Core` (net8.0), `TacxRpmApp` (MAUI, `net8.0-android`), `TacxRpmApp.Tests` (net8.0) |
| **UI:** MAUI XAML bound to `CommunityToolkit.Mvvm` ViewModels; `CommunityToolkit.Maui` for behaviours. |
| **Data:** MAUI `Preferences` (key/value) behind `IPreferenceStore`. No database. |
| **Messaging:** None. BLE GATT notifications only. |
| **Testing:** xUnit with hand-written fakes — no mocking framework. |
| **Infrastructure:** Android phone. No server, no cloud. |
| **CI/CD:** None. Local `dotnet build -f net8.0-android` and `dotnet test`. |
