using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nett;
using System.Collections.ObjectModel;
using System.Windows;

namespace FrpcManagerCSharp.ViewModels
{
    public partial class ConfigEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _serverAddr = "127.0.0.1";

        [ObservableProperty]
        private int _serverPort = 7000;

        [ObservableProperty]
        private ObservableCollection<ProxyConfig> _proxies = new();

        [ObservableProperty]
        private string _configSummary = "";

        [ObservableProperty]
        private bool _isModified;

        private readonly string _configPath;
        private readonly TomlConfigHandler _configHandler;
        private TomlTable? _originalConfig;

        public ConfigEditorViewModel(string configPath)
        {
            _configPath = configPath;
            _configHandler = new TomlConfigHandler(configPath);
            
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                _originalConfig = _configHandler.ReadConfig();
                
                // 加载服务器配置
                if (_originalConfig != null)
                {
                    ServerAddr = _originalConfig.TryGetValue("serverAddr", out var addrValue) ? addrValue?.ToString() ?? "127.0.0.1" : "127.0.0.1";
                    ServerPort = _originalConfig.TryGetValue("serverPort", out var portValue) && int.TryParse(portValue?.ToString(), out var port) ? port : 7000;
                }

                // 加载代理配置
                Proxies.Clear();
                if (_originalConfig != null && _originalConfig.TryGetValue("proxies", out var proxiesValue))
                {
                    try
                    {
                        if (proxiesValue is TomlTableArray proxyArray)
                        {
                            LoadProxiesFromNettTable(proxyArray);
                        }
                        else if (proxiesValue is TomlTable proxyTable)
                        {
                            // 兼容旧格式：直接处理表格式
                            // 手动创建代理配置对象，而不是尝试创建TomlTableArray
                            var proxy = new ProxyConfig();
                            
                            // 安全地获取值，如果不存在则使用默认值
                            proxy.Name = proxyTable.TryGetValue("name", out var nameValue) ? nameValue?.ToString() ?? "" : "";
                            proxy.Type = proxyTable.TryGetValue("type", out var typeValue) ? typeValue?.ToString() ?? "tcp" : "tcp";
                            proxy.LocalIP = proxyTable.TryGetValue("localIP", out var localIPValue) ? localIPValue?.ToString() ?? "127.0.0.1" : "127.0.0.1";
                            
                            if (proxyTable.TryGetValue("localPort", out var localPortValue) && int.TryParse(localPortValue?.ToString(), out var localPort))
                                proxy.LocalPort = localPort;
                            
                            if (proxyTable.TryGetValue("remotePort", out var remotePortValue) && int.TryParse(remotePortValue?.ToString(), out var remotePort))
                                proxy.RemotePort = remotePort;
                            
                            // 处理自定义域名
                            if (proxyTable.TryGetValue("customDomains", out var domainsValue) && domainsValue is TomlArray domainsArray)
                            {
                                var domains = domainsArray.Items
                                    .Where(item => item != null)
                                    .Select(item => item.ToString())
                                    .Where(domain => !string.IsNullOrWhiteSpace(domain));
                                
                                proxy.CustomDomains = string.Join(",", domains);
                            }
                            
                            Proxies.Add(proxy);
                        }
                        else if (proxiesValue is TomlTableArray tableArray && tableArray.Items.Any())
                        {
                            // 参考 TomlConfigHandler.cs 中的实现，使用 TomlTableArray 处理
                            foreach (var tableItem in tableArray.Items)
                            {
                                var proxy = new ProxyConfig();
                                
                                proxy.Name = tableItem.TryGetValue("name", out var nameValue) ? nameValue?.ToString() ?? "" : "";
                                proxy.Type = tableItem.TryGetValue("type", out var typeValue) ? typeValue?.ToString() ?? "tcp" : "tcp";
                                proxy.LocalIP = tableItem.TryGetValue("localIP", out var localIPValue) ? localIPValue?.ToString() ?? "127.0.0.1" : "127.0.0.1";
                                
                                if (tableItem.TryGetValue("localPort", out var localPortValue) && int.TryParse(localPortValue?.ToString(), out var localPort))
                                    proxy.LocalPort = localPort;
                                
                                if (tableItem.TryGetValue("remotePort", out var remotePortValue) && int.TryParse(remotePortValue?.ToString(), out var remotePort))
                                    proxy.RemotePort = remotePort;
                                
                                Proxies.Add(proxy);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 如果代理配置加载失败，创建默认代理
                        Console.WriteLine($"代理配置加载失败: {ex.Message}");
                        CreateDefaultProxy();
                    }
                }
                else
                {
                    CreateDefaultProxy();
                }

                UpdateConfigSummary();
                IsModified = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddProxy()
        {
            var newProxy = new ProxyConfig
            {
                Name = $"proxy_{Proxies.Count + 1}",
                Type = "tcp",
                LocalIP = "127.0.0.1",
                LocalPort = 8080,
                RemotePort = 8080 + Proxies.Count
            };
            Proxies.Add(newProxy);
            IsModified = true;
            UpdateConfigSummary();
        }

        [RelayCommand]
        private void RemoveProxy(ProxyConfig proxy)
        {
            if (proxy != null && Proxies.Contains(proxy))
            {
                Proxies.Remove(proxy);
                IsModified = true;
                UpdateConfigSummary();
            }
        }

        [RelayCommand]
        private void SaveConfig()
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(ServerAddr))
                {
                    MessageBox.Show("服务器地址不能为空", "验证错误", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ServerPort < 1 || ServerPort > 65535)
                {
                    MessageBox.Show("服务器端口必须在 1-65535 范围内", "验证错误", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证代理配置
                foreach (var proxy in Proxies)
                {
                    if (string.IsNullOrWhiteSpace(proxy.Name))
                    {
                        MessageBox.Show("代理名称不能为空", "验证错误", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (proxy.LocalPort < 1 || proxy.LocalPort > 65535 || 
                        proxy.RemotePort < 1 || proxy.RemotePort > 65535)
                    {
                        MessageBox.Show($"代理 '{proxy.Name}' 的端口必须在 1-65535 范围内", "验证错误", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // 构建新的配置
                var newConfig = Toml.Create();
                newConfig.Add("serverAddr", ServerAddr);
                newConfig.Add("serverPort", ServerPort);
                
                // 保留其他配置项
                if (_originalConfig != null)
                {
                    // 添加其他保留的配置项
                    foreach (var row in _originalConfig.Rows)
                    {
                        var key = row.Key;
                        if (!newConfig.Rows.Any(r => r.Key == key) && key != "proxies")
                        {
                            newConfig.Add(key, _originalConfig.Get(key));
                        }
                    }
                }

                // 添加代理配置 - 修复：使用正确的数组格式
                // 在 Nett 库中，使用 TomlObjectFactory.CreateEmptyAttachedTableArray 创建表数组
                var proxyArray = TomlObjectFactory.CreateEmptyAttachedTableArray(newConfig);
                
                foreach (var proxy in Proxies)
                {
                    var proxyConfig = Toml.Create();
                    proxyConfig.Add("name", proxy.Name);
                    proxyConfig.Add("type", proxy.Type);
                    proxyConfig.Add("localIP", proxy.LocalIP);
                    proxyConfig.Add("localPort", proxy.LocalPort);
                    proxyConfig.Add("remotePort", proxy.RemotePort);
                    
                    // 添加自定义域名
                    if (!string.IsNullOrEmpty(proxy.CustomDomains))
                    {
                        // 对于字符串数组，使用 CreateAttached 方法创建包含字符串的数组
                        var domains = proxy.CustomDomains
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(domain => domain.Trim())
                            .ToList();
                        var domainsArray = TomlObjectFactory.CreateAttached(newConfig, domains);
                        proxyConfig.Add("customDomains", domainsArray);
                    }
                    
                    // 将代理配置添加到表数组中
                    proxyArray.Add(proxyConfig);
                }
                
                // 将表数组添加到主配置中
                newConfig.Add("proxies", proxyArray);

                // 验证配置
                if (!_configHandler.ValidateConfig(newConfig, out string errorMessage))
                {
                    MessageBox.Show($"配置验证失败: {errorMessage}", "验证错误", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 保存配置
                _configHandler.SaveConfig(newConfig);
                _originalConfig = newConfig;
                IsModified = false;
                
                MessageBox.Show("配置保存成功", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateConfigSummary()
        {
            try
            {
                var summary = $"服务器: {ServerAddr}:{ServerPort}\n";
                summary += $"代理数量: {Proxies.Count}\n";
                
                foreach (var proxy in Proxies.Take(3))
                {
                    summary += $"  - {proxy.Name} ({proxy.Type}): {proxy.LocalIP}:{proxy.LocalPort} -> {proxy.RemotePort}\n";
                }
                
                if (Proxies.Count > 3)
                {
                    summary += $"  ... 还有 {Proxies.Count - 3} 个代理\n";
                }

                ConfigSummary = summary;
            }
            catch
            {
                ConfigSummary = "无法生成配置摘要";
            }
        }

        partial void OnServerAddrChanged(string value)
        {
            IsModified = true;
            UpdateConfigSummary();
        }

        partial void OnServerPortChanged(int value)
        {
            IsModified = true;
            UpdateConfigSummary();
        }

        // 创建默认代理配置
        private void CreateDefaultProxy()
        {
            var defaultProxy = new ProxyConfig
            {
                Name = "default_proxy",
                Type = "tcp",
                LocalIP = "127.0.0.1",
                LocalPort = 8080,
                RemotePort = 8080
            };
            Proxies.Add(defaultProxy);
        }

        // 从Nett的TomlTable加载代理配置 - 修复：适配数组格式
        private void LoadProxiesFromNettTable(TomlTableArray proxyArray)
        {
            Proxies.Clear();
            
            // 参考TomlConfigHandler.cs中的正确处理方法
            // 处理表数组格式的代理配置
            foreach (var proxyTable in proxyArray.Items)
            {
                var proxy = new ProxyConfig();
                
                proxy.Name = proxyTable.TryGetValue("name", out var nameValue) ? nameValue?.ToString() ?? "" : "";
                proxy.Type = proxyTable.TryGetValue("type", out var typeValue) ? typeValue?.ToString() ?? "tcp" : "tcp";
                proxy.LocalIP = proxyTable.TryGetValue("localIP", out var localIPValue) ? localIPValue?.ToString() ?? "127.0.0.1" : "127.0.0.1";
                
                if (proxyTable.TryGetValue("localPort", out var localPortValue) && int.TryParse(localPortValue?.ToString(), out var localPort))
                    proxy.LocalPort = localPort;
                
                if (proxyTable.TryGetValue("remotePort", out var remotePortValue) && int.TryParse(remotePortValue?.ToString(), out var remotePort))
                    proxy.RemotePort = remotePort;
                
                // 处理自定义域名
                if (proxyTable.TryGetValue("customDomains", out var domainsValue) && domainsValue is TomlArray domainsArray)
                {
                    var domains = domainsArray.Items
                        .Where(item => item != null)
                        .Select(item => item.ToString())
                        .Where(domain => !string.IsNullOrWhiteSpace(domain));
                    
                    proxy.CustomDomains = string.Join(",", domains);
                }
                
                Proxies.Add(proxy);
            }
        }
    }

    public partial class ProxyConfig : ObservableObject
    {
        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _type = "tcp";

        [ObservableProperty]
        private string _localIP = "127.0.0.1";

        [ObservableProperty]
        private int _localPort;

        [ObservableProperty]
        private int _remotePort;
        
        [ObservableProperty]
        private string _customDomains = "";
    }
}