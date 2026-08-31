# 8. Cross-Cutting Concerns

<!-- ARC42 §8 — Brief format. Document architectural approaches that span multiple components. -->

| Concern | Approach | Components Affected |
|---------|----------|-------------------|
| **Security / AuthN** | None, by design. No accounts, no network, no credentials, no personal data leaves the device. BLE link-layer only — any nearby app may claim the trainer first. | All |
| **Authorization** | Not applicable — single-user device-local app. |— |
| **Logging & Monitoring** | `Android.Util.Log` with the tag `TacxRpmApp`, confined to the Android transports — Core logs nothing and has no logging dependency. Every FE-C and heart-rate packet is logged as hex. No log file, no retention, no crash reporting — diagnosis requires a USB-attached `logcat`. | `AndroidTrainerTransport`, `AndroidHeartRateTransport` |
| **Error Handling** | Failures become human-readable Portuguese strings on `TrainerService.LastConnectionStatus` / `HeartRateMonitor.LastConnectionStatus` and reach the UI through ViewModel properties. Exceptions are caught broadly and converted to status text rather than propagated. No structured error type. | Core services → ViewModels → pages |
| **Persistence** | Key/value only, behind `IPreferenceStore`; `MauiPreferenceStore` wraps MAUI `Preferences`, tests use `FakePreferenceStore`. Original preference keys preserved, so saved presets survive the restructure. No database, no files, no ride data. Clearing app data resets to defaults. | `RpmSettings`, `MauiPreferenceStore` |
| **Configuration** | None. Timeouts, retry counts and scan duration are constants in `TrainerService` / `HeartRateMonitor`; GATT UUIDs are constants in the Android transports. | Core services, Android transports |
| **i18n / l10n** | None. UI strings and status messages are hard-coded Portuguese; identifiers are English. No resource files. | All pages, all services |
| **Testing Strategy** | xUnit project `TacxRpmApp.Tests` (net8.0), 36 tests covering FE-C packet building and parsing, HR measurement decoding, settings clamping and persistence, `TrainerService` driven by `FakeTrainerTransport`, and ViewModel state. Runs with no Android SDK present. **No CI** — the Android-free constraint on Core is enforced only by running the tests locally. On-device behaviour is still verified by a manual parity ride. | `TacxRpmApp.Core`, `TacxRpmApp.Tests` |
| **Resilience** | Connection: one retry after 700 ms, 12-second timeout per attempt. Nothing else — no reconnect after an established link drops, no backoff, no disconnect path, no state recovery. | `TrainerService` |
| **Concurrency** | GATT callbacks arrive on Android binder threads and are re-raised as events straight into Core services, which mutate their state without guards. `_discoveredDevices` is lock-guarded inside each transport; nothing else is. The transports do not marshal to the main thread. | Android transports, `TrainerService`, `HeartRateMonitor` |
| **Permissions** | Requested at runtime when the user taps connect. Manifest declares `BLUETOOTH`, `BLUETOOTH_ADMIN`, `BLUETOOTH_CONNECT`, `BLUETOOTH_SCAN` (`neverForLocation`), `ACCESS_FINE_LOCATION`. | `MainPage`, `AndroidManifest.xml` |
| **Background execution** | None. No foreground service and no wake lock, so Android may throttle or suspend BLE callbacks when the screen is off or the app is backgrounded. | All |
