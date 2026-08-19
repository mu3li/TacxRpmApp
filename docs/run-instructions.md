# Run Tacx RPM App

## Prerequisites

The project targets Android with .NET MAUI. The required tools are:

- .NET 8 SDK
- .NET MAUI and Android workloads
- JDK 17
- Android SDK and platform tools
- An Android emulator or physical Android phone

The app's Bluetooth functionality requires a physical Android phone. The emulator can be used to check the interface only.

## Configure the Terminal

From the project directory, configure Java and Android for the current terminal session:

```bash
cd /Users/cs/Documents/repos/TacxRpmApp

export JAVA_HOME=$(/usr/libexec/java_home -v 17)
export ANDROID_HOME="$HOME/Library/Android/sdk"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export PATH="$JAVA_HOME/bin:$ANDROID_HOME/platform-tools:$PATH"
```

Check the tools:

```bash
java -version
adb version
dotnet --info
dotnet workload list
```

If JDK 17 is not installed:

```bash
brew install --cask temurin@17
```

## Build the Android App

```bash
dotnet build -f net8.0-android
```

A successful build should report `Build succeeded` with `0 Error(s)`.

The project disables Android Fast Deployment and embeds the .NET assemblies in the APK. This is intentional: it prevents the emulator startup error about missing assemblies in `files/.__override__`.

## Run on an Android Emulator

Start an Android emulator first, then check that Android Debug Bridge sees it:

```bash
adb devices
```

The device should be listed with the state `device`. Run the app with:

```bash
dotnet build -t:Run -f net8.0-android
```

The command builds, installs, and launches the app. It uses the self-contained APK settings already configured in the project.

## Run on a Physical Android Phone

1. Enable **Developer options** on the phone.
2. Enable **USB debugging**.
3. Connect the phone to the Mac with USB.
4. Accept the USB debugging authorization prompt on the phone.
5. Confirm the connection:

```bash
adb devices
```

6. Run the app:

```bash
dotnet build -t:Run -f net8.0-android
```

For Tacx Bluetooth testing, pair the Tacx Neo 2T with the Android phone first and grant the app's Bluetooth permissions when Android asks.

## Useful Commands

List connected Android devices:

```bash
adb devices
```

Launch the installed app manually:

```bash
adb shell monkey -p TacxRpmApp.TacxRpmApp 1
```

Build without launching:

```bash
dotnet build -f net8.0-android
```

Build and launch Android explicitly:

```bash
dotnet build -t:Run -f net8.0-android
```

## Current Build Notes

The project has only one target, Android. Build it with:

```bash
dotnet build -f net8.0-android
```

The Android build may report warnings for nullability in the Bluetooth service and older Android Bluetooth APIs. These warnings do not currently prevent the Android app from building or launching.

The app can scan, inspect, and control basic resistance on a Tacx BLE connection. `TacxNeoService.SetResistanceAsync` sends the confirmed FE-C basic-resistance command through `fec3`. Verify real-device results with:

```bash
adb logcat -c
adb logcat -s TacxRpmApp:D
```

See [current-state.md](current-state.md) and [protocol-findings.md](protocol-findings.md) for the complete behavior and protocol details.
