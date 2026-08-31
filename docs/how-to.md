# make adb available in the terminal session
```bash
export JAVA_HOME=$(/usr/libexec/java_home -v 17) && export ANDROID_HOME="$HOME/Library/Android/sdk" && export ANDROID_SDK_ROOT="$ANDROID_HOME" && export PATH="$JAVA_HOME/bin:$ANDROID_HOME/platform-tools:$PATH"
```


# Run
```bash
export JAVA_HOME=$(/usr/libexec/java_home -v 17) && export ANDROID_HOME="$HOME/Library/Android/sdk" && export ANDROID_SDK_ROOT="$ANDROID_HOME" && export PATH="$JAVA_HOME/bin:$ANDROID_HOME/platform-tools:$PATH" && dotnet build -t:Run -f net8.0-android --no-restore
```