using System.Windows;
using FrpcManagerCSharp.Converters;

namespace FrpcManagerCSharp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 注册值转换器资源
            Resources.Add("InverseBooleanConverter", new InverseBooleanConverter());
            Resources.Add("StatusColorConverter", new StatusColorConverter());
            Resources.Add("ObservableCollectionToStringConverter", new ObservableCollectionToStringConverter());
            Resources.Add("BooleanToModifiedTextConverter", new BooleanToStringConverter
            {
                TrueValue = "已修改",
                FalseValue = "未修改"
            });
        }
    }
}

