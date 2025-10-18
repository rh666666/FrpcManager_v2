using System.IO;
using Nett;

namespace FrpcManagerCSharp
{
    public class ConfigManager
    {
        private readonly string _configDir;
        
        public ConfigManager()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            _configDir = Path.Combine(basePath, "config");
            
            // 确保config目录存在
            EnsureConfigDirectory();
        }
        
        // 确保config目录存在
        private void EnsureConfigDirectory()
        {
            if (!Directory.Exists(_configDir))
            {
                Directory.CreateDirectory(_configDir);
            }
        }
        
        // 获取所有配置文件
        public List<string> GetAllConfigFiles()
        {
            EnsureConfigDirectory();
            
            var files = Directory.GetFiles(_configDir, "*.toml")
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .Select(name => name!)
                .ToList();
                
            return files;
        }
        
        // 获取不带扩展名的配置文件名列表
        public List<string> GetConfigNames()
        {
            return GetAllConfigFiles()
                .Select(file => Path.GetFileNameWithoutExtension(file))
                .ToList();
        }
        
        // 获取配置文件完整路径
        public string GetConfigPath(string configName)
        {
            return Path.Combine(_configDir, $"{configName}.toml");
        }
        
        // 检查配置文件是否存在
        public bool ConfigExists(string configName)
        {
            return File.Exists(GetConfigPath(configName));
        }
        
        // 读取配置文件
        public TomlTable ReadConfig(string configName)
        {
            var configPath = GetConfigPath(configName);
            
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"配置文件不存在: {configPath}");
            }

            try
            {
                return Toml.ReadFile(configPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"读取配置文件失败: {ex.Message}", ex);
            }
        }
        
        // 保存配置文件
        public void SaveConfig(string configName, TomlTable config)
        {
            var configPath = GetConfigPath(configName);
            
            try
            {
                Toml.WriteFile(config, configPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存配置文件失败: {ex.Message}", ex);
            }
        }
        
        // 创建新配置文件
        public void CreateConfig(string configName)
        {
            if (ConfigExists(configName))
            {
                throw new InvalidOperationException($"配置文件已存在: {configName}");
            }
            
            var defaultConfig = TomlConfigHandler.CreateDefaultConfig();
            SaveConfig(configName, defaultConfig);
        }
        
        // 删除配置文件
        public void DeleteConfig(string configName)
        {
            var configPath = GetConfigPath(configName);
            
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"配置文件不存在: {configPath}");
            }
            
            try
            {
                File.Delete(configPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"删除配置文件失败: {ex.Message}", ex);
            }
        }
        
        // 重命名配置文件
        public void RenameConfig(string oldName, string newName)
        {
            var oldPath = GetConfigPath(oldName);
            var newPath = GetConfigPath(newName);
            
            if (!File.Exists(oldPath))
            {
                throw new FileNotFoundException($"源配置文件不存在: {oldPath}");
            }
            
            if (File.Exists(newPath))
            {
                throw new InvalidOperationException($"目标配置文件已存在: {newPath}");
            }
            
            try
            {
                File.Move(oldPath, newPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"重命名配置文件失败: {ex.Message}", ex);
            }
        }
        
        // 验证配置文件
        public bool ValidateConfig(string configName, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            try
            {
                var config = ReadConfig(configName);
                var handler = new TomlConfigHandler(GetConfigPath(configName));
                return handler.ValidateConfig(config, out errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
        
        // 获取配置摘要
        public string GetConfigSummary(string configName)
        {
            try
            {
                var config = ReadConfig(configName);
                return TomlConfigHandler.GetConfigSummary(config);
            }
            catch (Exception ex)
            {
                return $"加载配置失败: {ex.Message}";
            }
        }
    }
}