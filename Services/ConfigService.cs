using System;
using System.IO;
using GameCheatHelper.Core.Models;
using Newtonsoft.Json;
using NLog;

namespace GameCheatHelper.Services
{
    /// <summary>
    /// 配置管理服务
    /// 负责加载、保存和管理应用程序配置
    /// </summary>
    public class ConfigService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _configPath;
        private AppConfig _config;

        /// <summary>
        /// 当前配置
        /// </summary>
        public AppConfig Config => _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ConfigService()
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GameCheatHelper"
            );

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            _configPath = Path.Combine(configDir, "config.json");
            _config = new AppConfig();
            Logger.Info($"配置服务初始化，配置路径: {_configPath}");
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        public bool LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);

                    if (config != null)
                    {
                        _config = config;
                        Logger.Info("配置加载成功");
                        return true;
                    }
                }

                Logger.Info("配置文件不存在，使用默认配置");
                _config = CreateDefaultConfig();
                SaveConfig(); // 保存默认配置
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载配置失败，使用默认配置");
                _config = CreateDefaultConfig();
                return false;
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public bool SaveConfig()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                Logger.Info("配置保存成功");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "保存配置失败");
                return false;
            }
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefault()
        {
            _config = CreateDefaultConfig();
            SaveConfig();
            Logger.Info("配置已重置为默认值");
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                Version = "1.0.0",
                Settings = new Settings
                {
                    InputDelay = 10,
                    StartWithWindows = false,
                    MinimizeToTray = true,
                    ShowNotifications = true,
                    Language = "zh-CN",
                    DetectionInterval = 2000
                }
            };
        }

        /// <summary>
        /// 导出配置到文件
        /// </summary>
        public bool ExportConfig(string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Logger.Info($"配置导出成功: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "导出配置失败");
                return false;
            }
        }

        /// <summary>
        /// 从文件导入配置
        /// </summary>
        public bool ImportConfig(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Error($"配置文件不存在: {filePath}");
                    return false;
                }

                var json = File.ReadAllText(filePath);
                var config = JsonConvert.DeserializeObject<AppConfig>(json);

                if (config != null)
                {
                    _config = config;
                    SaveConfig();
                    Logger.Info($"配置导入成功: {filePath}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "导入配置失败");
                return false;
            }
        }
    }
}
