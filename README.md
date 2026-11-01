# WeType Audio Guard · 微信语音输入保留音乐

在 Windows 上使用微信输入法语音输入时，后台音乐可能被压制：视频继续播放，浏览器却在音量合成器中被设成静音。这个小工具在录音开始后自动恢复原本有声的 Chrome / Edge 音频会话，让你继续边听音乐边说话。

A small Windows utility that restores Chrome / Edge audio sessions muted when WeType voice input starts. PowerShell + Windows Core Audio, with no third-party runtime packages.

**当前版本解决的是应用被设为静音的情形。**它不会提高被调低的音量，不会修改微信输入法内部行为，不能保证录音开始时完全无停顿。实机使用中仍有约一秒的主观停顿反馈，具体延迟来源尚未确定。

## 为什么做这个工具

已在一台 Windows 电脑上复现：

- 微信输入法 3.0.1.27 按 `Alt+V` 开始语音输入。
- `wetype_update` 开始占用麦克风，Chrome 音频会话的 `Mute` 变成 `true`，音量仍为 100%。
- Windows「通信 → 不执行任何操作」不能阻止这次应用静音。
- 手动取消浏览器静音后，音乐可以在录音期间继续播放。

工具把最后一步自动完成。这是对已观察现象的补救，并不表示所有版本、设备和用户都会遇到同样问题。本项目与腾讯或微信输入法官方无关联。

## 使用

要求：Windows、Windows PowerShell 5.1 或 PowerShell 7，以及可以写入的工具目录。不需要管理员权限，不需要安装 NirSoft、Python 或 .NET SDK。

1. 在 [Releases](https://github.com/epanodos/wetype-audio-guard/releases) 下载 ZIP 并解压，或通过 Git 克隆本仓库。
2. 双击 `Start.cmd`。保护程序会在后台运行。
3. 先播放音乐，再启动微信输入法语音输入。
4. 不再需要时，双击**同一目录**中的 `Stop.cmd`。

不要直接在 ZIP 中运行。启动后保留工具文件夹，不要在运行中移动或重命名它。没有设置开机自启，重启后需要再次启动。

如果系统提示脚本执行策略禁止运行，请先阅读源码、确认下载来源，并按自己或组织的 PowerShell 策略处理；启动器不会自动修改执行策略。

也可以在 PowerShell 中运行：

```powershell
.\Start.ps1                  # 后台启动
.\Start.ps1 -Foreground      # 前台运行，便于查看错误
.\Start.ps1 -MonitorOnly     # 只记录判断，不改变音频状态
.\Stop.ps1                   # 请求当前目录的实例自行退出
```

同一用户会话只允许运行一个实例。重复启动不会叠加多个后台进程。如果从其他目录启动过，请先用原目录中的停止入口结束它。

## 工作方式

1. 每轮读取 Windows Core Audio 会话，随后等待约 100 毫秒。完整周期还包含读取耗时。
2. 检测 `wetype_update`、`wetype_server` 或 `wetype_renderer` 的录音会话从空闲变为活动。
3. 查看录音前的播放状态，只选择当时正在播放、未静音且音量大于零的 Chrome / Edge 会话。
4. 在录音开始后的两秒内，若这些会话被静音，针对每个会话取消静音一次，并读回确认。

工具不录音、不获取转写文字、不联网、不修改声卡驱动、不结束微信或浏览器进程，也不改变音量大小。日志只保留运行事件、进程名、PID、音量状态和错误信息；不会写入音频或语音文字。日志在本机生成，不随仓库发布。

## 适用范围与限制

| 场景 | 当前行为 |
| --- | --- |
| 原本有声的 Chrome 被微信输入法静音 | 已实机验证可以恢复 |
| 原本手动静音的 Chrome | 已实机验证保持静音 |
| Edge | 已纳入匹配逻辑，尚未实机验证 |
| 音量从 100% 变成较低数值，但未静音 | 不处理 |
| 录音前没有播放、录音中才新建的音频会话 | 不处理 |
| 录音已经开始后才启动保护程序 | 等待下一次录音 |
| 微信电脑版内置语音输入、其他音乐客户端 | 不在当前匹配范围内 |
| 蓝牙耳机切换通话模式、播放器暂停 | 不处理 |

仍需注意：

- 这是“先静音，再恢复”，可能有短暂断音；100 毫秒不是端到端恢复时间的保证。
- 依据录音开始前约 300 毫秒的状态判断。若恰好在这一时段手动静音，可能被恢复。
- 只恢复一次；录音期间再次手动静音会保留。如果输入法反复强制静音，本工具不会持续争抢。
- 通过进程名与时间关联判断触发条件，无法证明是谁调用了静音接口；同时发生的其他程序静音可能被误判。
- 使用外放音乐时，语音识别质量可能受影响。
- 输入法更新、音频设备切换、异常设备驱动可能影响检测。连续十次读取/恢复错误后程序退出，请检查日志。

## 排查与验证

`guard.log` 记录启动、录音状态和恢复结果，超过约 1 MiB 时清空后继续记录。`error.log` 用于后台启动错误。`guard.pid` 和 `stop.request` 是本地运行文件，不能据 PID 文件单独判断程序是否还在运行。

如音乐仍持续无声，先检查音量合成器是否显示浏览器被静音，再看 `guard.log` 是否记录 `CAPTURE_START`、`UNMUTE`。提交问题时请提供系统版本、输入法版本、播放器、音频设备，以及脱敏后的相关日志；不要上传语音或完整私人诊断文件。

开发者可运行只读检查：

```powershell
powershell.exe -NoProfile -File .\tests\Smoke.ps1
# 或
pwsh -NoProfile -File .\tests\Smoke.ps1
```

检查包含脚本语法、C# 编译及真实音频会话枚举，不会更改静音状态。复现行为仍需在实际 Windows 音频环境中测试。

## 源码

- `CoreAudio.cs`：Windows Core Audio COM 接口、音频会话读取与静音控制。
- `Guard.ps1`：状态采样、录音前状态判断、一次性恢复与运行日志。
- `Start.ps1` / `Stop.ps1`：启动、停止。
- `Start.cmd` / `Stop.cmd`：Windows 双击入口。

基于微软公开的 [Audio Session Events](https://learn.microsoft.com/en-us/windows/win32/coreaudio/audio-session-events) 和 [ISimpleAudioVolume::SetMute](https://learn.microsoft.com/en-us/windows/win32/api/audioclient/nf-audioclient-isimpleaudiovolume-setmute) 接口。代码由本项目实现，不包含微信输入法二进制文件。

## 许可证

[MIT](LICENSE)。欢迎提交 Issue 和 Pull Request，尤其是无短暂断音的可靠实现、不同音频设备的复现结果，以及更多播放器的支持。
