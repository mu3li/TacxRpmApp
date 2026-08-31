# Tacx RPM App: Next Steps

The next step is to verify the Android app end to end, then investigate the Tacx command protocol. The app currently scans and connects, but it does not yet change resistance.

## 1. Run The App On The Emulator

The emulator is useful for checking navigation, settings, and button behavior.

From the project folder:

```bash
cd /Users/cs/Documents/repos/TacxRpmApp

export JAVA_HOME=$(/usr/libexec/java_home -v 17)
export ANDROID_HOME="$HOME/Library/Android/sdk"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export PATH="$JAVA_HOME/bin:$ANDROID_HOME/platform-tools:$PATH"

adb devices
dotnet build -t:Run -f net8.0-android
```

Verify:

- The app launches.
- `CONFIG` opens correctly.
- Settings save and persist.
- `+` and `-` change values.
- Values cannot become negative.
- The three preset buttons change selected colors.

The project is Android-only, so use the Android target shown above for builds and deployment.

The current protocol research is recorded in [protocol-findings.md](protocol-findings.md). It points toward Tacx FE-C over BLE rather than standard FTMS.

The first FE-C diagnostic and basic-resistance command slices are now implemented. The app discovers Tacx `fec2`, enables notifications, logs received packets, and sends basic resistance through `fec3`. The command has been verified on the physical NEO 2T with command status `success`.

## 2. Test The Real Android Phone

The emulator cannot reliably test the Tacx Bluetooth connection.

1. Enable Developer options on the phone.
2. Enable USB debugging.
3. Connect the phone by USB.
4. Accept the debugging prompt.
5. Run:

```bash
adb devices
```

The phone must show as `device`.

Then install and launch:

```bash
dotnet build -t:Run -f net8.0-android
```

On the phone:

1. Enable Bluetooth.
2. Wake the Tacx NEO 2T.
3. Close Zwift, Garmin, Tacx Training, and other trainer apps.
4. Grant Bluetooth permission to TacxRpmApp.
5. Tap `CONECTAR`.
6. Tap `PESQUISAR DISPOSITIVOS`.
7. Select the Tacx device.
8. Confirm whether the app connects successfully.

While testing, collect the FE-C log output from a second terminal:

```bash
adb logcat -c
adb logcat -s TacxRpmApp:D
```

After connecting, look for lines similar to:

```text
FE-C notifications: local=True, descriptor=True
FE-C notification: A4...
```

Stop the log with `Ctrl+C` and save the complete hexadecimal notification lines. Do not press any write controls in nRF Connect and do not expect resistance to change yet.

At this stage, a successful test means that the app finds the trainer, returns to the main page showing `CONECTADO`, changes resistance after applying a preset, and produces a successful page `0x47` command-status response in logcat.

## 3. Record The Bluetooth Results

Write down:

- Phone model and Android version
- Tacx model and firmware, if known
- Device name shown in the scan
- Whether the FTMS service appears
- Whether the Control Point characteristic appears
- Whether the proprietary Tacx service appears
- Exact connection status or error
- Whether another app had previously connected to the trainer

This information determines which command path should be implemented.

## 4. Inspect The Protocol Before Writing More Commands

Use a BLE inspection app such as nRF Connect on the Android phone. With other trainer apps closed, inspect the Tacx device and record:

- Service UUIDs
- Characteristic UUIDs
- Read, write, write-without-response, and notify properties
- Notification descriptors
- Any response received after connecting

The basic-resistance command is now verified. Read [protocol-findings.md](protocol-findings.md) before implementing target power, simulation, calibration, or other FE-C commands.

## 5. Finalize Resistance Semantics

The current command uses FE-C basic-resistance values from `0` to `200`. Before changing the preset defaults or labels:

1. Confirm how the NEO 2T maps the `0..200` value to perceived resistance.
2. Decide whether the UI should describe values as percentage, FE-C units, or Newtons.
3. Add visible command success/failure feedback.
4. Handle disconnected and failed-write states.

The three presets already call the verified command path.

## 6. Improve The User Experience After The Protocol Works

After the protocol works:

- Improve the existing numeric FE-C and percentage labels if user testing shows that the scale is confusing.
- Improve the existing visible connection and command feedback for error recovery.
- Add a disconnect button.
- Reconnect cleanly after Bluetooth drops.
- Filter scan results to Tacx or FTMS devices.
- Surface scan failures.
- Add a real-device test record to `docs/current-state.md`.

## Immediate Action

First use the emulator to confirm the interface:

```bash
adb devices
dotnet build -t:Run -f net8.0-android
```

Then repeat the same command with the physical Android phone and document the Bluetooth service results.
