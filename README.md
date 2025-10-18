# FrpcManager_v2

一个基于WPF的Frp客户端管理工具，提供图形界面来管理frpc配置和进程。

## 功能特性

- 📋 多配置文件管理 - 创建、编辑、重命名和删除不同的frpc配置
- 🚀 一键启动/停止/重启frpc进程
- 📝 实时日志显示和自动滚动
- 🎨 简洁直观的用户界面
- 🔧 配置文件验证功能，确保配置正确

## 技术架构

- **开发语言**: C#
- **框架**: WPF (Windows Presentation Foundation)
- **架构模式**: MVVM (Model-View-ViewModel)
- **依赖库**:
  - CommunityToolkit.Mvvm - MVVM工具包
  - Nett - TOML配置文件处理

## 使用说明

1. **准备工作**
   - 确保在`frp`目录下放置`frpc.exe`文件

2. **启动程序**
   - 运行`FrpcManager_v2.exe`
   - 首次启动会自动创建`config`目录

3. **配置管理**
   - 通过界面创建新的配置文件
   - 编辑现有配置文件
   - 从下拉列表中选择要使用的配置

4. **启动frpc**
   - 选择配置文件后，点击启动按钮
   - 日志窗口将显示frpc的输出信息

## 配置文件说明

配置文件以TOML格式存储在`config`目录下，每个配置文件对应一个frpc配置。配置文件必须包含以下必需字段：

- `serverAddr`: frps服务器地址
- `serverPort`: frps服务器端口

代理配置需包含以下字段：
- `name`: 代理名称
- `type`: 代理类型（如tcp、udp、http等）

## 开发说明

### 调试模式

在调试模式下，程序会自动分配控制台窗口用于额外的调试输出。

### 构建项目

使用Visual Studio打开解决方案文件或使用命令行工具，选择发布配置进行构建。

```powershell
dotnet build
```

## 注意事项

- 确保frpc.exe版本与配置文件格式兼容
- 配置文件中的端口号必须在1-65535范围内
- 程序关闭时会自动停止运行中的frpc进程
- 使用ICO格式图标以确保最佳兼容性
- 调试信息仅在Debug模式下输出

## 许可证

MIT License