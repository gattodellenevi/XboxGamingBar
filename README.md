# CouchGamingBar

## What is it?

CouchGamingBar is a helper tool for gamers to control all gaming-related settings using the gamepad/game controller.
CouchGamingBar is built as an Xbox Game Bar widget as the frontend, and a Win32 helper as the backend tool.
It's a fork from this project: https://github.com/namquang93/XboxGamingBar, which I'm iterating on.

## Vision
The vision is to create a compact, user friendly, and fully featured tool for sofa gamers to control a plethora of settings with a controller.
It is aimed at both AMD and Intel/NVIDIA users, both Desktop and Laptop computers.  
It is NOT aimed at Handheld devices, for which alternative projects already exist and are pretty mature. Although it will work without issues.

PC gaming can be complex, so effort is put into making settings easy to understand, with sensible defaults and easily reversible

## Features

As of now, there are the following functions:

### Performance Control
- Performance Overlay using RivaTuner Statistics Server OSD.
- Extensive options for RTSS frame limiting
- Per-game Profile.
- CPU performance adjustments.
  - Enable or disable CPU Boost.
  - Set CPU Energy Performance Preference (EPP).
  - Set CPU clock speed limit.
- Frame limiter 

![alt text](Screenshots/v4_1.png?v=1)

### Quick System Settings
- Quickly change screen refresh rate and resolution.
- Binding gamepad keys to some system functions.
  - Lossless Scaling hotkey.

![alt text](Screenshots/v4_2.png?v=1)

### AMD Settings
  - Radeon Super Resolution.
  - AMD Fluid Motion Frame.
  - Radeon Anti-Lag.
  - Radeon Boost.
  - Radeon Chill.

![alt text](Screenshots/v4_3.png?v=1)

## Editions (Store vs. GitHub Release)
CouchGamingBar is available both on the Microsoft Store and as a sideloaded GitHub release. For a detailed comparison of features, hardware sensors, and privilege models, see **[Store vs. GitHub Differences](STORE_VS_GITHUB.md)**.

## Installation

### Step 1: Install the Security Certificate (.cer)
Sideloaded Windows packages are signed with a security certificate. You must add the certificate to your system's trusted store once:

Extract the downloaded release .zip file.
Locate and double-click the certificate file (.cer).
Click Install Certificate...
Select Local Machine (requires Administrator) and click Next.
Select Place all certificates in the following store.
Click Browse... and select Trusted People (or Trusted Root Certification Authorities).
Click OK > Next > Finish.
A message will appear saying: "The import was successful."

### Step 2: Install the App Package
Option A: Using the Automated PowerShell Script (Recommended)
In the extracted folder, right-click Add-AppDevPackage.ps1.
Select Run with PowerShell.
If prompted for execution policy, press Y and Enter.
Follow the on-screen prompts until installation completes.


Xbox Gaming Bar is 100% free and open source. It's built upon C#.
Libraries used:
- **[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)** for performance statistics overlay.

## Privacy & License
- [Privacy Policy](PRIVACY.md)
- [MIT License](LICENSE)


