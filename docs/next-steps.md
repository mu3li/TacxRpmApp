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

At this stage, a successful test means that the app finds the trainer, discovers its services, and returns to the main page showing `CONECTADO`. Resistance changing is not expected yet.

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

## 4. Inspect The Protocol Before Writing Commands

Use a BLE inspection app such as nRF Connect on the Android phone. With other trainer apps closed, inspect the Tacx device and record:

- Service UUIDs
- Characteristic UUIDs
- Read, write, write-without-response, and notify properties
- Notification descriptors
- Any response received after connecting

The old `docs/chat-summary.md` mentions a possible command format, but it is not verified. Do not implement that command based only on the summary.

## 5. Implement One Verified Resistance Command

The next code change should be limited to `TacxNeoService`:

1. Confirm the correct write characteristic.
2. Enable notifications if responses are required.
3. Send one known resistance value.
4. Log the write result and response.
5. Verify that the trainer physically changes resistance.
6. Handle disconnected and failed-write states.

Only after one fixed resistance command works should the three presets call it.

## 6. Improve The User Experience After The Protocol Works

After the protocol works:

- Display the numeric preset values on the main page.
- Show connection and command errors visibly.
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
