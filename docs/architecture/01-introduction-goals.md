# 1. Introduction and Goals

<!-- ARC42 §1 — Brief format. Keep this under one page. -->

## Purpose

TacxRpmApp lets a rider switch a Tacx NEO 2T between three preconfigured resistance levels — uphill, cruise and downhill — with a single tap, so the trainer can follow the intensity cues of an indoor RPM/spin class. It exists because no commercial trainer application offers that three-level interface. Its second purpose is to record the session so the ride can be uploaded to external services afterwards.

## Quality Goals

| Priority | Quality Goal | Description |
|----------|-------------|-------------|
| 1 | Data durability | A recorded session survives an app crash, a BLE dropout, Android backgrounding the app, and a phone reboot. Nothing deletes the raw samples. |
| 2 | Connection reliability | The trainer link is established and held for a continuous 60-minute class, and recovers automatically from a dropout without splitting the session. |
| 3 | Testability | Protocol decoding and file generation are verifiable without the trainer or a phone. |
| 4 | Responsiveness | A preset tap changes trainer resistance without a perceptible delay while riding. |

Priorities 1–3 describe the target state. Priority 4 is met today. See `10-quality-requirements.md` for current status per scenario.

## Stakeholders

| Role | Contact | Key Expectations |
|------|---------|-----------------|
| Rider / Owner / Developer | mu3li | Presets apply instantly mid-class without needing to look at the screen; rides end up in Strava and, eventually, Samsung Health |
| Class instructor (indirect) | — | Sets the intensity cues the presets are configured to match; no interaction with the app |
