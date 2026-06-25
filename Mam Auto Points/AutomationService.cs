using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace MAMAutoPoints
{
    public static class AutomationService
    {
        public class UserSummary
        {
            public string? Username { get; set; }
            public string VipExpires { get; set; } = "N/A";
            public string Downloaded { get; set; } = "N/A";
            public string Uploaded { get; set; } = "N/A";
            public string Ratio { get; set; } = "N/A";
        }

        // === MAM HARD RULES ===
        private const int POINTS_PER_BLOCK = 50000;
        private const int GB_PER_BLOCK = 100;
        private const int MIN_POINTS_FOR_PURCHASE = 60100;

        // === FREELEECH WEDGE ===
        private const int FL_WEDGE_COST = 50000;

        public static async Task RunAutomationAsync(
            string cookieFile,
            int pointsBuffer,
            bool vipEnabled,
            bool buyFlBeforeGb,
            bool flOnlyMode,
            int nextRunHours,
            Action<string> log,
            Action<UserSummary> updateUserInfo,
            Action<int, int> updateTotals,
            Action<int>? updateCurrentPoints = null)
        {
            try
            {
                log("Starting automation process.");

                var cookies = await CookieManager.LoadCookiesAsync(cookieFile);

                // ================= SESSION CHECK =================
                string mamUid = await ApiHelper.GetSessionIdAsync(cookies);
                if (string.IsNullOrEmpty(mamUid))
                {
                    log("Session invalid. Please check your cookie file.");
                    return;
                }

                log("Session valid.");

                // ================= USER INFO =================
                try
                {
                    var userSummaryDict = await ApiHelper.GetUserSummaryAsync(cookies);

                    var summary = new UserSummary
                    {
                        Username = userSummaryDict.TryGetValue("username", out var userElem)
                            ? userElem.GetString()
                            : "N/A",

                        VipExpires = userSummaryDict.TryGetValue("vip_until", out var vipElem)
                            ? FormatVipExpires(vipElem)
                            : "N/A",

                        Downloaded = userSummaryDict.TryGetValue("downloaded", out var dlElem)
                            ? dlElem.GetString() ?? "N/A"
                            : "N/A",

                        Uploaded = userSummaryDict.TryGetValue("uploaded", out var ulElem)
                            ? ulElem.GetString() ?? "N/A"
                            : "N/A",

                        Ratio = userSummaryDict.TryGetValue("ratio", out var ratioElem)
                            ? ratioElem.ToString()
                            : "N/A"
                    };

                    updateUserInfo(summary);
                }
                catch (Exception ex)
                {
                    log("Failed to update user information: " + ex.Message);
                }

                // ================= POINTS =================
                log("Collecting current points.");
                int points = await ApiHelper.GetSeedBonusAsync(cookies, mamUid);
                int initialPoints = points;

                if (points <= 0)
                {
                    log("Failed to retrieve bonus points.");
                    return;
                }

                log($"Current points: {points}");
                updateCurrentPoints?.Invoke(points);

                bool vipPurchased = false;

                // ================= VIP =================
                if (vipEnabled)
                {
                    DateTime vipExpiry = await ApiHelper.GetVipExpiryAsync(cookies);
                    TimeSpan vipRemaining = vipExpiry - DateTime.Now;

                    log($"Current VIP expiry: {vipExpiry:MMM dd, yyyy h:mm tt} ({vipRemaining.TotalDays:F1} days remaining)");

                    if (vipRemaining.TotalDays <= 83)
                    {
                        string timestamp =
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                        string vipUrl = ApiHelper.GetVipUrl(timestamp);
                        var vipResult = await ApiHelper.SendCurlRequestAsync(vipUrl, cookies);

                        if (vipResult.TryGetValue("success", out var successElem) &&
                            successElem.GetBoolean())
                        {
                            log("VIP purchase successful!");
                            vipPurchased = true;
                        }
                        else
                        {
                            log("VIP purchase failed or not available.");
                        }
                    }
                    else
                    {
                        log("VIP purchase not required; current VIP period exceeds threshold (83 days).");
                    }
                }

                // ================= FREELEECH WEDGE =================
                int flWedgesPurchased = 0;
                bool shouldBuyWedge = buyFlBeforeGb || flOnlyMode;

                if (shouldBuyWedge)
                {
                    if (points < FL_WEDGE_COST + pointsBuffer)
                    {
                        log("Not enough points to buy Freeleech Wedge (requires 50,000 + buffer).");
                    }
                    else
                    {
                        log("Attempting Freeleech Wedge purchase...");

                        bool success = await ApiHelper.BuyFreeleechWedgeAsync(
                            cookies,
                            mamUid,
                            log
                        );

                        if (success)
                        {
                            flWedgesPurchased = 1;
                            points = await ApiHelper.GetSeedBonusAsync(cookies, mamUid);
                            log("Freeleech Wedge purchase confirmed.");
                        }
                        else
                        {
                            log("Freeleech Wedge purchase failed (points did not decrease).");
                        }
                    }
                }

                // ================= FL-ONLY MODE =================
                if (flOnlyMode)
                {
                    int runPointsSpentFlOnly = initialPoints - points;

                    updateTotals(0, Math.Max(runPointsSpentFlOnly, 0));

                    log("FL-only mode enabled — skipping upload GB purchases.");
                    log("=== Summary ===");
                    log($"VIP Purchase: {(vipPurchased ? "Yes" : "No")}");
                    log($"Freeleech Wedges Purchased: {flWedgesPurchased}");
                    log("Upload GB Purchased: Skipped (FL-only mode)");
                    log($"Points Spent This Run: {runPointsSpentFlOnly}");
                    return;
                }

                // ================= UPLOAD GB =================
                int actualPurchasedGB = 0;

                if (points < MIN_POINTS_FOR_PURCHASE)
                {
                    log($"Not enough points ({points}). Need at least {MIN_POINTS_FOR_PURCHASE} to purchase {GB_PER_BLOCK} GiB");
                }
                else
                {
                    int requestedGB = GB_PER_BLOCK;
                    log($"{points} points available. Purchasing {requestedGB} GiB of upload for {POINTS_PER_BLOCK} points");

                    string url = ApiHelper.GetPointsUrl(requestedGB);
                    await ApiHelper.SendCurlRequestAsync(url, cookies);

                    int newPoints = points - POINTS_PER_BLOCK;
                    log($"After purchase, points: {newPoints}");

                    points = newPoints;
                    actualPurchasedGB = requestedGB;
                }

                // ================= TOTALS =================
                int runPointsSpent = initialPoints - points;

                if (runPointsSpent > 0)
                {
                    updateTotals(actualPurchasedGB, runPointsSpent);
                }
                else
                {
                    updateTotals(0, 0);
                }

                // ================= SUMMARY =================
                log("=== Summary ===");
                log($"VIP Purchase: {(vipPurchased ? "Yes" : "No")}");
                log($"Freeleech Wedges Purchased: {flWedgesPurchased}");

                if (actualPurchasedGB > 0)
                {
                    log($"Total Upload GB Purchased (this run): {actualPurchasedGB} GiB");
                }
                else
                {
                    log("No upload credit purchased this run.");
                }

                log($"Points Spent This Run: {runPointsSpent}");
            }
            catch (Exception ex)
            {
                log("An unexpected error occurred: " + ex.Message);
            }
        }

        private static string FormatVipExpires(JsonElement vipElem)
        {
            string vipStr = vipElem.GetString() ?? "";
            return DateTime.TryParse(vipStr, out DateTime vipDate)
                ? vipDate.ToString("MMM dd, yyyy h:mm tt")
                : vipStr;
        }
    }
}
