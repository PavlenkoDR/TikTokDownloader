using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TikTokDownloader
{
    struct SettingsJsonData
    {
        public string language;
    }

    static class Settings
    {
        static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings");

        public static void SetLanguage(in string language)
        {
            CultureInfo.CurrentCulture = new CultureInfo(language);
            var obj = new SettingsJsonData { language = language };
            var jsonString = JsonConvert.SerializeObject(obj);
            File.WriteAllText(path, jsonString);
        }
        public static string GetLanguage()
        {
            return CultureInfo.CurrentCulture.Name;
        }

        public static void Load()
        {
            var language = CultureInfo.CurrentCulture.Name;
            if (File.Exists(path))
            {
                var jsonString = File.ReadAllText(path);
                var obj = JsonConvert.DeserializeObject<SettingsJsonData>(jsonString);
                if (obj.language != null)
                {
                    language = obj.language;
                }
            }
            CultureInfo.CurrentCulture = new CultureInfo(language);
        }
    }
}
