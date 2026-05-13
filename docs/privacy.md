# Desktop Portal 隐私说明

Desktop Portal 是本地 Windows 工具。

## 数据处理

- 不联网。
- 不采集用户数据。
- 不上传快捷键、路径、网页地址或日志。
- 不使用账号系统。
- 不依赖云服务。

## 本地文件

配置文件保存在：

- `%AppData%\DesktopPortal\config.json`

日志文件保存在：

- `%AppData%\DesktopPortal\logs\app.log`

日志用于排查本地运行问题，可能包含规则名称、目标路径、快捷键和异常信息。日志只保存在本机。

## 开机自启

如果用户启用开机自启，程序会写入当前用户注册表：

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

这个操作不需要管理员权限。
