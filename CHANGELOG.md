# Changelog

All notable changes to **MAMAutoPoints** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.4.5wfb] - 2026-09-13

### Fixed
- **"Buy ONLY Freeleech Wedges" checkbox not persisting across restarts** — `checkBoxFlOnly` state was never saved to or loaded from the config file, so it reverted to unchecked every time the app closed and restarted. Now saves and restores correctly like all other settings.

## [2.4.4wfb] - 2026-09-13

### Fixed
- Settings persistence — slider tier was saved from a stale hidden combo index and always restored to `100 GB`; now saves the actual trackbar position
- CI actions bumped to Node 24-compatible releases (`actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/upload-artifact@v7`)

### Added
- `Save Settings` button — manually writes all settings to the config file and confirms the saved path
- GitHub Actions workflow (`.github/workflows/build-exe-on-release.yml`) — builds a single-file self-contained EXE and attaches `MAM-Auto-Points.zip` to published releases
- `CHANGELOG.md` following Keep a Changelog format

## [2.4.3wfb] - 2026-09-09

### Added
- Slider `Min Upload GB` with 7 selectable tiers:
  - `500 pts -> 1 GB`
  - `1,250 pts -> 2.5 GB`
  - `2,500 pts -> 5 GB`
  - `10,000 pts -> 20 GB`
  - `25,000 pts -> 50 GB`
  - `50,000 pts -> 100 GB`
  - `Variable -> All I can afford`

### Changed
- Replaced fixed 100 GiB purchase with tier-based purchase logic
- Variable tier buys `floor((min(points, 99999) - buffer) / 500)` GB (max 199 GB)
- Points cap of `99,999` enforced; buffer is always respected as reserve

## [2.4.2wfb] - 2026-06-25

### Fixed
- `GetSeedBonusAsync` now uses `?uid=` instead of `?id=` — API was returning wrong points due to incorrect parameter name

## [2.4.1wfb] - 2026-06-24

### Fixed
- Remaining points and points-spent display now calculated correctly after purchase (was showing 0 / full balance due to API returning stale data)

## [2.4wfb] - 2026-06-23

### Added
- Fork branding by wildfirebill
- Points/Min tracking — shows last scan points and estimated earning rate
- Renamed executable to `Mam Auto Points wfb.exe`

### Changed
- Purchase logic now buys exactly 100 GiB for 50,000 points when balance reaches 60,100
- Timer default 15 minutes (was 12 hours), minimum 3 minutes
- VIP default now unchecked

## [2.3] - 2026-06-23

### Added
- Freeleech Wedge option (50,000 pts each)
- Scrollable instructions dialog
- Persistent totals, Next Run scheduling, and >24h countdown fix
- Fixed false upload summary / totals when no points spent

### Changed
- More efficient spending (no purchase loop needed)
- Updated GB values for new site rules

## [2.1] - 2025-12-26

### Added
- Play LOTTO button
- Millionaires Club button
- Unified fixed-width layout
- System error notification on failure to run
- Save states for settings and cookie