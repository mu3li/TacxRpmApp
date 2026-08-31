# Android-free core with a fakeable BLE transport

**Date:** 2026-08-31
**Status:** Draft
**Selected:** Android-free core with transport interfaces

---

## Problem

Protocol decoding lived inside `Android.Bluetooth`-typed classes, so no test runner could load it. The top quality goals — never lose a ride, survive a dropout, verify decoding off-device — could not be verified at all.

---

## Decision

Split the codebase into three projects:

- `TacxRpmApp.Core` (net8.0) — protocol encode/decode, `TrainerService`, `HeartRateMonitor`, `RpmSettings`, ViewModels. No Android reference.
- `TacxRpmApp` (net8.0-android) — MAUI pages bound to Core ViewModels, plus the Android implementations of the ports.
- `TacxRpmApp.Tests` (net8.0) — xUnit, driving Core through fakes.

Core owns the platform boundary as interfaces it defines: `ITrainerTransport`, `IHeartRateTransport`, `IPreferenceStore`. The Android app supplies the real implementations; the test project supplies `FakeTrainerTransport` and `FakePreferenceStore`, which replay captured packets and record writes. Byte-level encoding and decoding are pure static functions with no dependencies.

---

## Alternatives Considered

### Alternative 1: Android-free core with transport interfaces

| | |
|---|---|
| **Positive impact** | Protocol and orchestration run in a plain test runner with no Android SDK; dropouts and malformed packets are reproducible on demand; the FE-C byte layout is pinned by tests |
| **Negative impact** | One interface hop between service and GATT; three projects instead of one; nothing but a local test run stops Android types from creeping back into Core |

Selected. It is the only option that makes byte-level behaviour verifiable without hardware, which is what the quality goals require.

### Alternative 2: Instrumented on-device tests

| | |
|---|---|
| **Positive impact** | Exercises the real GATT stack and the real trainer |
| **Negative impact** | Slow, requires the phone and the NEO 2T attached, and cannot reproduce a dropout or a corrupt packet on demand |

Rejected. It cannot cover the failure modes the quality goals are about.

### Alternative 3: Leave as-is and test manually

| | |
|---|---|
| **Positive impact** | No restructuring, no regression risk |
| **Negative impact** | Cannot cover byte-level parsing, which is exactly where silent errors live; every change costs a ride to validate |

Rejected.

---

## Serves

<!-- Quality goals are recorded in 00-communication-canvas.md; .cliq/quality-goals does not exist yet,
     so the IDs below refer to the canvas list by position. -->

| ID | Type | Note |
|----|------|------|
| QG-1 | Quality Goal | Recording logic can be tested against replayed dropouts before it ever reaches a ride |
| QG-3 | Quality Goal | Protocol decoding is verifiable off-device — this is the goal the decision exists for |

---

## Metadata

- **Related DCARs:** none
- **Affected Systems:** `TacxRpmApp.Core`, `TacxRpmApp` (Android app), `TacxRpmApp.Tests`
- **ARC42 Sections Impacted:** §5 Building Block View, §8 Cross-Cutting Concerns
- **Interface changes:** none external. Introduces internal `ITrainerTransport`, `IHeartRateTransport` and `IPreferenceStore`.
