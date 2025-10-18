using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;
using FrpcManagerCSharp.ViewModels;

namespace FrpcManagerCSharp
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private bool _isAtBottom = true;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            this.DataContext = _viewModel;
            
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
            
            // 订阅ScrollViewer的滚动事件
            LogScrollViewer.ScrollChanged += LogScrollViewer_ScrollChanged;
            
            // 直接订阅_viewModel.Logs的CollectionChanged事件
            if (_viewModel.Logs is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += (sender, e) => Logs_CollectionChanged(sender ?? notifyCollection, e);
            }
            
            // 窗口关闭时的资源清理
            this.Closing += (sender, e) => MainWindow_Closing(sender ?? this, e);
        }

        private void LogScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 判断是否滚动到底部
            _isAtBottom = (e.ExtentHeight - e.ViewportHeight <= e.VerticalOffset + 1.0);
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // 当集合变化且当前在底部时，滚动到底部
            if (_isAtBottom && e.Action == NotifyCollectionChangedAction.Add)
            {
                // 使用Dispatcher延迟执行，确保UI已更新
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // 确保ScrollViewer和ItemsControl已完全渲染
                    LogScrollViewer.UpdateLayout();
                    LogScrollViewer.ScrollToBottom();
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 异步调用Dispose方法确保frpc进程正确关闭
            Task.Run(() => _viewModel.Dispose());
        }
    }
}