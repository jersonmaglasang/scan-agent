using System;
using System.Configuration;
using System.IO;

namespace ScanAgent
{
    public class SettingsManager
    {
        private static SettingsManager instance;
        private Configuration config;

        public static SettingsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SettingsManager();
                }
                return instance;
            }
        }

        private SettingsManager()
        {
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
            catch
            {
                // Fallback if config file doesn't exist
                config = null;
            }
        }

        public string ScanFolder
        {
            get
            {
                string folder = GetSetting("ScanFolder");
                if (string.IsNullOrEmpty(folder))
                {
                    // Default to My Documents\Scans
                    folder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Scans"
                    );

                    // Create folder if it doesn't exist
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                }
                return folder;
            }
            set
            {
                SetSetting("ScanFolder", value);
            }
        }

        public string WebHookUrl
        {
            get
            {
                return GetSetting("WebHookUrl");
            }
            set
            {
                SetSetting("WebHookUrl", value);
            }
        }

        public bool AutoStart
        {
            get
            {
                string value = GetSetting("AutoStart");
                return value == "true";
            }
            set
            {
                SetSetting("AutoStart", value ? "true" : "false");
            }
        }

        public int WebSocketPort
        {
            get
            {
                string value = GetSetting("WebSocketPort");
                int port;
                if (int.TryParse(value, out port) && port > 0)
                {
                    return port;
                }
                return 8124; // Default port
            }
            set
            {
                SetSetting("WebSocketPort", value.ToString());
            }
        }

        public string[] ScannerPrograms
        {
            get
            {
                string value = GetSetting("ScannerPrograms");
                if (string.IsNullOrEmpty(value))
                {
                    return new string[0];
                }
                return value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                if (value == null || value.Length == 0)
                {
                    SetSetting("ScannerPrograms", "");
                }
                else
                {
                    SetSetting("ScannerPrograms", string.Join("|", value));
                }
            }
        }

        private string GetSetting(string key)
        {
            try
            {
                if (config != null && config.AppSettings.Settings[key] != null)
                {
                    return config.AppSettings.Settings[key].Value;
                }
            }
            catch { }
            return string.Empty;
        }

        private void SetSetting(string key, string value)
        {
            try
            {
                if (config != null)
                {
                    if (config.AppSettings.Settings[key] == null)
                    {
                        config.AppSettings.Settings.Add(key, value);
                    }
                    else
                    {
                        config.AppSettings.Settings[key].Value = value;
                    }
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Error saving setting: " + ex.Message);
            }
        }
    }
}

