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

        private static HttpClient CreateHttpClient(Dictionary<string, string> cookies)
        {
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Uri("https://www.myanonamouse.net"), new Cookie("mam_id", cookies["mam_id"]));
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
            using (var client = CreateHttpClient(cookies))
            {
                string requestUrl = MAM_API_ENDPOINT + "?snatch_summary";
                var response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                string responseContent = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    if (doc.RootElement.TryGetProperty("uid", out JsonElement uidProp))
                    {
                        return uidProp.ValueKind == JsonValueKind.Number ? uidProp.GetInt64().ToString() : uidProp.GetString() ?? "";
                    }
                }
                return "";
            }
        }

        public static async Task<Dictionary<string, JsonElement>> GetUserSummaryAsync(Dictionary<string, string> cookies)
        {
            using (var client = CreateHttpClient(cookies))
            {
                string requestUrl = MAM_API_ENDPOINT + "?snatch_summary";
                var response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    var dict = new Dictionary<string, JsonElement>();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.Clone();
                    }
                    return dict;
                }
            }
        }

        public static async Task<int> GetSeedBonusAsync(Dictionary<string, string> cookies, string mamUid)
        {
            using (var client = CreateHttpClient(cookies))
            {
                string url = "https://www.myanonamouse.net/jsonLoad.php?id=" + mamUid;
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseContent = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    if (doc.RootElement.TryGetProperty("seedbonus", out JsonElement sbProp) && sbProp.TryGetInt32(out int seedBonus))
                    {
                        return seedBonus;
                    }
                }
                return 0;
            }
        }

        public static async Task<DateTime> GetVipExpiryAsync(Dictionary<string, string> cookies)
        {
            using (var client = CreateHttpClient(cookies))
            {
                var response = await client.GetAsync(MAM_API_ENDPOINT);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    string vipUntil = "1970-01-01 00:00:00";
                    if (doc.RootElement.TryGetProperty("vip_until", out JsonElement vipProp))
                    {
                        vipUntil = vipProp.GetString() ?? "1970-01-01 00:00:00";
                    }
                    return DateTime.TryParse(vipUntil, out DateTime vipExpiry) ? vipExpiry : new DateTime(1970, 1, 1);
                }
            }
        }

        public static async Task<Dictionary<string, JsonElement>> SendCurlRequestAsync(string url, Dictionary<string, string> cookies)
        {
            using (var client = CreateHttpClient(cookies))
            {
                var response = await client.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP request failed with status code {response.StatusCode}: {json}");
                }
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var dict = new Dictionary<string, JsonElement>();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.Clone();
                    }
                    return dict;
                }
            }
        }

        public static string GetPointsUrl(int gb, string timestamp)
        {
            return POINTS_URL + gb.ToString() + "&_=" + timestamp;
        }

        public static string GetVipUrl(string timestamp)
        {
            return VIP_URL_TEMPLATE.Replace("{timestamp}", timestamp);
        }
    }
}
