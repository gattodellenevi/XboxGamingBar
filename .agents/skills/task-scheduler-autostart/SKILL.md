---
name: task-scheduler-autostart
description: Explains how to configure Windows Task Scheduler for CouchGamingBarHelper to autostart on boot/logon with system tray icon and full elevation.
---

# Windows Task Scheduler Configuration for CouchGamingBarHelper

When configuring Windows Task Scheduler to autostart `CouchGamingBarHelper.exe` at boot/logon, follow these specific settings to ensure the system tray icon appears and hardware controls (TDP, RyzenAdj, CPU Power profiles) function properly:

## 1. General Tab
* **Security Options**: Select **"Run only when user is logged on"**.
  * *Reason*: Selecting "Run whether user is logged on or not" or running under "At system startup" forces the process into **Session 0**, where Windows system tray notification icons (`NotifyIcon`) cannot be rendered or displayed.
* **Privileges**: Check **"Run with highest privileges"**.
  * *Reason*: Required for hardware monitoring (LibreHardwareMonitorLib, CPU power schemes, native performance counters).
* **Configure for**: Windows 10 or Windows 11.

## 2. Triggers Tab
* **Begin the task**: Set to **"At log on"** (At log on of any user or specified user).
  * *Reason*: "At system startup" triggers before user desktop logon, launching the task into Session 0 without access to the interactive desktop taskbar.

## 3. Actions Tab
* **Action**: Start a program.
* **Program/script**: Path to `%LOCALAPPDATA%\CouchGamingBarHelper\CouchGamingBarHelper.exe`.
  * *Important*: Do **NOT** point directly to `C:\Program Files\WindowsApps\...`. WindowsApps is ACL-isolated by `TrustedInstaller`, preventing non-AppContainer execution via Task Scheduler and causing launch failures. Additionally, paths in `WindowsApps` are tied to specific package versions and break across MSIX updates.
  * `Register-AutostartTask.ps1` automatically copies the helper executable, dependency DLLs (`ADLXCSharpBind.dll`, `RTSSSharedMemoryNET.dll`, `Shared.dll`, etc.), and `Assets` into `%LOCALAPPDATA%\CouchGamingBarHelper` prior to registering the task.
* **Start in (optional)**: Absolute directory path containing `CouchGamingBarHelper.exe` (`%LOCALAPPDATA%\CouchGamingBarHelper`).

## 4. Settings Tab
* **Stop the task if it runs longer than**: **Uncheck** this option so Windows does not automatically kill the helper after 3 days.
* **If the task is already running**: Select "Do not start a new instance".
