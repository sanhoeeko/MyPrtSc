using MyPrtSc.Properties;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Forms;

namespace MyPrtSc
{
    public class AppConfig
    {
        static string config_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public string DocString { get; set; }
        public string BaseDir { get; set; }
        public bool IfAutoConvert {  get; set; }
        public bool IfOptimizePng {  get; set; }

        public AppConfig()
        {
            BaseDir = @"D:\MyPrtSc_screenshot";
            DocString = "# 请修改 baseDir 值并重启程序\n# 示例：\"baseDir\": \"C:\\MyScreenshots\"";
        }

        public static void EnsureConfigExists()
        {
            try
            {
                if (!File.Exists(config_path))
                {
                    File.WriteAllBytes(config_path, Resources.default_config);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法生成配置文件: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(config_path, json);
            }
            catch { }
        }

        public static AppConfig Load()
        {
            EnsureConfigExists();
            var json = File.ReadAllText(config_path);
            return JsonConvert.DeserializeObject<AppConfig>(json);
        }
    }
}