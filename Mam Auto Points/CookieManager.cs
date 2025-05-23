using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MAMAutoPoints
{
    public static class CookieManager
    {
        public static async Task<Dictionary<string, string>> LoadCookiesAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var dict = new Dictionary<string, string>();
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "");
                    throw new Exception("Cookies file created. Please update it with your session cookie.");
                }
                string cookieValue = File.ReadAllText(filePath).Trim();
                dict["mam_id"] = cookieValue;
                return dict;
            });
        }
    }
}
