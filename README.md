# MAMAutoPoints

MAMAutoPoints is a Windows Forms application that automates the spending of bonus points on MyAnonAMouse. It monitors your bonus point balance and, when conditions are met, automatically purchases upload credit—and optionally extends your VIP membership.

---

## Table of Contents

- [Overview](#overview)
- [Download](#download)
- [Usage](#usage)
  - [Creating Your Cookie File](#creating-your-cookie-file)
  - [Configuring Settings](#configuring-settings)
- [Release Notes](#release-notes)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

---

## Overview

MAMAutoPoints simplifies managing your bonus points:
- **Automated Bonus Points Spending:**  
  When your bonus point count exceeds a specified threshold, the program automatically spends your points to purchase upload credit.
  
- **VIP Purchase Logic:**  
  If enabled, the application will purchase VIP membership only if your current VIP period is 83 days or less (maintaining a 3-day buffer before reaching the 90-day cap).

- **Customizable Settings:**  
  Adjust settings such as the Points Buffer and Next Run Delay directly from the user interface.

---

## Download

You can download the latest release of MAMAutoPoints from the [Releases](https://github.com/Plungis/MAMAutoPoints/releases) page. Simply download the provided `.exe` file and run it on your Windows machine.

*No need to clone or compile the source code—just download and start automating!*

---

## Usage

### Creating Your Cookie File

1. Launch the application.
2. Click **"Create my Cookie!"**.
3. Enter the unique security string from your MyAnonAMouse account (this is the long string shown after submitting your IP under Security settings).
4. Save the file when prompted. This file will be used for authentication.

### Configuring Settings

- **Buy Max VIP?:**  
  Check this box if you want the application to automatically purchase VIP membership when your remaining VIP period is 83 days or less.

- **Points Buffer:**  
  The default value is set to `10000`. This ensures that even after purchasing upload credit, you maintain a reserve of bonus points.

- **Next Run Delay:**  
  Set this value (in hours) to schedule the next automation run. The default is `12` hours.

- **Cookies File:**  
  Use the **Select File** button to choose your cookie file.

### Running the Automation

Click **"Run Script"** to start the automation process. The log output will display a concise summary, including whether a VIP purchase occurred and the total number of GB purchased.

---

## Release Notes

### v2.1
- **Added Play LOTTO Button**
- **Added Millionares Club Button**
- **Unified Fixed-Width Layout**
- **Added system error notification on failure to run**
- **Added Save States for Settings and Cookie**




