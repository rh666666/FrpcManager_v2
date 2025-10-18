using System;
using System.Windows;
using FrpcManagerCSharp.ViewModels;

namespace FrpcManagerCSharp
{
    public partial class ConfigEditorWindow : Window
    {
        public ConfigEditorWindow(string configPath)
        {
            InitializeComponent();
            DataContext = new ConfigEditorViewModel(configPath);
            
            // 设置窗口图标
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app-icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath, UriKind.Absolute));
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                // 如果图标设置失败，记录错误但不影响程序运行
                Console.WriteLine($"设置图标失败: {ex.Message}");
#endif
            }
        }
    }
}