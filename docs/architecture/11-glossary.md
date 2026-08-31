# 11. Glossary

<!-- ARC42 §11 — Define terms that have specific meaning in this system's context. -->

| Term | Definition | Context |
|------|-----------|---------|
| ANT+ | Wireless sensor protocol widely used in cycling and fitness equipment | Origin of the FE-C message format used here |
| FE-C | Fitness Equipment Control — the ANT+ profile for controlling trainers | Tunnelled through BLE by the NEO 2T; the app's control path |
| FTMS | Fitness Machine Service — the Bluetooth SIG standard for trainer control (`0x1826`) | Discovered on the trainer but **not** used for control |
| GATT | Generic Attribute Profile — the BLE service/characteristic model | How all device communication is structured |
| CCCD | Client Characteristic Configuration Descriptor (`0x2902`) | Written to enable notifications on `fec2` and `0x2A37` |
| Central / Peripheral | BLE roles: the central initiates connections, the peripheral advertises | The app is central only |
| Page | A numbered FE-C message type, e.g. `0x10` general data, `0x19` trainer data | Determines how a 13-byte notification is decoded |
| Basic resistance | FE-C command `0x30` — a constant braking force independent of speed | What the three presets send |
| ERG mode | Trainer holds a fixed target power regardless of cadence (FE-C `0x31`) | Not implemented |
| Isotonic | Constant resistance regardless of speed | Under consideration to make the trainer feel like a static bike |
| Virtual momentum | The NEO's simulated flywheel inertia | The behaviour the rider wants to remove |
| Preset | One of three stored resistance values — uphill, cruise, downhill | The app's core feature |
| Passo | Portuguese for "step" — the `+`/`-` increment | UI label for `RpmSettings.Increment` |
| RPM class | Indoor group cycling class with instructor-led intensity cues | The usage context the app is designed around |
| TCX | Training Center XML — an XML activity file format accepted by Strava | Planned export format |
| FIT | Flexible and Interoperable Data Transfer — Garmin's binary activity format | The cycling standard; a possible later export target |
| NDJSON | Newline-delimited JSON — one JSON object per line | Planned crash-safe raw sample store |
| Health Connect | Android platform API for sharing health data between apps | Possible future route into Samsung Health |
| DCAR | Decision Centric Architecture Review — short decision record format | Used in `decisions/` for hard-to-reverse choices |
| ADR | Architecture Decision Record — lighter-weight decision record | Used for reversible, tactical decisions |
