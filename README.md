# GroupChat

[![Download Now](https://img.shields.io/badge/Download%20Now-%23007ACC?style=for-the-badge&logo=github&logoColor=white)](https://github.com/zemendaniel/groupchat/releases/latest)

A fast, lightweight, cross‑platform LAN group chat application built with .NET and Avalonia. Simple to set up, easy to use, and designed to work on Windows, macOS, and Linux. Perfect for chatting in a school class room, or quickly sending a link to a co-worker.

### Works without requiring admin privileges. On the same subnet, the app functions even if it isn’t explicitly allowed through the firewall.
### If you like this project, please star this repo :)

<br/>
<img width="800" height="600" alt="image" src="https://github.com/user-attachments/assets/06ddc4d7-279a-4894-97ff-6d776f9f8b2a" />
<br/>
<img width="800" height="600" alt="image" src="https://github.com/user-attachments/assets/ead7ea50-b326-4859-bf83-7d2aca2e2645" />

## Table of Contents
- Features
- How does it work?
- Installation (Windows, macOS, Linux)
- Usage
- Contributing
- Roadmap (extras)
- Troubleshooting (extras)
- FAQ (extras)

## Features
- Cross‑platform: Windows, macOS, and Linux.
- Simple setup: runs as a desktop app with a minimal UI.
- Local configuration: user settings stored per user profile.
- Optional password for rooms/sessions (secure, encrypted communication).
- Light on resources and quick to launch.

## How does it work?
- The app sends UDP broadcast messages and listens for UDP broadcast messages.
- If you provide a password, the app encrypts the messages with AES. All users in the room must have the same password.
- The app uses the local network to communicate with other users.

## Installation
You can install GroupChat using the prebuilt binaries in Releases. If you don't trust the binaries, you can build GroupChat yourself. (More info below.)

Platform-specific note:
- Password saving is currently Windows‑only. On macOS and Linux, your password will not be stored between sessions yet.

### Windows
1. Download GroupChat-win-x64.exe (64 bit) from Releases.
2. Double‑click to run.
3. Windows Defender will think the app is a virus, but it's not. Just click 'More Info' and 'Run Anyway'.
4. Windows may prompt you to allow the app through the firewall. Clicking ‘Allow Access’ is optional — if you cancel (e.g., without admin privileges), the app will still function normally.

### Linux
1. Download GroupChat-linux-x64 from Releases.
2. Make the file executable: `chmod +x GroupChat-linux-x64`
3. Run the file: `./GroupChat-linux-x64`

### MacOS
1. Download GroupChat-osx-arm64.zip (Apple Silicon) from Releases.
3. Extract the archive.
3. Make the file executable: `chmod +x GroupChat-osx-arm64`
4. Run the file: `./GroupChat-osx-arm64`

If you need other versions (like 32 bit or ARM) feel free to ask me to publish them. Or if you don't trust the binaries, you can build GroupChat yourself. For that you will need to download and install the .NET 9 SDK. Then either clone the repo or download the zip. Afer that, you can run the following command in the root directory of the project:
`dotnet publish groupchat.gui/groupchat.gui.csproj -c Release -r <YOUR RELEASE HERE (eg. win-x64)> --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true`

## Usage
- Enter your name and your shared password (if you have one). Make sure all users have entered the same password, otherwise you won't be able to see each other's messages.
- If you have more network interfaces, you can select which one to use.
- Select a custom port or leave the default. Make sure all participants use the same port to communicate.
- Click Start Chat or press Enter inside either the nickname or password textbox.
- You can now send messages up to 900 characters long, including emojis, to devices on your local subnet.
- To send a message, press Enter.
- You can also copy and paste messages.
- Your configuration is stored in your user profile: AppData\GroupChat on Windows, ~/Library/Application Support/GroupChat on macOS, and ~/.config/GroupChat on Linux. On Windows, your password is securely saved using DPAPI in the AppData folder

## Contributing
Contributions are welcome! To get started:
- Open issues for bugs or feature requests.
- Fork the repo and submit a Pull Request with a clear description.
- Keep changes focused and include repro steps or tests when possible.

High‑impact contribution ideas:
- Implement secure password storage on macOS (Keychain) and Linux (e.g., libsecret).
- Packaging helpers (e.g., .desktop files, signing/notarization guidance for macOS).
- Automated UI and integration tests.

## Roadmap
- Secure password storage for macOS and Linux.
- Signed binaries.
- Package MasOS release with into .app
- .desktop file for Linux

## Troubleshooting
- Windows: SmartScreen warning → “More info” → “Run anyway.”
- macOS: “App is damaged or can’t be opened” or “unidentified developer”
    - Right‑click → Open, or run:
``` bash
    chmod +x /path/to/GroupChat
    xattr -d com.apple.quarantine /path/to/GroupChat
```
- Linux: “Permission denied”
    - Ensure it’s executable:
``` bash
    chmod +x ./GroupChat-linux-x64
```
- Connection issues
    - Ensure both sides use the same port and that firewalls allow traffic.
    - Try a different port if needed.
    - Make sure everyone has entered the same password.

## FAQ
- Is GroupChat free?
    - Yes, GNU Public Licensed.

- Does it collect telemetry?
    - No personal telemetry is collected.

- Where is my configuration stored?
    - In your user profile’s application data directory for your platform.

- Why isn’t my password remembered on macOS/Linux?
    - Feature pending; currently supported on Windows only.

If you have ideas or run into issues, please open an Issue - your feedback helps shape GroupChat!


