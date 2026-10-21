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

        // === UPLOAD GB SLIDER (1 GB .. All I can afford) ===
        // Kept for backward compat with old configs — maps to slider value
        public enum PurchaseTier
        {
            Gb1_500pts = 0,
            Gb2_5_1250pts = 1,
            Gb5_2500pts = 2,
            Gb20_10000pts = 3,
            Gb50_25000pts = 4,
            Gb100_50000pts = 5,
            Variable_MaxAffordable = 6
        }

        public static (int cost, double gb) GetTierCostGbPublic(PurchaseTier tier) => GetTierCostGb(tier);
        private static (int cost, double gb) GetTierCostGb(PurchaseTier tier)
        {
            return tier switch
            {
                PurchaseTier.Gb1_500pts => (500, 1),
                PurchaseTier.Gb2_5_1250pts => (1250, 2.5),
                PurchaseTier.Gb5_2500pts => (2500, 5),
                PurchaseTier.Gb20_10000pts => (10000, 20),
                PurchaseTier.Gb50_25000pts => (25000, 50),
                PurchaseTier.Gb100_50000pts => (50000, 100),
                PurchaseTier.Variable_MaxAffordable => (0, 0),
                _ => (50000, 100)
            };
        }

        public const int MAX_POINTS_CAP = 99999;
        public const int MIN_UPLOAD_GB = 1;
        public const int MAX_UPLOAD_GB = 199; // floor(99999/500)
        private const int POINTS_PER_GB = 500;

        // Legacy constants for backward compat
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
            Action<double, int> updateTotals,
            Action<int>? updateCurrentPoints = null,
            PurchaseTier purchaseTier = PurchaseTier.Gb100_50000pts,
            double customUploadGb = -1,
            bool useMaxAffordable = false)
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
                double actualPurchasedGB = 0;
                int actualPointsSpentGb = 0;

                // Resolve tier or slider value
                // If customUploadGb is set (>=1) use it; else fall back to PurchaseTier for backward compat
                bool sliderMode = customUploadGb >= MIN_UPLOAD_GB;
                double requestedGbFixed = 0;
                int tierCostFixed = 0;
                bool isVariable = useMaxAffordable || purchaseTier == PurchaseTier.Variable_MaxAffordable;

                if (!isVariable)
                {
                    if (sliderMode)
                    {
                        requestedGbFixed = Math.Clamp(customUploadGb, MIN_UPLOAD_GB, MAX_UPLOAD_GB);
                        tierCostFixed = (int)(requestedGbFixed * POINTS_PER_GB);
                    }
                    else
                    {
                        var (tierCost, tierGb) = GetTierCostGb(purchaseTier);
                        requestedGbFixed = tierGb;
                        tierCostFixed = tierCost;
                    }
                }

                if (isVariable)
                {
                    // All I can afford with MINIMUM threshold from slider
                    int cappedPoints = Math.Min(points, MAX_POINTS_CAP);
                    int availableForSpend = cappedPoints - pointsBuffer;
                    int maxAffordableGb = availableForSpend / POINTS_PER_GB;
                    maxAffordableGb = Math.Clamp(maxAffordableGb, 0, MAX_UPLOAD_GB);
                    int minimumGb = sliderMode ? (int)Math.Clamp(customUploadGb, MIN_UPLOAD_GB, MAX_UPLOAD_GB) : MIN_UPLOAD_GB;
                    int minimumCost = minimumGb * POINTS_PER_GB;
                    if (maxAffordableGb < MIN_UPLOAD_GB)
                    {
                        log($"Not enough points ({points}) for MAX purchase. Need at least {POINTS_PER_GB + pointsBuffer} (500 pts + {pointsBuffer} buffer) for {MIN_UPLOAD_GB} GiB (cap {MAX_POINTS_CAP} pts)");
                    }
                    else if (maxAffordableGb < minimumGb)
                    {
                        log($"Not enough points ({points}) for minimum {minimumGb} GiB ({minimumCost} pts + {pointsBuffer} buffer). Max affordable is {maxAffordableGb} GiB — waiting.");
                    }
                    else
                    {
                        double requestedGbVar = maxAffordableGb;
                        int costVar = maxAffordableGb * POINTS_PER_GB;
                        log($"{points} points available (buffer {pointsBuffer}, cap {MAX_POINTS_CAP}, min {minimumGb} GB). Purchasing MAX {requestedGbVar} GiB of upload for {costVar} points");

                        string urlVar = ApiHelper.GetPointsUrl(requestedGbVar);
                        await ApiHelper.SendCurlRequestAsync(urlVar, cookies);

                        points -= costVar;
                        actualPurchasedGB = requestedGbVar;
                        actualPointsSpentGb = costVar;
                        log($"After purchase, points: {points}");
                    }
                }
                else
                {
                    // Fixed slider value: need cost + buffer, capped at 99,999
                    int cappedPointsForCheck = Math.Min(points, MAX_POINTS_CAP);
                    int required = tierCostFixed + pointsBuffer;
                    if (cappedPointsForCheck < required)
                    {
                        log($"Not enough points ({points}). Need at least {required} to purchase {requestedGbFixed} GiB ({tierCostFixed} pts + {pointsBuffer} buffer)");
                    }
                    else
                    {
                        log($"{points} points available. Purchasing {requestedGbFixed} GiB of upload for {tierCostFixed} points");

                        string url = ApiHelper.GetPointsUrl(requestedGbFixed);
                        await ApiHelper.SendCurlRequestAsync(url, cookies);

                        points -= tierCostFixed;
                        actualPurchasedGB = requestedGbFixed;
                        actualPointsSpentGb = tierCostFixed;
                        log($"After purchase, points: {points}");
                    }
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
