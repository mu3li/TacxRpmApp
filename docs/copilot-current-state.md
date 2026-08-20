# Tacx RPM App: Current State

This document describes what is in the repository now. It is intended as a memory aid for returning to the project after a break.

## 1. What The App Is

TacxRpmApp is a .NET MAUI application aimed at Android phones. Its intended purpose is to provide three quick resistance presets for a Tacx NEO 2T indoor trainer during an RPM class:

- **UPHILL**: a harder resistance preset
- **CRUISE**: a middle resistance preset
- **DOWNHILL**: an easier resistance preset

The app also has a Bluetooth device browser and a configuration page. The interface text is currently in Portuguese, while the project and code identifiers are mostly English.

Important status: Bluetooth discovery, GATT service inspection, FE-C notifications, and basic resistance control are implemented and have been verified on a physical NEO 2T. The Apply buttons currently send FE-C basic-resistance values in the `0..200` range.

## 2. Current User Flow

### Starting the app

`App` creates a navigation page containing `MainPage`. `MauiProgram` registers one shared `TacxNeoService`, one shared `RpmSettings`, and one `MainPage` through dependency injection.

### Connecting

1. Tap **CONECTAR**.
2. Android asks for Bluetooth permission if it has not already been granted.
3. The app opens the **Dispositivos** page.
4. Tap **PESQUISAR DISPOSITIVOS**.
5. The app scans for 8 seconds and lists named BLE devices. Each row shows the name, MAC address, advertised service UUIDs, and a **LIGAR** button.
6. Tap **LIGAR** beside the intended trainer.
7. The app calls `ConnectGatt`, waits up to 12 seconds for the connection attempt, discovers GATT services, and retries once after a failed attempt.
8. A connection is considered successful when the trainer exposes either:
   - the standard FTMS Control Point characteristic; or
   - the proprietary Tacx write characteristic.
9. On success the device page closes and the main button changes to **CONECTADO**.

The scan does not currently filter by the name `Tacx` or by a specific advertised service. Named BLE devices from the surrounding area can therefore appear in the list.

### Adjusting presets

Each preset has `+` and `-` buttons. The amount changed is the configured **PASSO** (increment). Values cannot be reduced below zero.

Tap the large colored preset area to apply it. The selected preset is shown by its colored Apply button:

- Uphill: red
- Cruise: green
- Downhill: blue
- Not selected: gray

The Apply action calls `TacxNeoService.SetResistanceAsync`, which sends an FE-C basic-resistance command through the Tacx `fec3` characteristic using Write Without Response. The command was verified on the physical trainer and returned FE-C command status `success`.

### Configuring values

Tap **CONFIG** to edit:

- Uphill
- Cruise
- Downhill
- Passo

Values are parsed as integers. Uphill, Cruise, and Downhill must be at least zero. Passo must be at least one. Invalid input leaves the page open and shows **Use valores válidos.** Valid input is saved and the page returns to the main screen after a short delay.

## 3. Defaults And Persistence

The first run defaults are:

| Setting | Default | Constraint |
| --- | ---: | --- |
| Uphill | 45 | 0 or greater |
| Cruise | 20 | 0 or greater |
| Downhill | 10 | 0 or greater |
| Increment / Passo | 2 | 1 or greater |

`RpmSettings` stores values using `Microsoft.Maui.Storage.Preferences`. They persist on the device between app launches and are not stored in a project file or database. Clearing the app's Android data resets them to the defaults.

The main screen displays each preset as both its FE-C value and percentage equivalent, for example `45 FE-C (22.5%)`. The status line also shows the latest connection or command result. Configuration accepts preset values from `0` through `200`.

## 4. Project Map

| File | Responsibility |
| --- | --- |
| `TacxRpmApp.csproj` | Android-only MAUI project, package references, and Android packaging settings |
| `MauiProgram.cs` | MAUI builder, CommunityToolkit registration, dependency injection registrations |
| `App.xaml` / `App.xaml.cs` | Application resources and root navigation page |
| `MainPage.xaml` / `MainPage.xaml.cs` | Main preset screen, connect/config navigation, preset adjustment and selected-state colors |
| `DevicePage.xaml` / `DevicePage.xaml.cs` | BLE scan results, device selection, connection feedback |
| `ConfigPage.xaml` / `ConfigPage.xaml.cs` | Editing and validating persisted preset settings |
| `RpmSettings.cs` | Preferences-backed settings model |
| `BleDeviceInfo.cs` | Immutable scan-result record: name, address, advertised services |
| `TacxNeoService.cs` | Android BLE scan, GATT connection, service discovery, connection status |
| `Platforms/Android/AndroidManifest.xml` | Android Bluetooth and location permission declarations |
| `Platforms/Android/MainActivity.cs` | Android MAUI activity entry point and configuration changes |
| `Platforms/Android/MainApplication.cs` | Android application entry point |
| `docs/run-instructions.md` | Environment setup and build/run commands |
| `docs/chat-summary.md` | Historical development notes; some protocol notes in it are planned, not current behavior |

## 5. BLE Implementation

The Android service uses `BluetoothLeScanner`, `BluetoothGatt`, and `BluetoothGattCallback`.

### UUIDs currently recognized

| Purpose | UUID |
| --- | --- |
| Standard FTMS service | `00001826-0000-1000-8000-00805f9b34fb` |
| Standard FTMS Control Point | `00002ad9-0000-1000-8000-00805f9b34fb` |
| Proprietary Tacx service | `6e40fec1-b5a3-f393-e0a9-e50e24dcca9e` |
| Proprietary Tacx write characteristic | `6e40fec3-b5a3-f393-e0a9-e50e24dcca9e` |

During service discovery the app identifies both the standard FTMS Control Point and the proprietary Tacx characteristics. Basic resistance uses the proprietary Tacx `fec3` characteristic because that is the confirmed NEO 2T path.

### Connection behavior and errors

- Scan duration: 8 seconds.
- Connection timeout per attempt: 12 seconds.
- Number of connection attempts: up to 2.
- Retry delay: 700 milliseconds.
- Android security exceptions are converted into a permission-related status.
- A failed connection can display Android status `133`.
- If no FTMS service is found, the service reports the discovered service list when available.
- A trainer can be connected for inspection even when the standard FTMS service is absent, provided the proprietary Tacx write characteristic is found.

The service is Android-specific because the project now has only an Android target.

## 6. Permissions And Prerequisites

The Android manifest declares:

- `BLUETOOTH`
- `BLUETOOTH_ADMIN`
- `BLUETOOTH_CONNECT`
- `BLUETOOTH_SCAN` with `neverForLocation`
- `ACCESS_FINE_LOCATION`

The app requests `Permissions.Bluetooth` when **CONECTAR** is tapped. The phone must have Bluetooth enabled. For reliable trainer testing, use a physical Android phone; the emulator is suitable for UI/build checks but normally cannot exercise the trainer's real Bluetooth connection.

The development machine needs .NET 8, the MAUI and Android workloads, JDK 17, Android SDK/platform tools, and either an Android emulator or a USB-connected Android phone. See [run-instructions.md](run-instructions.md) for shell setup.

## 7. Real-Device Test Procedure

### Prepare the trainer and phone

1. Plug in and wake the Tacx NEO 2T.
2. Close other trainer apps that might already own the Bluetooth connection, such as Garmin, Zwift, or Tacx Training.
3. Turn on Bluetooth on the Android phone.
4. Enable Developer options and **USB debugging** on the phone.
5. Connect the phone to the Mac with a data-capable USB cable.
6. Accept the phone's USB debugging authorization prompt.
7. Confirm that the computer sees the phone:

```bash
adb devices
```

The phone should appear with state `device`, not `unauthorized` or `offline`.

### Install and launch

From the repository root:

```bash
export JAVA_HOME=$(/usr/libexec/java_home -v 17)
export ANDROID_HOME="$HOME/Library/Android/sdk"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export PATH="$JAVA_HOME/bin:$ANDROID_HOME/platform-tools:$PATH"

dotnet build -t:Run -f net8.0-android
```

Grant Bluetooth access when Android asks. If the app was already installed, the same command updates and launches it.

### Verify the currently implemented behavior

1. Confirm the main page opens and shows **CONECTAR**, **CONFIG**, and the three presets.
2. Open **CONFIG**, change the values, tap **GUARDAR**, close and reopen the app, and confirm the values persist by returning to **CONFIG**.
3. Test `+` and `-`; confirm `-` never makes a value negative.
4. Tap **CONECTAR** and grant Bluetooth permission.
5. Tap **PESQUISAR DISPOSITIVOS** and wait for the 8-second scan to finish.
6. Select the trainer and observe the connection result.
7. If connection succeeds, confirm the main button becomes **CONECTADO**.
8. Tap a preset and confirm only its button changes to the preset color.
9. Confirm that the trainer resistance changes and that logcat reports `FE-C command status: command=0x30, status=success`.

### What counts as a useful test result

Record the phone model, Android version, trainer firmware if known, whether another app was connected, the scan result, the selected device name/address, and the exact status text. This information is needed before implementing and validating the write protocol.

## 8. Troubleshooting

**No devices appear:** Ensure Bluetooth is on, the trainer is awake, other trainer apps are closed, permissions are granted, and the phone is close to the trainer. The current scanner ignores unnamed advertisements, so a device without a BLE name will not be shown.

**The phone is not listed by `adb devices`:** Unlock the phone, reconnect the USB cable, choose a data/USB debugging mode, and accept the authorization dialog. Do not diagnose BLE until ADB reports `device`.

**Permission denied:** Open Android Settings, find the app's permissions, enable nearby-device/Bluetooth access, then restart the app. Android versions differ in the wording of this permission.

**Connection fails with `133`:** Power-cycle or wake the trainer, close competing Bluetooth apps, disable and re-enable Bluetooth, rescan, and retry. The app already retries once, but it does not yet implement a full disconnect/reconnect lifecycle.

**The build fails:** Confirm that JDK 17 and the Android SDK environment variables are configured as shown in [run-instructions.md](run-instructions.md), then run `dotnet build -f net8.0-android`.

## 9. Known Limitations And Next Work

- `SetResistanceAsync` sends verified FE-C basic-resistance commands in the `0..200` range.
- The FE-C basic-resistance framing is confirmed, but the user-facing meaning of each preset value still needs to be finalized. The current values are FE-C range values, not guaranteed Newton values.
- The app does not subscribe to notifications or read current trainer state.
- There is no explicit disconnect button or connection lifecycle recovery after leaving the page.
- Scan errors are not surfaced from `OnScanFailed`.
- Only named BLE devices are listed, and scan results are not filtered to Tacx/FTMS devices.
- The main page shows FE-C and percentage values, but the percentage is a protocol-scale equivalent and not a guaranteed Newton measurement.
- There are no automated tests in the repository.
- Settings are local device preferences; there is no export, backup, or synchronization.
The next high-value implementation step is to confirm the characteristic properties and response/notification format on a real NEO 2T, then implement one verified resistance write with logging and a focused device test before reconnecting it to all three preset buttons.

## 10. Safe Working Rules For Future Changes

- Build Android explicitly while working on BLE: `dotnet build -f net8.0-android`.
- Keep the connection and command protocol in `TacxNeoService`; keep screen behavior in the pages.
- Test permission denial as well as permission approval.
- Do not describe resistance control as working until a real trainer changes resistance and the result is repeatable after reconnecting.
- Avoid committing credentials, personal access tokens, or generated `bin/` and `obj/` output.