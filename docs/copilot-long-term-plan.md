# Tacx NEO 2T Custom Training App — Development Plan

## Goal

Build a custom application that:

1. Connects directly to a Tacx NEO 2T.
2. Reads power, speed, cadence and trainer state.
3. Controls resistance / ERG mode.
4. Runs custom workouts.
5. Records the complete training session locally.
6. Exports workouts as FIT.
7. Uploads completed workouts to Strava.
8. Eventually supports advanced Tacx-specific functionality.
9. Optionally acts as a bridge to other training applications.

---

# 1. Recommended Architecture

```text
                    ┌──────────────────────┐
                    │      Tacx NEO 2T     │
                    │                      │
                    │ Power                │
                    │ Speed                │
                    │ Cadence              │
                    │ Resistance           │
                    │ Trainer control      │
                    └──────────┬───────────┘
                               │
                         BLE / ANT+ FE-C
                               │
                               ▼
                    ┌──────────────────────┐
                    │       YOUR APP       │
                    │                      │
                    │ Trainer connection   │
                    │ Workout engine       │
                    │ ERG / resistance     │
                    │ Data recorder        │
                    │ FIT exporter         │
                    │ Sensor manager       │
                    └──────────┬───────────┘
                               │
                 ┌─────────────┴─────────────┐
                 │                           │
                 ▼                           ▼
        ┌─────────────────┐        ┌─────────────────┐
        │ Local storage   │        │   Strava API    │
        │                 │        │                 │
        │ Raw samples     │        │ OAuth            │
        │ Workout data    │        │ FIT upload       │
        │ FIT files       │        │ Activity status  │
        └─────────────────┘        └─────────────────┘
```
The app should own the workout session.

Do NOT make Strava or Garmin Connect the primary data recorder.

# 2. Technology Strategy
Trainer communication

Start with Bluetooth Low Energy.

Prioritize:

    Fitness Machine Service (FTMS)
    Cycling Power Service
    Cycling Speed and Cadence
    Tacx-specific services only when necessary

Also consider ANT+ FE-C for desktop applications.
Data format

Use FIT as the primary export format.

Keep the internal representation independent from FIT.
´´´text
Trainer / sensors
       ↓
Internal data model
       ↓
FIT exporter
       ↓
.fit file
´´´
This allows future support for:

    Strava
    Garmin
    TrainingPeaks
    intervals.icu
    other services
    manual file export

# 3. Phase 1 — Project Skeleton
Objectives

Create the application structure before touching trainer-specific code.

Implement:

    Application shell
    Settings
    Workout screen
    Trainer connection screen
    Sensor screen
    Ride history
    Storage layer
    Logging system

Suggested modules:
´´´text
app/
├── ui/
├── trainer/
├── sensors/
├── workout/
├── recording/
├── fit/
├── integrations/
│   └── strava/
├── storage/
└── diagnostics/
´´´
Important design rule

Keep trainer communication behind an abstraction.

Example:
´´´text
Trainer
├── connect()
├── disconnect()
├── start()
├── stop()
├── setTargetPower()
├── setResistance()
└── telemetry stream
´´´
The rest of the application should not care whether the trainer uses BLE or ANT+.
# 4. Phase 2 — Connect to NEO 2T
First milestone

Connect to the NEO 2T and reliably receive telemetry.

Implement:

    BLE scanning
    Device identification
    Connection
    Service discovery
    Characteristic discovery
    Notifications
    Reconnection
    Connection state

Capture:

timestamp
power
speed
cadence
trainer state

Diagnostics

Create a debug screen showing:

Trainer: NEO 2T
Connection: Connected

Power:      247 W
Speed:      34.2 km/h
Cadence:    91 RPM

Packets/sec: 1
Connection quality: Good

Do not build workouts yet.

First make the connection extremely reliable.
# 5. Phase 3 — Trainer Control

Implement trainer control after telemetry is stable.

Start with:
ERG mode

Target power = 250 W

The app tells the trainer to maintain the requested power.

Support:

    Set target watts
    Increase/decrease target watts
    Start ERG
    Stop ERG
    Pause
    Resume

Resistance mode

Support manual resistance.

Example:

Resistance: 35%

Future simulation mode

Eventually support:

gradient
weight
rolling resistance
aerodynamic drag
wind

But keep this separate from the basic ERG implementation.
# 6. Phase 4 — Workout Engine

Create a workout abstraction.

Example:

Workout
├── warmup
├── interval
├── recovery
├── interval
├── recovery
└── cooldown

Each step should contain:

duration
target power
mode
cadence target (optional)

Example:

{
  "duration": 300,
  "mode": "ERG",
  "targetPower": 250
}

The workout engine should be independent from the trainer implementation.

Workout Engine
      ↓
Trainer Controller
      ↓
NEO 2T

This is important because it allows workouts to be tested without a physical trainer.
# 7. Phase 5 — Data Recording

Create a recording session whenever a workout starts.

Record samples at approximately 1 Hz initially.

Example:

{
  "timestamp": "2026-08-20T18:42:15Z",
  "power": 247,
  "speed": 34.2,
  "cadence": 91,
  "heartRate": 151,
  "targetPower": 250,
  "distance": 12.43
}

Keep raw measurements.

Do not only store averages.
Suggested model

Ride
├── id
├── startTime
├── endTime
├── duration
├── distance
├── energy
├── averagePower
├── normalizedPower
├── averageCadence
├── averageHeartRate
├── samples[]
└── workout metadata

# 8. Sensor Manager

Create a generic sensor abstraction.

SensorManager
│
├── Trainer
│   ├── power
│   ├── speed
│   └── cadence
│
├── Heart Rate Sensor
│   └── heart rate
│
└── Optional GPS

This makes it possible to support external HR sensors without coupling the workout engine to a particular manufacturer.
# 9. Phase 6 — Local Storage

Store completed rides locally.

Recommended strategy:

Database
    ↓
Ride metadata

File storage
    ↓
Raw recording
    ↓
FIT file

A ride should survive:

    app crash
    phone/computer restart
    loss of internet
    failed Strava upload

The workout must never depend on Strava being online.
# 10. Phase 7 — FIT Export

Implement a FIT exporter.

Pipeline:

Ride
 ↓
FIT encoder
 ↓
.fit

The FIT should contain, where available:

    timestamp
    power
    speed
    cadence
    heart rate
    distance
    calories
    elapsed time
    workout information
    lap information

Keep FIT generation as a separate module:

fit/
├── FitEncoder
├── FitActivity
├── FitLap
└── FitRecord

# 11. Phase 8 — Strava Integration

Use Strava OAuth.

Required flow:

User
 ↓
Connect Strava
 ↓
OAuth authorization
 ↓
Access token
 ↓
Refresh token

Request the permission necessary to upload activities.

After a ride:

Ride complete
      ↓
Generate FIT
      ↓
Save FIT locally
      ↓
Add to upload queue
      ↓
Upload to Strava
      ↓
Poll upload status
      ↓
Mark ride as synced

Important

Never delete the local FIT just because Strava upload succeeded.

The local FIT is useful as the user's portable backup.
# 12. Upload Queue

Create a durable upload queue.

Example:

UploadQueue

Ride #123
Status: pending

Ride #124
Status: uploaded

Ride #125
Status: failed
Retry: 3

Handle:

    no internet
    expired OAuth token
    server errors
    duplicate uploads
    app restart
    retry with backoff

This will make the app much more reliable.
# 13. Phase 9 — Workout UI

Only after the underlying system works should the main training UI be built.

Suggested screen:

┌───────────────────────────────────────┐
│              250 W                    │
│                                       │
│         ███████████████               │
│                                       │
│ Power       247 W                     │
│ Cadence      91 RPM                   │
│ Speed       34.2 km/h                 │
│ Heart Rate  151 bpm                   │
│                                       │
│ Target      250 W                     │
│                                       │
│      [-]     ERG      [+]             │
│                                       │
│          32:14 elapsed                │
└───────────────────────────────────────┘

Later add:

    power graph
    cadence graph
    HR graph
    workout timeline
    interval countdown
    lap buttons
    resistance controls
    custom workout editor

# 14. Phase 10 — Advanced Tacx Features

Do this only after the standard protocol implementation is stable.

Investigate Garmin/Tacx-specific functionality for:

    Road Feel
    virtual shifting/gearing
    advanced resistance control
    proprietary trainer features
    advanced simulation

These may require Tacx-specific documentation/access rather than only standard FTMS.

Do not make proprietary features a dependency for the core application.
# 15. Optional — External App Bridge

This is a separate project phase.

Potential architecture:

                  NEO 2T
                    │
                    ▼
                YOUR APP
                    │
          ┌─────────┴─────────┐
          ▼                   ▼
      Recording          External apps
                              │
                    ┌─────────┴─────────┐
                    ▼                   ▼
                  App A               App B

The app could potentially expose trainer telemetry to another application.

However, this is considerably more complicated than uploading completed workouts.

Start with:

NEO 2T → Your App → FIT → Strava

before attempting:

NEO 2T → Your App → Other live applications

# 16. Testing Strategy
Unit tests

Test:

    workout calculations
    interval transitions
    FIT generation
    distance calculation
    power calculations
    upload queue
    OAuth state
    retry logic

Hardware tests

Test:

    connect
    disconnect
    reconnect
    trainer powered off
    Bluetooth temporarily unavailable
    workout pause
    app backgrounding
    app crash
    trainer restart

Recording tests

Verify that a ride can be recovered after:

App crash
Computer restart
Phone restart
Internet loss
Bluetooth disconnect
Strava outage

# 17. MVP Definition

The first usable version should only need:
Trainer

    Connect to NEO 2T
    Read power
    Read speed
    Read cadence
    Set ERG target
    Set resistance
    Pause/resume

Workout

    Start workout
    Stop workout
    Basic interval structure
    Live target power
    Workout timer

Recording

    Record 1 Hz samples
    Save locally
    Recover from connection loss
    View completed rides

Export

    Generate FIT
    Save FIT
    Import/open FIT independently

Strava

    OAuth
    Upload FIT
    Track upload status
    Retry failed uploads

Do not add advanced graphics or social features before this works.
# 18. Recommended Development Order

´´´text
1. Project skeleton
       ↓
2. BLE discovery
       ↓
3. NEO 2T telemetry
       ↓
4. ERG control
       ↓
5. Resistance control
       ↓
6. Workout engine
       ↓
7. Local recording
       ↓
8. FIT export
       ↓
9. Strava OAuth
       ↓
10. Strava upload
       ↓
11. Robust reconnection
       ↓
12. UI polish
       ↓
13. Advanced Tacx features
       ↓
14. External-app bridge
´´´
# 19. Important Architectural Principles
Principle 1 — Your app owns the ride

Do not depend on Strava, Garmin Connect, or another service for recording.
Principle 2 — Keep raw data

Never throw away the original trainer samples.
Principle 3 — Separate control from recording

TrainerController
WorkoutEngine
RideRecorder
FitExporter
StravaClient

should be independent modules.
Principle 4 — Make integrations replaceable

Strava should be one destination, not the foundation.

Ride
 ├── FIT
 ├── Strava
 ├── Garmin
 ├── TrainingPeaks
 └── local archive

Principle 5 — Hardware failures should not lose workouts

The recorder should be designed for failure.
# 20. Suggested Long-Term Architecture

                    ┌──────────────────────┐
                    │      Workout UI      │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │    Workout Engine    │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │  Trainer Controller  │
                    └──────────┬───────────┘
                               │
                 ┌─────────────┴─────────────┐
                 │                           │
                 ▼                           ▼
          BLE / FTMS                    ANT+ FE-C
                 │                           │
                 └─────────────┬─────────────┘
                               ▼
                           NEO 2T

                            Telemetry
                                │
                                ▼
                            ┌──────────────────┐
                            │ Sensor Manager   │
                            └────────┬─────────┘
                                    │
                                    ▼
                            ┌──────────────────┐
                            │   Ride Recorder  │
                            └────────┬─────────┘
                                    │
                                    ▼
                            ┌──────────────────┐
                            │    Ride Store    │
                            └────────┬─────────┘
                                    │
                                    ├──────────────► FIT Export
                                    │                    │
                                    │                    ├── Local file
                                    │                    └── Strava
                                    │
                                    └──────────────► Other integrations

# 21. First Concrete Milestone

The first thing to build should be a small diagnostic application.

It should do exactly this:

1. Scan for NEO 2T
2. Connect
3. Discover BLE services
4. Subscribe to telemetry
5. Display:
   - power
   - speed
   - cadence
6. Allow:
   - ERG target change
   - resistance change
7. Log everything to a file
8. Disconnect cleanly

Do not start with the full training UI.

Once this diagnostic application reliably controls the NEO 2T, the rest of the project becomes much easier.
# 22. External Documentation

Garmin/Tacx developer documentation:

https://developer.garmin.com/tacx-trainer/

Strava API documentation:

https://developers.strava.com/docs/

Strava upload documentation:

https://developers.strava.com/docs/uploads/
Final Target

The finished application should behave like this:

                  USER
                   │
                   ▼
             YOUR APP
                   │
          ┌────────┴────────┐
          │                 │
       CONTROL           RECORD
          │                 │
          ▼                 ▼
       NEO 2T            RIDE DATA
                              │
                    ┌─────────┴─────────┐
                    ▼                   ▼
                  FIT                DATABASE
                    │
                    ▼
                 STRAVA

The core philosophy is:

Control the trainer yourself, own the raw training data yourself, and treat Strava as an export/integration target.