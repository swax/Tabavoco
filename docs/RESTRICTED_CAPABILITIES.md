Tabavoco is a desktop volume control overlay requiring runFullTrust because it uses native APIs unavailable in sandboxed/UWP contexts:

1. COM Interop: IAudioEndpointVolume/IMMDeviceEnumerator to control system volume.
2. Win32: SetWindowPos/SetWindowLong to stay above the taskbar and hide from Alt+Tab.
3. SMTC: System Media Transport Controls for media playback buttons.
4. Registry: HKCU Run key for optional launch-at-startup.

No user data collected. No network requests.
