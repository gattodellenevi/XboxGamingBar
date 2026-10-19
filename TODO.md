# Project Backlog & TODOs

- [x] **Display Current RTSS Configuration When Non-Elevated**
  - Active RTSS framerate limiter status (limit value, slider, judder-free divisors, limiter mode) is displayed in high-contrast read-only state (`IsEnabled = true`, `IsHitTestVisible = false`, `IsTabStop = false`) instead of being collapsed or dimmed to unreadable gray when the helper is running non-elevated (`isMismatch`).
  - Added `[ 🔒 READ-ONLY ]` pill badge in the Framerate Limiter card header and an elevation warning banner with a 1-click "Restart Helper as Administrator" action button.
  - In the Quick Access tab, the Frame Limiter tile displays the active FPS limit with padlock (e.g. `60 FPS • 🔒` or `OFF • 🔒`), subtitle indicates elevation is required to modify, and clicking the tile triggers the helper elevation restart.
  - Added `QuickElevationWarningCard` to the Quick Access tab for seamless couch controller navigation.
  - Preserved full interactivity for OSD controls (metrics overlay level, text size slider) without requiring elevation.
