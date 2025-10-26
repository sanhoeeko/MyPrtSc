using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace MyPrtSc
{
    public class AppConfig
    {
        static string config_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
        private Dictionary<string, string> config_dict = new Dictionary<string, string>();

        public AppConfig()
        {
        }

        public static void EnsureConfigExists()
        {
            if (!File.Exists(config_path))
            {
                byte[] exeBytes = Properties.Resources.default_config;
                File.WriteAllBytes(config_path, exeBytes);
            }
        }

        public static AppConfig Load()
        {
            EnsureConfigExists();
            var config = new AppConfig();
            
            try
            {
                var lines = File.ReadAllLines(config_path);
                config.config_dict.Clear();
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // 跳过空行和注释行
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;
                    
                    // 解析 key = value 格式
                    var parts = trimmedLine.Split(new char[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        config.config_dict[key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"配置文件读取失败: {ex.Message}");
            }
            
            return config;
        }

        // 访问属性字典
        public string GetString(string key)
        {
            return config_dict.ContainsKey(key) ? config_dict[key] : null;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (config_dict.ContainsKey(key))
            {
                return bool.TryParse(config_dict[key], out bool result) ? result : defaultValue;
            }
            return defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (config_dict.ContainsKey(key))
            {
                return int.TryParse(config_dict[key], out int result) ? result : defaultValue;
            }
            return defaultValue;
        }
    }
}