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

                int totalUploadGB = 0;
                int[] gbValues = new int[] { 100, 20, 5, 1 };
                foreach (int gb in gbValues)
                {
                    int uploadRequired = gb * 500 + pointsBuffer;
                    while (points > uploadRequired)
                    {
                        log($"{points} > {uploadRequired} - purchasing {gb} GB of upload");
                        string timestamp = ((long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
                        string url = ApiHelper.GetPointsUrl(gb, timestamp);
                        await ApiHelper.SendCurlRequestAsync(url, cookies);
                        await Task.Delay(1000);
                        int newPoints = await ApiHelper.GetSeedBonusAsync(cookies, mamUid);
                        log($"After purchase, points: {newPoints}");
                        if (newPoints < points)
                        {
                            points = newPoints;
                            totalUploadGB += gb;
                        }
                        else
                        {
                            log("Purchase did not reduce points - aborting.");
                            return;
                        }
                    }
                }

                int runPointsSpent = initialPoints - points;
                updateTotals(totalUploadGB, runPointsSpent);
                log("=== Summary ===");
                log($"VIP Purchase: {(vipPurchased ? "Yes" : "No")}");
                log($"Total Upload GB Purchased (this run): {totalUploadGB} GB");
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
