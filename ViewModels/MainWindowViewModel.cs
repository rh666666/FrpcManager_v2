using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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

        private readonly FrpProcessManager _frpManager;
        private readonly string _frpcPath;
        private readonly string _configPath;

        public MainWindowViewModel()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            _frpcPath = Path.Combine(basePath, "frp", "frpc.exe");
            _configPath = Path.Combine(basePath, "frp", "frpc.toml");
            
            _frpManager = new FrpProcessManager(_frpcPath, _configPath, AddLog, UpdateStatus);
            
            // 加载初始配置摘要
            try
            {
                var configHandler = new TomlConfigHandler(_configPath);
                var config = configHandler.ReadConfig();
                ConfigSummary = TomlConfigHandler.GetConfigSummary(config);
            }
            catch (Exception ex)
            {
                ConfigSummary = $"加载配置失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Start()
        {
            if (!IsRunning)
            {
                _frpManager.Start();
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
            if (IsRunning)
            {
                _frpManager.Stop();
                await Task.Delay(1000);
            }
            _frpManager.Start();
        }

        [RelayCommand]
        private void EditConfig()
        {
            try
            {
                // 使用绝对路径
                var configEditor = new ConfigEditorWindow(_configPath);
                configEditor.ShowDialog();
                
                // 重新加载配置摘要
                var configHandler = new TomlConfigHandler(_configPath);
                var config = configHandler.ReadConfig();
                ConfigSummary = TomlConfigHandler.GetConfigSummary(config);
                
                AddLog("配置编辑完成");
            }
            catch (Exception ex)
            {
                AddLog($"打开配置编辑器失败: {ex.Message}");
                MessageBox.Show($"打开配置编辑器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddLog(string message)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
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