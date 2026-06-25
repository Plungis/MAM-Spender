using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MAMAutoPoints
{
    public static class ApiHelper
    {
        private const string MAM_API_ENDPOINT = "https://www.myanonamouse.net/jsonLoad.php";
        private const string POINTS_URL = "https://www.myanonamouse.net/json/bonusBuy.php/?spendtype=upload&amount=";
        private const string VIP_URL_TEMPLATE = "https://www.myanonamouse.net/json/bonusBuy.php/?spendtype=VIP&duration=max&_={timestamp}";

        // === FREELEECH WEDGE ===
        private const int FL_WEDGE_COST = 50000;
        private const string FL_WEDGE_URL_TEMPLATE =
            "https://www.myanonamouse.net/json/bonusBuy.php/?spendtype=wedges&source=points&_={timestamp}";

        private static HttpClient CreateHttpClient(Dictionary<string, string> cookies)
        {
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(
                new Uri("https://www.myanonamouse.net"),
                new Cookie("mam_id", cookies["mam_id"])
            );

            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "C#-HttpClient");
            return client;
        }

        public static async Task<string> GetSessionIdAsync(Dictionary<string, string> cookies)
        {
            using var client = CreateHttpClient(cookies);
            var response = await client.GetAsync(MAM_API_ENDPOINT + "?snatch_summary");
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.TryGetProperty("uid", out var uid)
                ? uid.ToString()
                : "";
        }

        public static async Task<Dictionary<string, JsonElement>> GetUserSummaryAsync(
            Dictionary<string, string> cookies)
        {
            using var client = CreateHttpClient(cookies);
            var response = await client.GetAsync(MAM_API_ENDPOINT + "?snatch_summary");
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var dict = new Dictionary<string, JsonElement>();

            foreach (var prop in doc.RootElement.EnumerateObject())
                dict[prop.Name] = prop.Value.Clone();

            return dict;
        }

        public static async Task<int> GetSeedBonusAsync(
            Dictionary<string, string> cookies,
            string mamUid)
        {
            using var client = CreateHttpClient(cookies);
            var response = await client.GetAsync($"{MAM_API_ENDPOINT}?uid={mamUid}");
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return doc.RootElement.TryGetProperty("seedbonus", out var sb) &&
                   sb.TryGetInt32(out int val)
                ? val
                : 0;
        }

        public static async Task<DateTime> GetVipExpiryAsync(Dictionary<string, string> cookies)
        {
            using var client = CreateHttpClient(cookies);
            var response = await client.GetAsync(MAM_API_ENDPOINT);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return doc.RootElement.TryGetProperty("vip_until", out var vip) &&
                   DateTime.TryParse(vip.GetString(), out var dt)
                ? dt
                : new DateTime(1970, 1, 1);
        }

        public static async Task<Dictionary<string, JsonElement>> SendCurlRequestAsync(
            string url,
            Dictionary<string, string> cookies)
        {
            using var client = CreateHttpClient(cookies);
            var response = await client.GetAsync(url);

            string json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {response.StatusCode}: {json}");

            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, JsonElement>();

            foreach (var prop in doc.RootElement.EnumerateObject())
                dict[prop.Name] = prop.Value.Clone();

            return dict;
        }

        public static string GetPointsUrl(int gb) => POINTS_URL + gb;

        public static string GetVipUrl(string timestamp) =>
            VIP_URL_TEMPLATE.Replace("{timestamp}", timestamp);

        // ================= FREELEECH WEDGE (MATCHES BASH SCRIPT) =================

        public static async Task<bool> BuyFreeleechWedgeAsync(
            Dictionary<string, string> cookies,
            string mamUid,
            Action<string> log)
        {
            int before = await GetSeedBonusAsync(cookies, mamUid);

            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string url = FL_WEDGE_URL_TEMPLATE.Replace("{timestamp}", timestamp);

            log($"Wedge request URL: {url}");

            await SendCurlRequestAsync(url, cookies);
            await Task.Delay(800);

            int after = await GetSeedBonusAsync(cookies, mamUid);

            log($"Wedge verification: before={before}, after={after}");

            return (before - after) >= FL_WEDGE_COST;
        }
    }
}
