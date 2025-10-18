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
        }
    }
}