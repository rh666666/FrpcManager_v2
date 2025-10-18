using System.Runtime.InteropServices;
using System.Windows;
using FrpcManagerCSharp.Converters;

namespace FrpcManagerCSharp
{
    public partial class App : Application
    {
        /// <summary>
        /// Allocates a new console for current process.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();

        /// <summary>
        /// Frees the console.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
#if DEBUG
            // 仅在Debug模式下分配控制台用于调试输出
            AllocConsole();
            Console.WriteLine("程序启动，控制台已分配（Debug模式）");
#endif
            
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
        
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            
#if DEBUG
            // 仅在Debug模式下释放控制台
            FreeConsole();
#endif
        }
    }
}

