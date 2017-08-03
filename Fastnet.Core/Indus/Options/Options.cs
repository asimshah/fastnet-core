using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace Fastnet.Core.Indus
{
    public abstract class Options
    {
        internal static string settingsText;
        internal static bool GetSettings()
        {
            if (settingsText == null)
            {
                var filename = GetSettingsFileName();
                if (!File.Exists(filename))
                {
                    return false;
                }
                settingsText = File.ReadAllText(filename);

            }
            return true;
        }
        private static string GetSettingsFileName()
        {
            return Path.Combine(GetRootFolder(), "settings.json");
        }
        private static string GetRootFolder()
        {
            string folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return folder;
        }
        internal static void Refresh()
        {
            settingsText = null;
        }
    }
    public class OptionsProvider
    {
        public static void Refresh()
        {
            Options.Refresh();
        }
        public static T Get<T>() where T : Options, new()
        {
            if (Options.GetSettings())
            {
                // we have some settings text
                string sectionName = typeof(T).Name;
                var subItem = JObject.Parse(Options.settingsText);
                var subtext = subItem[sectionName];
                if(subtext != null)
                {
                    return subtext.ToObject<T>();
                }
            }
            return new T();
            //var op = new OptionProvider();
            //var text = op.GetSettings();
            
            
            //var subtext = subItem[sectionName];
            //var options = subtext?.ToObject<T>();
            //return options;
        }
        //private string GetSettings()
        //{
        //    if (settingsText == null)
        //    {
        //        var filename = GetSettingsFileName();
        //        if (!File.Exists(filename))
        //        {
        //            CreateSettingsFile();
        //        }
        //        settingsText = File.ReadAllText(filename);
        //    }
        //    return settingsText;
        //}
        //protected void Refresh()
        //{
        //    settingsText = null;
        //}
        //private void CreateSettingsFile()
        //{
        //    //string defaultText = @"{
        //    //    ""LoggingOptions"": {
        //    //        ""LogFolder"" : ""default"",
        //    //        ""ShortCategoryNames"": true,
        //    //        ""Filters"": [
        //    //                {
        //    //            ""Category"": ""Default"",
        //    //            ""Level"": ""Trace""
        //    //            }
        //    //        ]
        //    //    }
        //    //}";
        //    string defaultText = @"{
        //        ""LoggingOptions"": {
        //            ""LogFolder"" : ""default"",
        //        }
        //    }";
        //    var filename = GetSettingsFileName();
        //    File.WriteAllText(filename, defaultText);
        //}
        //private string GetSettingsFileName()
        //{
        //    return Path.Combine(GetRootFolder(), "settings.json");
        //}
        //private string GetRootFolder()
        //{
        //    //return "";
        //    string folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        //    return folder;
        //}
    }
}
