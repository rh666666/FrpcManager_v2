using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace FrpcManagerCSharp.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _status = "未运行";

        [ObservableProperty]
        private string _configSummary = "";

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private ObservableCollection<string> _logs = new();

        [ObservableProperty]
        private ObservableCollection<string> _configNames = new();

        [ObservableProperty]
        private string _selectedConfig = "";

        private readonly FrpProcessManager _frpManager;
        private readonly string _frpcPath;
        private readonly ConfigManager _configManager;

        public MainWindowViewModel()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            _frpcPath = Path.Combine(basePath, "frp", "frpc.exe");
            _configManager = new ConfigManager();
            
            // 加载配置文件列表
            LoadConfigNames();
            
            // 如果有配置文件，选择第一个
            if (ConfigNames.Count > 0)
            {
                SelectedConfig = ConfigNames[0];
                LoadConfigSummary();
            }
            else
            {
                ConfigSummary = "无可用配置文件";
            }
            
            // 初始化进程管理器（稍后设置实际配置路径）
            _frpManager = new FrpProcessManager(_frpcPath, "", AddLog, UpdateStatus);
            
            // 添加测试日志，验证显示功能
            AddLog("程序启动成功");
            AddLog($"当前目录: {basePath}");
            AddLog($"frpc路径: {_frpcPath}");
            AddLog($"配置文件数量: {ConfigNames.Count}");
        }

        private void LoadConfigNames()
        {
            ConfigNames.Clear();
            var names = _configManager.GetConfigNames();
            foreach (var name in names)
            {
                ConfigNames.Add(name);
            }
        }

        private void LoadConfigSummary()
        {
            if (string.IsNullOrEmpty(SelectedConfig))
            {
                ConfigSummary = "请选择配置文件";
                return;
            }

            try
            {
                ConfigSummary = _configManager.GetConfigSummary(SelectedConfig);
            }
            catch (Exception ex)
            {
                ConfigSummary = $"加载配置失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Start()
        {
            if (string.IsNullOrEmpty(SelectedConfig))
            {
                MessageBox.Show("请先选择要启动的配置文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!IsRunning)
            {
                var configPath = _configManager.GetConfigPath(SelectedConfig);
                
                // 验证配置文件
                if (!_configManager.ValidateConfig(SelectedConfig, out var errorMessage))
                {
                    MessageBox.Show($"配置文件验证失败: {errorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 更新进程管理器的配置路径
                _frpManager.UpdateConfigPath(configPath);
                _frpManager.Start();
                
                // 记录启动命令
                AddLog($"启动命令: frp/frpc.exe -c {configPath}");
            }
        }

        [RelayCommand]
        private void Stop()
        {
            if (IsRunning)
            {
                _frpManager.Stop();
            }
        }

        [RelayCommand]
        private async Task Restart()
        {
            if (string.IsNullOrEmpty(SelectedConfig))
            {
                MessageBox.Show("请先选择要重启的配置文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (IsRunning)
            {
                _frpManager.Stop();
                await Task.Delay(1000);
            }
            
            var configPath = _configManager.GetConfigPath(SelectedConfig);
            
            // 验证配置文件
            if (!_configManager.ValidateConfig(SelectedConfig, out var errorMessage))
            {
                MessageBox.Show($"配置文件验证失败: {errorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // 更新进程管理器的配置路径
            _frpManager.UpdateConfigPath(configPath);
            _frpManager.Start();
        }

        [RelayCommand]
        private void EditConfig()
        {
            if (string.IsNullOrEmpty(SelectedConfig))
            {
                MessageBox.Show("请先选择要编辑的配置文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var configPath = _configManager.GetConfigPath(SelectedConfig);
                var configEditor = new ConfigEditorWindow(configPath);
                configEditor.ShowDialog();
                
                // 重新加载配置摘要
                LoadConfigSummary();
                
                AddLog($"配置 '{SelectedConfig}' 编辑完成");
            }
            catch (Exception ex)
            {
                AddLog($"打开配置编辑器失败: {ex.Message}");
                MessageBox.Show($"打开配置编辑器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void CreateConfig()
        {
            var configName = Microsoft.VisualBasic.Interaction.InputBox("请输入新配置文件名（不带.toml后缀）:", "创建配置文件", "");
            
            if (string.IsNullOrWhiteSpace(configName))
            {
                return;
            }

            try
            {
                _configManager.CreateConfig(configName);
                LoadConfigNames();
                SelectedConfig = configName;
                LoadConfigSummary();
                
                AddLog($"配置文件 '{configName}' 创建成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void DeleteConfig()
        {
            if (string.IsNullOrEmpty(SelectedConfig))
            {
                MessageBox.Show("请先选择要删除的配置文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定要删除配置文件 '{SelectedConfig}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _configManager.DeleteConfig(SelectedConfig);
                    var oldConfig = SelectedConfig;
                    LoadConfigNames();
                    
                    // 选择新的配置文件
                    if (ConfigNames.Count > 0)
                    {
                        SelectedConfig = ConfigNames[0];
                        LoadConfigSummary();
                    }
                    else
                    {
                        SelectedConfig = "";
                        ConfigSummary = "无可用配置文件";
                    }
                    
                    AddLog($"配置文件 '{oldConfig}' 删除成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void RenameConfig()
        {
            if (string.IsNullOrEmpty(SelectedConfig))
            {
                MessageBox.Show("请先选择要重命名的配置文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var newName = Microsoft.VisualBasic.Interaction.InputBox("请输入新的配置文件名（不带.toml后缀）:", "重命名配置文件", SelectedConfig);
            
            if (string.IsNullOrWhiteSpace(newName) || newName == SelectedConfig)
            {
                return;
            }

            try
            {
                _configManager.RenameConfig(SelectedConfig, newName);
                LoadConfigNames();
                SelectedConfig = newName;
                LoadConfigSummary();
                
                AddLog($"配置文件重命名成功: '{SelectedConfig}' -> '{newName}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 当选择的配置文件改变时
        partial void OnSelectedConfigChanged(string value)
        {
            LoadConfigSummary();
        }

        private void AddLog(string message)
        {
            string logMessage;
            
            // 检查消息是否已经包含时间戳格式（形如 2025-10-18 17:23:21.867）
            if (System.Text.RegularExpressions.Regex.IsMatch(message, @"\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3}"))
            {
                // 对于已经包含详细时间戳的frpc日志，只添加方括号
                logMessage = message;
            }
            else
            {
                // 对于自定义消息，添加时间戳
                logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            }
            
#if DEBUG
            // 仅在Debug模式下输出到控制台进行调试
            Console.WriteLine(logMessage);
#endif
            
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                Logs.Add(logMessage);
#if DEBUG
                Console.WriteLine($"日志已添加到集合，当前集合数量: {Logs.Count}");
#endif
            });
        }

        private void UpdateStatus(string status)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                Status = status;
                IsRunning = status == "运行中";
            });
        }
    }
}