# MAMAutoPoints

**Original creator:** [Plungis](https://github.com/Plungis)
**Maintained by:** wildfirebill

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue)](https://github.com/curiousgamez-mam/MAM-Spender)
[![Language](https://img.shields.io/badge/Language-C%23-green)](https://dotnet.microsoft.com/)
[![Framework](https://img.shields.io/badge/Framework-.NET%208-purple)](https://dotnet.microsoft.com/)

**MAMAutoPoints** is a Windows Forms (.NET 8) automation tool for **MyAnonAMouse (MAM)** — a private e-learning tracker. It monitors your seed bonus points and automatically purchases upload credit and/or VIP membership when configurable thresholds are met. No browser or manual interaction required.

---

## Features

- **Automated Upload Credit Purchases** — Slider sets your minimum upload credit from 1 GiB (500 pts) up to 100 GiB (50,000 pts), or **Variable** mode buys *all you can afford* (capped at 99,999 pts / 199 GiB)
- **VIP Auto-Renewal** — Optionally purchases VIP membership when your remaining period drops to 83 days or less (unchecked by default)
- **Freeleech Wedge Support** — Optionally buy FL wedges (50,000 pts) before or instead of upload credit
- **Points/Min Tracking** — Displays last scan points total and estimates your points-per-minute earning rate across sessions
- **Customizable Schedule** — Runs every 15 minutes by default (configurable down to 3 minutes)
- **Persistent Settings** — Saves all configuration, cookie path, and cumulative totals across restarts
- **System Tray Integration** — Minimizes to tray with background execution
- **Error Notifications** — Optional balloon tips on automation failures
- **One-Click Links** — Quick access to MAM Lotto and Millionaires Club from the UI
- **Self-Contained EXE** — No .NET runtime required; single-file portable executable, built automatically on every release

---

## Download

Grab the latest portable `.exe` from the [Releases page](https://github.com/curiousgamez-mam/MAM-Spender/releases) — no installation or runtime needed.

---

## How It Works

1. The app authenticates using a **mam_id session cookie** tied to your IP address
2. Every N minutes (default: 15), it fetches your current seed bonus balance from the MAM API
3. Depending on the slider setting, it purchases upload credit at the selected tier:
   - `500 pts -> 1 GB` | `1,250 pts -> 2.5 GB` | `2,500 pts -> 5 GB` | `10,000 pts -> 20 GB` | `25,000 pts -> 50 GB` | `50,000 pts -> 100 GB`
   - `Variable` -> buys `floor((min(points, 99999) - buffer) / 500)` GB (max 199 GiB)
4. Purchases trigger when `points >= tierCost + buffer` (fixed) or `points >= 500 + buffer` (Variable)
5. If VIP renewal is enabled and your VIP has ≤ 83 days remaining, it renews first
6. If FL Wedge mode is enabled, it buys a Freeleech Wedge (50,000 pts) before or instead of upload GB
7. Results and points/min are logged and displayed in the UI

---

## Usage

### Creating Your Cookie File

1. Launch the application
2. Click **"Create my Cookie!"**
3. Enter the unique security string from your MyAnonAMouse account (found under Menu → Preferences → Security → Create Session)
4. Save the file as `MAM.cookies`

### Configuring Settings

| Setting | Description | Default |
|---|---|---|
| Buy Max VIP? | Auto-renew VIP when ≤83 days remain | Off |
| Buy FL Wedge before GB? | Purchase FL Wedge before upload credit | Off |
| Buy ONLY Freeleech Wedges | Skip upload credit, only buy FL Wedges | Off |
| Min Upload GB (slider) | 1 GB / 2.5 GB / 5 GB / 20 GB / 50 GB / 100 GB / Variable (all you can afford) | 100 GB |
| Points Buffer | Minimum reserve kept after purchase (e.g. 2.5 GB needs 6,250 pts with 5,000 buffer) | 10,000 |
| Next Run Delay | Check interval in minutes (min: 3) | 15 |

### Running

Click **"Run Script"** to start the scheduled automation. Click **"Run Script Immediately"** to trigger an unscheduled run.

---

## Requirements

- Windows 10 or later (64-bit)
- An active MyAnonAMouse account
- A valid mam_id session cookie (generated in-app)

---

## Tech Stack

- **Language:** C# (.NET 8, Windows Forms)
- **HTTP:** `HttpClient` with cookie-based auth
- **API:** MyAnonAMouse JSON API (`myanonamouse.net`)
- **Build:** Single-file self-contained publish via `dotnet publish`, automated by GitHub Actions on every release

---

## Release Notes

See [CHANGELOG.md](CHANGELOG.md) for the full history.

### v2.4.5wfb (current)
- **Fix:** "Buy ONLY Freeleech Wedges" checkbox now persists across restarts — it was the only setting missing from the config file save/load logic

### v2.4.4wfb
- **Added:** `Save Settings` button — writes all settings to the config file and confirms the saved path
- **Fix:** Settings now persist correctly — the slider tier was being saved from a stale hidden value and always restored to `100 GB`; it now saves and restores your actual selection
- **CI:** Workflow actions updated to Node 24-compatible releases (checkout v7, setup-dotnet v6, upload-artifact v7)

### v2.4.3wfb
- **Feature:** Slider `Min Upload GB` with 7 tiers: `500 pts=1 GB` / `1,250=2.5 GB` / `2,500=5 GB` / `10k=20 GB` / `25k=50 GB` / `50k=100 GB` / `Variable=All I can afford` (cap `99,999 pts` = `199 GB` max)
- **Behavior:** Slider sets minimum upload credit; `Variable` buys `floor((min(points,99999)-buffer)/500)` GB
- **CI:** GitHub Actions workflow builds the single-file EXE and attaches it to each release

---

## Keywords

MAM, MyAnonAMouse, bonus points, seed bonus, upload credit, private tracker automation, MAM points spender, freeleech wedge, VIP renewal, torrent tracker tool, MyAnonAMouse bot, MAM upload GB, bonus point manager

---

## Disclaimer

This tool is **not affiliated** with MyAnonAMouse. Use at your own risk.
