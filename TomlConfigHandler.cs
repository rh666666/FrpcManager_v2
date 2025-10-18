using System;
using System.IO;
using System.Linq;
using System.Text;
using Nett;

namespace FrpcManagerCSharp
{
    public class TomlConfigHandler
    {
        private readonly string _configPath;

        public TomlConfigHandler(string configPath)
        {
            _configPath = configPath;
        }

        // 读取配置文件
        public TomlTable ReadConfig()
        {
            if (!File.Exists(_configPath))
            {
                // 如果文件不存在，创建默认配置
                var defaultConfig = CreateDefaultConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }

            try
            {
                return Toml.ReadFile(_configPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"读取配置文件失败: {ex.Message}", ex);
            }
        }

        // 保存配置文件
        public void SaveConfig(TomlTable config)
        {
            try
            {
                // 修复：正确的参数顺序
                Toml.WriteFile(config, _configPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存配置文件失败: {ex.Message}", ex);
            }
        }

        // 验证配置文件
        public bool ValidateConfig(TomlTable config, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                // 检查必需字段
                if (!config.Rows.Any(row => row.Key == "serverAddr"))
                {
                    errorMessage = "配置文件缺少必需字段: serverAddr";
                    return false;
                }

                if (!config.Rows.Any(row => row.Key == "serverPort"))
                {
                    errorMessage = "配置文件缺少必需字段: serverPort";
                    return false;
                }

                // 验证端口范围
                var port = config.Get<int>("serverPort");
                if (port < 1 || port > 65535)
                {
                    errorMessage = "serverPort 必须在 1-65535 范围内";
                    return false;
                }

                // 验证代理配置
                if (config.Rows.Any(row => row.Key == "proxies"))
                {
                    var proxies = config.Get<TomlTableArray>("proxies");
                    // 修复：获取正确的数量
                    int proxyCount = proxies.Count;
                    
                    for (int i = 0; i < proxyCount; i++)
                    {
                        var proxy = proxies[i];
                        var proxyKey = $"proxy_{i}";
                        
                        if (!proxy.Rows.Any(r => r.Key == "name"))
                        {
                            errorMessage = $"代理配置 {i} 缺少必需字段: name";
                            return false;
                        }

                        if (!proxy.Rows.Any(r => r.Key == "type"))
                        {
                            errorMessage = $"代理配置 {i} 缺少必需字段: type";
                            return false;
                        }

                        if (!proxy.Rows.Any(r => r.Key == "localIP"))
                        {
                            errorMessage = $"代理配置 {i} 缺少必需字段: localIP";
                            return false;
                        }

                        if (!proxy.Rows.Any(r => r.Key == "localPort"))
                        {
                            errorMessage = $"代理配置 {i} 缺少必需字段: localPort";
                            return false;
                        }

                        if (!proxy.Rows.Any(r => r.Key == "remotePort"))
                        {
                            errorMessage = $"代理配置 {i} 缺少必需字段: remotePort";
                            return false;
                        }

                        // 验证端口
                        var localPort = proxy.Get<int>("localPort");
                        if (localPort < 1 || localPort > 65535)
                        {
                            errorMessage = $"代理配置 {i} 的 localPort 必须在 1-65535 范围内";
                            return false;
                        }

                        var remotePort = proxy.Get<int>("remotePort");
                        if (remotePort < 1 || remotePort > 65535)
                        {
                            errorMessage = $"代理配置 {i} 的 remotePort 必须在 1-65535 范围内";
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"配置文件验证失败: {ex.Message}";
                return false;
            }
        }

        // 创建默认配置文件
        public static TomlTable CreateDefaultConfig()
        {
            // 修复：使用正确的创建方法
            var config = Toml.Create();
            config.Add("serverAddr", "127.0.0.1");
            config.Add("serverPort", 7000);
            
            // 修复：创建代理数组而不是表
            // 使用 TomlObjectFactory.CreateEmptyAttachedTableArray 创建表数组
            var proxyArray = TomlObjectFactory.CreateEmptyAttachedTableArray(config);
            
            var proxy = Toml.Create();
            proxy.Add("name", "ssh");
            proxy.Add("type", "tcp");
            proxy.Add("localIP", "127.0.0.1");
            proxy.Add("localPort", 22);
            proxy.Add("remotePort", 6000);
            
            // 修复：将代理添加到数组而不是直接添加到配置
            proxyArray.Add(proxy);
            config.Add("proxies", proxyArray);
            
            return config;
        }

        // 获取配置摘要
        public static string GetConfigSummary(TomlTable config)
        {
            if (config == null) return "无效配置";
            
            var summary = new StringBuilder();
            summary.AppendLine($"服务器: {config.Get<string>("serverAddr")}:{config.Get<int>("serverPort")}");
            
            // 获取代理数量
            if (config.Rows.Any(row => row.Key == "proxies"))
            {
                var proxies = config.Get<TomlTableArray>("proxies");
                int proxyCount = proxies.Items.Count();
                summary.AppendLine($"代理数量: {proxyCount}");
            }
            else
            {
                summary.AppendLine("代理数量: 0");
            }
            
            return summary.ToString();
        }
    }
}