using System.Windows;
using FrpcManagerCSharp.ViewModels;

namespace FrpcManagerCSharp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}