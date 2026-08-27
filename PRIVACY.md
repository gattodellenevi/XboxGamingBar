# Privacy Policy for Couch Gaming Bar

**Effective Date:** August 27, 2026  
**Last Updated:** August 27, 2026  
**Publisher:** Zanchisoft  
**Repository:** https://github.com/gattodellenevi/XboxGamingBar

---

## 1. Introduction

**Couch Gaming Bar** ("we", "our", or "the application"), published by **Zanchisoft**, is committed to protecting your privacy. This Privacy Policy explains what information is processed by Couch Gaming Bar and confirms that **we do not collect, store remotely, share, or transmit any personal data or personally identifiable information (PII)**.

Couch Gaming Bar operates as a local Windows utility (Xbox Game Bar widget and desktop helper companion) designed to give users controller-friendly control over gaming and hardware settings.

---

## 2. Information We Do NOT Collect

- **No Personal Identifiable Information (PII):** We do not collect or request your name, email address, postal address, phone number, IP address, username, or account credentials.
- **No User Tracking or Analytics:** The application does not contain telemetry SDKs, tracking pixels, analytics frameworks (such as Google Analytics, Firebase, App Center), or advertising identifiers.
- **No Advertising:** The application contains no advertisements, ad networks, or user profiling mechanisms.
- **No Remote Transmission:** The application does not transmit any of your personal or hardware data to any external server or third party.

---

## 3. Data Processed Locally on Your Device

To provide its core functionality, Couch Gaming Bar interacts with system APIs and hardware purely **on your local machine**:

### a. Hardware & System Telemetry
- **Metrics Read:** GPU/CPU temperatures, clock speeds, usage percentages, fan speeds, power profiles, frame rates, and display configurations (refresh rates and resolutions).
- **Purpose:** Displaying real-time performance statistics in the on-screen display (OSD) overlay and allowing the user to adjust power/performance settings.
- **Processing:** This data is queried in real time via local APIs (e.g., Windows native APIs, AMD ADLX, LibreHardwareMonitor) and is retained only in volatile system memory (RAM). It is never recorded permanently or transmitted over any network.

### b. Active Process & Foreground Window Detection
- **Information Read:** Process names / window titles of currently running games.
- **Purpose:** Applying user-configured Per-Game Profiles automatically when a specific game is active.
- **Processing:** Evaluated strictly in local memory to match user profile names. It is never logged or shared.

### c. User Preferences & Settings Storage
- **Information Stored:** User-selected preferences such as FPS limits, CPU power scheme preferences, OSD display modes, and custom per-game configurations.
- **Location:** Stored exclusively in local files (`settings.xml` and profile XML files) within your local device's application data directories:
  - `%LOCALAPPDATA%\Packages\Zanchisoft.CouchGamingBar_...\LocalState` (when running packaged)
  - `%LOCALAPPDATA%\CouchGamingBar` (when running unpackaged)
- **Control:** You have full ownership and control over these files.

### d. Local Diagnostic Logs
- **Information Stored:** Non-personal technical diagnostics, errors, and operational events (e.g., helper startup, connection state, API return codes) written via NLog.
- **Location:** Stored locally in `%LOCALAPPDATA%` (e.g., `widget_*.log`, `helper_*.log`).
- **Retention:** Configured to automatically rotate and purge after 3 days (`maxArchiveDays="3"`). Logs remain solely on your device unless you voluntarily provide them for troubleshooting in a public GitHub issue.

---

## 4. Network and Internet Access

- The Microsoft Store package of Couch Gaming Bar does **not** request or use the `internetClient` capability.
- The application operates entirely **offline**.
- External hyperlinks (such as opening the GitHub repository or AMD/NVIDIA download pages) are triggered only upon explicit user action and are handled by your default web browser outside of the application sandbox.

---

## 5. Third-Party Libraries

Couch Gaming Bar utilizes trusted open-source and vendor libraries for local hardware communication:
- **[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)**: For local hardware sensor queries.
- **AMD ADLX SDK**: For local AMD GPU driver setting adjustments.
- **RivaTuner Statistics Server (RTSS)**: For local on-screen performance overlay rendering.

None of these integrated libraries are configured to collect or transmit telemetry to external servers.

---

## 6. Data Retention and Deletion

All application data resides exclusively on your local computer. You can delete all data at any time by:
1. Uninstalling **Couch Gaming Bar** via **Windows Settings > Apps > Installed apps**.
2. Optionally deleting the local settings folder at `%LOCALAPPDATA%\CouchGamingBar` and `%LOCALAPPDATA%\Packages\Zanchisoft.CouchGamingBar_*`.

---

## 7. Children's Privacy

Couch Gaming Bar is not directed at children, nor does it collect personal data from anyone, including children under the age of 13 (or applicable age in your jurisdiction), in compliance with COPPA (Children's Online Privacy Protection Act) and GDPR (General Data Protection Regulation).

---

## 8. Open Source & Transparency

Couch Gaming Bar is free and open-source software. You can inspect the source code and verify our privacy and security practices directly on GitHub:  
https://github.com/gattodellenevi/XboxGamingBar

---

## 9. Changes to This Privacy Policy

We may update this Privacy Policy from time to time to reflect changes in application features or legal requirements. Any updates will be published with a revised "Last Updated" date in the GitHub repository.

---

## 10. Contact Us

If you have any questions, concerns, or feedback regarding this Privacy Policy, you may contact us by:
- Opening an issue on our GitHub repository: https://github.com/gattodellenevi/XboxGamingBar/issues
- Reaching out to the publisher **Zanchisoft** via the Microsoft Store developer contact channel.
