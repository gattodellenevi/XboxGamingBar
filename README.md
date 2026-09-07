# CouchGamingBar

## What is it TLDR?

Control PC performance, RTSS FPS limits, display refresh rate, and AMD Radeon settings from your gamepad using Xbox Game Bar. Built for sofa PC gamers. 

## Vision
The vision is to create a compact, user friendly, and fully featured tool for sofa gamers to control a plethora of settings with a controller.
It is aimed at both AMD and Intel/NVIDIA users, both Desktop and Laptop computers.  
It is NOT aimed at Handheld devices, for which alternative projects already exist and are pretty mature. Although it will work without issues.

PC gaming can be complex, so effort is put into making settings easy to understand, with sensible defaults and easily reversible

## What is it, a bit more

Take full command of your PC gaming performance without leaving the sofa.

Couch Gaming Bar is a lightweight, controller-first Xbox Game Bar widget and system companion that lets you adjust frame limits, hardware power profiles, display settings, and GPU features directly with your gamepad—no mouse or keyboard required.

Whether you are playing on a big-screen TV from the couch, a desktop gaming rig, or a gaming laptop, Couch Gaming Bar puts essential performance tweaks at your fingertips with a single press of the Xbox guide button.

    ━━━━━━━━━━━━━━━━━━━━━
    🎮 CONTROLLER-FIRST COUCH GAMING
    ━━━━━━━━━━━━━━━━━━━━━
    • Complete Gamepad Control: Navigate menus seamlessly using D-pad, analog sticks, and shoulder bumpers (LT/RT).
    • Instant Game Bar Overlay: Summon your quick settings overlay anytime via Win + G or the controller Xbox button.
    • Per-Game Automatic Profiles: Configure custom settings per game that switch automatically upon game launch.

    ━━━━━━━━━━━━━━━━━━━━━
    ⚡ PERFORMANCE & FRAME PACING
    ━━━━━━━━━━━━━━━━━━━━━
    • Advanced RTSS Frame Limiter: Stabilize frametimes and eliminate stutter using RivaTuner Statistics Server integration.
    • Judder-Free Frame Sync: Snap FPS targets automatically to clean refresh-rate dividers (1/2, 1/3, 1/4 Hz).
    • Real-Time Metrics Overlay (OSD): Track FPS and hardware metrics with customizable text scaling.
    • Multiple Limiter Sync Modes: Support for Async, Front Edge, Back Edge, and low-latency frame pacing.

    ━━━━━━━━━━━━━━━━━━━━━
    🖥️ DISPLAY & SYSTEM CONTROLS
    ━━━━━━━━━━━━━━━━━━━━━
    • Quick Refresh Rate Switcher: Switch between 60Hz, 120Hz, 144Hz, 240Hz, and custom display frequencies.
    • Resolution Selector: Change active screen resolutions on the fly.
    • CPU Power Management: Toggle Dynamic CPU Boost, adjust Energy Performance Preference (EPP), and set maximum CPU clock limits (MHz) to reduce fan noise and heat.

    ━━━━━━━━━━━━━━━━━━━━━
    🔴 AMD RADEON™ SUITE (ADLX)
    ━━━━━━━━━━━━━━━━━━━━━
    Control driver-level AMD Radeon features directly from the widget (on supported AMD hardware):
    • Radeon Super Resolution (RSR) with dynamic sharpness adjustment
    • AMD Fluid Motion Frames (AFMF) frame generation
    • Radeon Anti-Lag for ultra-low input latency
    • Radeon Boost & Radeon Chill dynamic framerate regulation

    ━━━━━━━━━━━━━━━━━━━━━
    🔒 PRIVACY & OPEN SOURCE
    ━━━━━━━━━━━━━━━━━━━━━
    • 100% Offline & Private: No telemetry, no background analytics, no ads, and no external tracking.
    • Minimal Footprint: Optimized for ultra-low CPU baseline usage (<0.5% idle).
    • Open Source: Fully transparent codebase licensed under the MIT License.

*Note: Performance overlay and advanced frame limiting features require RivaTuner Statistics Server (RTSS) installed on the system. AMD features require compatible AMD Radeon graphics drivers.*

## Editions (Store vs. GitHub Release)
CouchGamingBar is available both on the Microsoft Store and as a sideloaded GitHub release. For a detailed comparison of features, hardware sensors, and privilege models, see **[Store vs. GitHub Differences](STORE_VS_GITHUB.md)**.

## Installation

  - Download the release zip file
  - Right click on the Install.cmd and select "Run as Administrator"

Xbox Gaming Bar is 100% free and open source. It's built upon C#.
Libraries used:
- **[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)** for performance statistics overlay.

## Privacy & License
- [Privacy Policy](PRIVACY.md)
- [MIT License](LICENSE)


