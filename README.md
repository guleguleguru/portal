# Desktop Portal / 桌面传送门

Desktop Portal 是一个轻量级 Windows 本地桌面工具，用全局快捷键快速切换或打开自定义目标。

支持目标：

- 网页 URL
- 本地文件，例如 xlsx、docx、pdf、pptx
- 文件夹
- exe 程序

支持快捷键：

- F1-F12
- 数字键
- Ctrl / Alt / Shift 组合键
- 鼠标中键、鼠标侧键

## 运行方式

开发运行：

```powershell
dotnet run --project .\DesktopPortal\DesktopPortal.csproj
```

发布打包：

```powershell
.\scripts\publish-release.ps1
```

发布产物默认生成到：

- `artifacts\DesktopPortal-0.1.0-beta-win-x64\DesktopPortal.exe`
- `artifacts\DesktopPortal-0.1.0-beta-win-x64.zip`

## 本地数据

配置文件：

- `%AppData%\DesktopPortal\config.json`
- 保存配置前会在 `%AppData%\DesktopPortal\backups` 生成 `config.backup.*.json`，默认保留 5 份。

日志文件：

- `%AppData%\DesktopPortal\logs\app.log`

日志超过 5MB 会自动轮转，默认保留 3 个归档文件。

主界面的“健康检查”会检查启用规则的快捷键格式、快捷键重复和目标路径/URL 是否有效；未处理异常会写入本地日志，便于排查问题。

## 托盘和退出

关闭主窗口时，程序会最小化到系统托盘继续运行。

托盘右键菜单可以：

- 打开设置
- 暂停或恢复全部快捷键
- 重载配置
- 退出程序

## 隐私

Desktop Portal 不联网、不做账号系统、不采集用户数据、不上传配置。所有规则、配置和日志都只保存在本机。

更多说明见 [隐私说明](docs/privacy.md)。

## 已知限制

- 普通浏览器 URL 只能尽量激活匹配标题或域名的浏览器窗口，不能精确切换浏览器内部标签页。
- 如果目标程序以管理员权限运行，而 Desktop Portal 不是管理员权限，Windows 可能拒绝前台激活。
- 未签名版本可能触发 Windows SmartScreen 提示。

## 当前版本

`0.1.0-beta`
