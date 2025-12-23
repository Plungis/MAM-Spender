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

        public static async Task RunAutomationAsync(
            string cookieFile,
            int pointsBuffer,
            bool vipEnabled,
            int nextRunHours,
            Action<string> log,
            Action<UserSummary> updateUserInfo,
            Action<int, int> updateTotals)
        {
            try
            {
                log("Starting automation process.");
                var cookies = await CookieManager.LoadCookiesAsync(cookieFile);
                var userSummaryDict = await ApiHelper.GetUserSummaryAsync(cookies);
                var summary = new UserSummary
                {
                    Username = userSummaryDict.TryGetValue("username", out var userElem) ? userElem.GetString() : "N/A",
                    VipExpires = userSummaryDict.TryGetValue("vip_until", out var vipElem) ? FormatVipExpires(vipElem) : "N/A",
                    Downloaded = userSummaryDict.TryGetValue("downloaded", out var dlElem) ? dlElem.GetString() ?? "N/A" : "N/A",
                    Uploaded = userSummaryDict.TryGetValue("uploaded", out var ulElem) ? ulElem.GetString() ?? "N/A" : "N/A",
                    Ratio = userSummaryDict.TryGetValue("ratio", out var ratioElem) ? ratioElem.ToString() : "N/A"
                };
                updateUserInfo(summary);

                string mamUid = await ApiHelper.GetSessionIdAsync(cookies);
                if (string.IsNullOrEmpty(mamUid))
                {
                    log("Session invalid. Please check your cookie file.");
                    return;
                }
                log("Session valid.");
                log("Collecting current points.");
                int points = await ApiHelper.GetSeedBonusAsync(cookies, mamUid);
                int initialPoints = points;
                if (points == 0)
                {
                    log("Failed to retrieve bonus points.");
                    return;
                }
                log($"Current points: {points}");

                bool vipPurchased = false;
                if (vipEnabled)
                {
                    DateTime vipExpiry = await ApiHelper.GetVipExpiryAsync(cookies);
                    TimeSpan vipRemaining = vipExpiry - DateTime.Now;
                    log($"Current VIP expiry: {vipExpiry:MMM dd, yyyy h:mm tt} ({vipRemaining.TotalDays:F1} days remaining)");
                    if (vipRemaining.TotalDays <= 83)
                    {
                        string timestamp = ((long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
                        string vipUrl = ApiHelper.GetVipUrl(timestamp);
                        var vipResult = await ApiHelper.SendCurlRequestAsync(vipUrl, cookies);
                        if (vipResult.TryGetValue("success", out var successElem) && successElem.GetBoolean())
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
                // intentional integer math, round down.
                int totalUploadGB = ((points - pointsBuffer) / 500);
                if(50 > totalUploadGB)
                {
                    // Can no longer purchase less than 50GiB via scripts.
                    log("Cannot purchase more than 50 GiB of upload - aborting");
                }
                else
                {
                    log($"{points} > {uploadRequired} - purchasing {totalUploadGB} GiB of upload");
                    string url = ApiHelper.GetPointsUrl(totalUploadGB);
                    await ApiHelper.SendCurlRequestAsync(url, cookies);
                    await Task.Delay(1000);
                    int newPoints = await ApiHelper.GetSeedBonusAsync(cookies, mamUid);
                    log($"After purchase, points: {newPoints}");
                    if (newPoints < points)
                    {
                        points = newPoints;
                    }
                    else
                    {
                        log("Purchase did not reduce points - aborting.");
                        return;
                    }
                }
                int runPointsSpent = initialPoints - points;
                updateTotals(totalUploadGB, runPointsSpent);
                log("=== Summary ===");
                log($"VIP Purchase: {(vipPurchased ? "Yes" : "No")}");
                log($"Total Upload GB Purchased (this run): {totalUploadGB} GiB");
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
            return DateTime.TryParse(vipStr, out DateTime vipDate) ? vipDate.ToString("MMM dd, yyyy h:mm tt") : vipStr;
        }
    }
}
