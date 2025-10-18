using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FrpcManagerCSharp
{
    public class FrpProcessManager(string frpcPath, string configPath, Action<string> logCallback, Action<string> statusCallback)
    {
        private readonly string _frpcPath = frpcPath;
        private readonly string _configPath = configPath;
        private readonly Action<string> _logCallback = logCallback;
        private readonly Action<string> _statusCallback = statusCallback;
        private Process? _frpcProcess;

        public bool IsRunning => _frpcProcess != null && !_frpcProcess.HasExited;

        // 启动 frpc 进程
        public void Start()
        {
            if (IsRunning)
            {
                _logCallback("frpc 已在运行中");
                return;
            }

            // 检查文件是否存在
            if (!System.IO.File.Exists(_frpcPath))
            {
                _logCallback($"错误: frpc.exe 未找到在路径: {_frpcPath}");
                _statusCallback("启动失败");
                return;
            }

            if (!System.IO.File.Exists(_configPath))
            {
                _logCallback($"错误: 配置文件未找到在路径: {_configPath}");
                _statusCallback("启动失败");
                return;
            }

            try
            {
                _frpcProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _frpcPath,
                        Arguments = $"-c \"{_configPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(_frpcPath)
                    }
                };

                // 订阅输出事件
                _frpcProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logCallback(e.Data ?? string.Empty);
                };
                
                _frpcProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logCallback($"错误：{e.Data ?? string.Empty}");
                };

                _frpcProcess.Start();
                _frpcProcess.BeginOutputReadLine();
                _frpcProcess.BeginErrorReadLine();

                _logCallback("frpc 启动成功");
                _statusCallback("运行中");

                // 监听进程退出
                _frpcProcess.Exited += (s, e) =>
                {
                    if (_frpcProcess != null)
                    {
                        _logCallback($"frpc 已退出（退出码：{_frpcProcess.ExitCode}）");
                        _statusCallback("已停止");
                        _frpcProcess.Dispose();
                        _frpcProcess = null;
                    }
                };
                _frpcProcess.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                _logCallback($"启动失败：{ex.Message}");
                _statusCallback("启动失败");
                _frpcProcess?.Dispose();
                _frpcProcess = null;
            }
        }

        // 停止 frpc 进程
        public void Stop()
        {
            if (!IsRunning)
            {
                _logCallback("frpc 未在运行");
                return;
            }

            try
            {
                _logCallback("正在停止 frpc...");
                if (_frpcProcess != null)
                {
                    _frpcProcess.Kill();
                    
                    // 等待进程退出，最多等待5秒
                    if (_frpcProcess.WaitForExit(5000))
                    {
                        _logCallback("frpc 已停止");
                        _statusCallback("已停止");
                    }
                    else
                    {
                        _logCallback("警告: 停止 frpc 超时");
                        _statusCallback("已停止");
                    }
                    
                    _frpcProcess.Dispose();
                    _frpcProcess = null;
                }
            }
            catch (Exception ex)
            {
                _logCallback($"停止失败：{ex.Message}");
            }
        }

        // 重启 frpc 进程
        public void Restart()
        {
            _logCallback("正在重启 frpc...");
            
            if (IsRunning)
            {
                Stop();
            }
            
            // 等待进程完全停止
            Task.Delay(1500).ContinueWith(_ => Start());
        }
    }
}