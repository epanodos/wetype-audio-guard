# WeType Audio Guard · 微信语音输入保留音乐

微信输入法语音输入时，后台音乐会被静音。WeType Audio Guard 自动恢复浏览器的声音，让你边听音乐边说话。

适用于 Windows，支持 Chrome 和 Edge。

## 使用方法

1. 在 [Releases](https://github.com/epanodos/wetype-audio-guard/releases/latest) 下载 ZIP 并解压。
2. 双击 `Start.cmd`，工具会在后台运行。
3. 播放音乐，正常使用微信输入法语音输入。
4. 双击同一文件夹中的 `Stop.cmd` 停止运行。

重启电脑后，再次双击 `Start.cmd` 即可。移动工具文件夹前，先停止运行。

运行环境：Windows PowerShell 5.1 或 PowerShell 7。无需管理员权限，无需额外安装依赖。

## 功能

- 在微信输入法开始语音输入时，自动取消浏览器被触发的静音。
- 保留原来的音量大小。
- 保留录音前已手动设置的静音状态。
- 在本地后台运行，重复启动只保留一个实例。

## 命令行

```powershell
.\Start.ps1                  # 后台启动
.\Start.ps1 -Foreground      # 前台运行
.\Start.ps1 -MonitorOnly     # 只记录状态
.\Stop.ps1                   # 停止运行
```

运行记录保存在 `guard.log`，启动错误保存在 `error.log`。

## 开发

工具使用 PowerShell 和 Windows Core Audio 接口，检测微信输入法的录音状态，恢复录音前正在播放的浏览器音频。

| 文件 | 用途 |
| --- | --- |
| `CoreAudio.cs` | 读取音频会话、控制静音 |
| `Guard.ps1` | 检测录音状态、恢复声音 |
| `Start.ps1` / `Stop.ps1` | 启动和停止 |
| `Start.cmd` / `Stop.cmd` | 双击运行入口 |
| `tests/Smoke.ps1` | 脚本语法、编译和音频接口检查 |

运行检查：

```powershell
powershell.exe -NoProfile -File .\tests\Smoke.ps1
```

欢迎通过 [Issues](https://github.com/epanodos/wetype-audio-guard/issues) 反馈问题，或提交 Pull Request。

## 许可证

[MIT](LICENSE)
