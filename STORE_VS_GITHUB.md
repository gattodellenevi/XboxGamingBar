# Microsoft Store vs. GitHub Release Versions

CouchGamingBar is distributed in two editions: the **Microsoft Store** version and the **GitHub (Sideload / Release)** version.

While both editions share the same core controller-first overlay interface and Xbox Game Bar integration, they differ in telemetry capabilities, background helper automation, and system privilege models due to Microsoft Store sandboxing and certification requirements.

---

## Comparison Matrix

| Feature / Aspect | GitHub Release (`Release`) | Microsoft Store (`Store`) |
| :--- | :--- | :--- |
| **Hardware Telemetry Provider** | **`LibreHardwareMonitorLib` (v0.9.6)**<br>via [`LibreHardwareProvider.cs`](Samples/XboxGamingBarHelper/Hardware/LibreHardwareProvider.cs) | **Windows Performance Counters & Native APIs**<br>via [`WindowsHardwareProvider.cs`](Samples/XboxGamingBarHelper/Hardware/WindowsHardwareProvider.cs) |
| **CPU Temperature & Wattage** | **Supported**<br>Full package temperatures and wattage monitoring | **Unsupported / Hidden**<br>Native Windows APIs cannot query ring-0 temperature or power sensors without kernel drivers |
| **Active Provider Badge (UI)** | Green badge: **`LIBREHARDWARE 0.9.6`** | Blue badge: **`WINDOWS API`** (*Store sandbox mode*) |
| **Companion App Auto-Launch** | **Automatic**<br>Automatically launches RTSS and AMD Software via `Process.Start` if not running | **Manual**<br>External process launching is bypassed; RTSS or AMD Software must be opened manually |
| **UAC Execution Level** | [`highestAvailable`](Samples/XboxGamingBarHelper/app.manifest) | [`asInvoker`](Samples/XboxGamingBarHelper/app.Store.manifest) |
| **AppX / MSIX Capabilities** | Declares [`allowElevation`](Samples/XboxGamingBarPackage/Package.appxmanifest) capability | `allowElevation` omitted to comply with Store policies |
| **Installation Method** | Sideloaded package (`.msixbundle` / `.appxbundle`) via automated script (`Install.cmd`) | 1-Click install directly from Microsoft Store |
| **Security Certificate** | Requires one-time import of self-signed `.cer` certificate into `Trusted People` / `Root` | Pre-trusted automatically via Microsoft Store signing |
| **Autostart Setup** | Automated via Task Scheduler ([`Register-AutostartTask.ps1`](Samples/Register-AutostartTask.ps1)) with highest runlevel | Standard logon registration or manual Task Scheduler setup |
| **Updates** | Manual download of new releases from GitHub | Automatic background updates through Microsoft Store |

---

## Why Are There Differences?

### 1. Microsoft Store Sandbox & Certification Policies
Microsoft Store applications and Desktop Bridge packages are subject to strict certification rules:
- **Restricted Capabilities (`allowElevation`)**: Store packages cannot declare unrestricted administrative elevation without special store exemption.
- **Kernel-Level Drivers**: Low-level hardware monitoring libraries (such as `LibreHardwareMonitorLib`) that access ring-0 hardware ports are restricted. The Store edition therefore compiles out `LibreHardwareMonitorLib` and `HidSharp`, falling back to unprivileged Windows native APIs (`System.Diagnostics.PerformanceCounter`, `CallNtPowerInformation`, and `GlobalMemoryStatusEx`).

### 2. External Process Spawning
The GitHub build proactively launches RivaTuner Statistics Server (RTSS) and AMD Software (Adrenalin Edition) when their corresponding features or overlays are enabled. Under Microsoft Store sandboxing, spawning arbitrary external Win32 processes is omitted to ensure predictable sandboxed behavior.

---

## Which Version Should You Use?

### Choose GitHub Release if:
- You want **CPU package temperature** and **CPU wattage** displayed in your performance overlay.
- You want RTSS or AMD Software to **automatically launch** in the background when needed.
- You are comfortable with sideloading and running the one-time certificate installation script (`Install.cmd`).

### Choose Microsoft Store if:
- You prefer **effortless 1-click installation** and **automatic background updates**.
- You do not need CPU temperature / wattage telemetry (basic CPU%, RAM, battery, and GPU stats via ADLX are sufficient).
- You want an untampered, store-verified package without managing certificates.
