# MAMAutoPoints

**Original creator:** [Plungis](https://github.com/Plungis)
**Modified by:** wildfirebill

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue)](https://github.com/Plungis/MAM-Spender)
[![Language](https://img.shields.io/badge/Language-C%23-green)](https://dotnet.microsoft.com/)
[![Framework](https://img.shields.io/badge/Framework-.NET%208-purple)](https://dotnet.microsoft.com/)

**MAMAutoPoints** is a Windows Forms (.NET 8) automation tool for **MyAnonAMouse (MAM)** — a private e-learning tracker. It monitors your seed bonus points and automatically purchases upload credit and/or VIP membership when configurable thresholds are met. No browser or manual interaction required.

---

## Features

- **Automated Upload Credit Purchases** — Spends 50,000 bonus points for 100 GiB of upload credit when your balance reaches 60,100
- **VIP Auto-Renewal** — Optionally purchases VIP membership when your remaining period drops to 83 days or less (unchecked by default)
- **Freeleech Wedge Support** — Optionally buy FL wedges (50,000 pts) before or instead of upload credit
- **Points/Min Tracking** — Displays last scan points total and estimates your points-per-minute earning rate across sessions
- **Customizable Schedule** — Runs every 15 minutes by default (configurable down to 3 minutes)
- **Persistent Settings** — Saves all configuration, cookie path, and cumulative totals across restarts
- **System Tray Integration** — Minimizes to tray with background execution
- **Error Notifications** — Optional balloon tips on automation failures
- **One-Click Links** — Quick access to MAM Lotto and Millionaires Club from the UI
- **Self-Contained EXE** — No .NET runtime required; single-file portable executable

---

## Download

Grab the latest portable `.exe` from the [Releases page](https://github.com/Plungis/MAMAutoPoints/releases) — no installation or runtime needed.

---

## How It Works

1. The app authenticates using a **mam_id session cookie** tied to your IP address
2. Every N minutes (default: 15), it fetches your current seed bonus balance from the MAM API
3. If your balance is ≥ **60,100 points**, it purchases **100 GiB of upload credit** for **50,000 points** (leaving ≥ 10,100 in reserve)
4. If VIP renewal is enabled and your VIP has ≤ 83 days remaining, it renews first
5. If FL Wedge mode is enabled, it buys a Freeleech Wedge (50,000 pts) before or instead of upload GB
6. Results and points/min are logged and displayed in the UI

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
| Points Buffer | Minimum reserve; purchase triggers at 60,100 | 10,000 |
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
- **Build:** Single-file self-contained publish via `dotnet publish`

---

## Release Notes

### v2.4.2wfb (current)
- **Bug fix:** `GetSeedBonusAsync` now uses `?uid=` instead of `?id=` — API was returning wrong points due to incorrect parameter name

### v2.4.1wfb
- **Bug fix:** Remaining points and points-spent display now calculated correctly after purchase (was showing 0 / full balance due to API returning stale data)

### v2.4wfb
- Branded fork by wildfirebill — credits to original creator Plungis
- **Purchase logic:** Now buys exactly 100 GiB for 50,000 points when balance reaches 60,100
- **Timer changed:** Default 15 minutes (was 12 hours), minimum 3 minutes
- **Added Points/Min tracking:** Shows last scan points and estimated earning rate
- **VIP default:** Now unchecked by default

### v2.3
- Original release continues upstream under Plungis

### v2.1
- Added Play LOTTO Button
- Added Millionaires Club Button
- Unified Fixed-Width Layout
- Added system error notification on failure to run
- Added Save States for Settings and Cookie

---

## Keywords

MAM, MyAnonAMouse, bonus points, seed bonus, upload credit, private tracker automation, MAM points spender, freeleech wedge, VIP renewal, torrent tracker tool, MyAnonAMouse bot, MAM upload GB, bonus point manager

---

## Disclaimer

This tool is **not affiliated** with MyAnonAMouse. Use at your own risk.
