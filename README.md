# RomaToHiragana

**罗马字文件名 → 平假名 批量重命名工具**（Windows 桌面应用）

选中一个文件夹后，程序扫描其中所有文件，把**文件名主体**的罗马字（拉丁字母拼写的日语）转换为**平假名**，并在预览确认后一键批量重命名。文件的**扩展名始终原样保留**（如 `.txt`、`.jpg` 不变）。

窗口标题：*「罗马字文件名 → 平假名 批量重命名」*

> 📖 想参与贡献？请阅读 [贡献指南](CONTRIBUTING.md)；参与即表示同意遵守 [行为准则](CODE_OF_CONDUCT.md)。

---

## 功能特性

- **批量扫描与预览**：选择一个文件夹，列出全部文件，实时显示「原文件名 → 转换后文件名」。
- **安全重命名**：仅转换文件名主体；数字、标点、汉字、假名等非拉丁字母字符原样保留，避免误改。
- **勾选控制**：可全选 / 全不选 / 单独取消某些文件，只处理勾选且状态为「待转换」的文件。
- **自动跳过**：目标文件名已存在、源与目标相同等情况自动跳过，不覆盖已有文件。
- **状态统计**：显示总文件数、待转换、已跳过、失败数量。
- **操作日志**：记录每次转换 / 失败详情。
- **主题跟随系统**：亮/暗模式（`PreferSystemTheme`）与强调色（`PreferUserAccentColor`）跟随 Windows 系统设置。

---

## 技术栈

| 项目 | 说明 |
| --- | --- |
| 框架 | [Avalonia UI](https://avaloniaui.net/) 12.0（跨平台 .NET UI 框架） |
| 主题库 | [FluentAvaloniaUI](https://github.com/amwx/FluentAvalonia) 3.0.2 |
| 运行时 | .NET 10.0（`net10.0`），Windows 桌面（`WinExe`） |
| 语言 | C#（`LangVersion latest`，可空引用类型、隐式 using） |

---

## 目录结构

```
Roma2Hiragana/
├── .github/workflows/         # GitHub Actions
│   ├── build.yml              # CI：构建 + 发布产物
│   └── release.yml            # 推送 v* 标签时自动发 Release
├── RomaToHiragana.csproj      # 项目文件（Avalonia 12 + FluentAvalonia 3）
├── Program.cs                 # 应用入口，启动 Avalonia 生命周期
├── App.axaml                  # 应用级配置（FluentAvaloniaTheme：亮/暗模式 + 系统强调色）
├── App.axaml.cs               # 应用初始化，设置主窗口
├── MainWindow.axaml           # 主窗口 XAML 布局（文件列表、统计、日志、状态栏）
├── MainWindow.axaml.cs        # 主窗口交互逻辑（仅依赖服务接口，不感知转换细节）
├── app.manifest               # Windows 应用清单
├── CHANGELOG.md               # 变更记录（Keep a Changelog），发布说明来源
├── CONTRIBUTING.md            # 贡献指南
├── CODE_OF_CONDUCT.md         # 行为准则
├── LICENSE.txt                # GPL-3.0 许可证（官方全文）
└── Services/                  # 业务核心（与 UI 解耦，全部通过接口注入）
    ├── IRomajiConverter.cs            # 罗马字→平假名 转换器抽象
    ├── RomajiToHiraganaConverter.cs   # 转换核心算法
    ├── IFileScanner.cs                # 扫描器接口
    ├── FileScanner.cs                 # 扫描目录、拆分文件名主体/扩展名
    ├── IFileInfo.cs                   # 数据模型接口 + RenameStatus 状态枚举
    ├── FileItem.cs                    # 单个文件数据模型（INotifyPropertyChanged）
    ├── IFileRenamer.cs                # 重命名接口 + RenameResult 结果记录
    └── FileRenamer.cs                 # 执行 File.Move 完成实际重命名
```

> `bin/`、`obj/`、`.vs/` 为构建产物和 IDE 缓存，已被 `.gitignore` 忽略。

---

## 架构与数据流

UI 层与服务层通过接口解耦，`MainWindow` 只依赖 `IFileScanner` 和 `IFileRenamer`，由构造函数注入实现。

```
用户选择文件夹
   │
   ▼
MainWindow (UI 层)
   │  仅依赖 IFileScanner / IFileRenamer 接口
   ▼
FileScanner.ScanAsync()            扫描目录 → 拆分 文件名主体(stem) + 扩展名
   │
   ▼  对每个文件名主体调用
RomajiToHiraganaConverter         罗马字 → 平假名
   │
   ▼
FileItem（含原文件名 / 转换后文件名 / 状态）
   │
   ▼
FileRenamer.RenameAsync()          File.Move 实际重命名
```

---

## 转换规则

`RomajiToHiraganaConverter` 支持：

- **基本五十音**：清音、浊音、半浊音（`a`、`ka`、`shi`、`tsu`、`ga`、`pa`…）
- **拗音**：`kya`、`sho`、`nyu`、`gyo`…
- **长音**：元音重复（`aa/ii/uu/ee/oo`）与 `ou` 形式（`kou` → こう）
- **促音**：双写辅音（`kitto` → きっと、`gakkou` → がっこう），以及 `tch` 系列
- **拨音**：`n` 在词尾或辅音前 → ん；`n` + 元音 → な行；兼容旧式ヘボン式 `mb/mp` → ん
- **兼容训令式旧拼写**：`si/ti/tu/hu/zi/sya/tya...`
- **未识别字符原样保留**：数字、标点、汉字、假名等不受影响

示例：

| 原文件名 | 转换后 |
| --- | --- |
| `shinbun.txt` | `しんぶん.txt` |
| `gakkou.jpg` | `がっこう.jpg` |
| `kyoto_note.md` | `きょうと_note.md` |

---

## 构建与运行

前置要求：.NET SDK 10.0。

```bash
# 还原依赖
dotnet restore

# 运行（调试）
dotnet run

# 发布到当前平台（win-x64）
dotnet publish -c Release -r win-x64 --self-contained false
```

发布产物位于 `bin/Release/net10.0/win-x64/publish/`，可直接运行
`RomaToHiragana.exe`。

---

## 使用方式

1. 启动程序，点击「浏览...」选择包含目标文件的文件夹。
2. 点击「刷新文件列表」扫描并预览转换结果。
3. 通过「全选 / 全不选」或逐项勾选决定参与重命名的文件。
4. 点击「开始批量转换并重命名」，查看状态栏与操作日志的结果。

> 提示：仅转换文件名主体（不含扩展名）；纯英文单词序列会自动保留，避免误改。

---

## 持续集成（GitHub Actions）

项目内置 `.github/workflows/build.yml`，在每次 `push` / `pull_request` 时自动在 `windows-latest` 上执行：

1. 使用 `actions/checkout` 拉取代码。
2. 用 `actions/setup-dotnet` 安装 .NET 10.0 SDK（并启用依赖缓存）。
3. `dotnet restore` 还原依赖。
4. `dotnet build`（Debug）验证编译。
5. `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true` 发布。
6. 通过 `actions/upload-artifact` 上传 `RomaToHiragana-win-x64-release` 作为构建产物。

> 也支持在 Actions 页面点击 **Run workflow** 手动触发（`workflow_dispatch`）。

### 自动发布 Release

内置 `.github/workflows/release.yml`：当推送 `v*` 标签（如 `v1.0.0`）时，自动：

1. 构建并以 `win-x64` 发布（单文件、自包含）。
2. 从 [CHANGELOG.md](CHANGELOG.md) 抽取与当前版本对应的那一段作为 **Release Notes**。
3. 创建 GitHub Release（`softprops/action-gh-release`），上传发布产物。

因此发新版的流程是：更新 `CHANGELOG.md` → 打 `vX.Y.Z` 标签并推送 → 自动出 Release。

> 运行时若无对应版本的 changelog 条目，该步骤会报错并中止，避免生成内容错误的 Release。
> 请保持 `CHANGELOG.md` 中的版本段标题与标签一致（如 `## [1.0.0]` 对应标签 `v1.0.0`）。

---

## 参与贡献

欢迎以 **Issue** 和 **Pull Request** 的方式参与本项目。提交前请阅读 [贡献指南](CONTRIBUTING.md)，
并遵守 [行为准则](CODE_OF_CONDUCT.md)。提交 PR 即表示你同意你的贡献以项目的 GPL-3.0 许可一并发布。

---

## 开源协议

本项目采用 [GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.html) 许可发布，详见仓库根目录的 [LICENSE.txt](LICENSE.txt) 文件。

对本项目的分发、修改或再发布须遵守 GPL-3.0 及其条款：任何使用者都享有自由运行、研究、修改、再分发和发布的权利，且必须向接收者提供同等自由及对应源代码（copyleft）。
