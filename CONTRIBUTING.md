# 贡献指南

感谢你有兴趣参与 **RomaToHiragana** 的开发！无论你是来提问题、修 Bug、加功能，还是改进文档，都很欢迎。本指南帮助你高效、顺畅地参与贡献。

> 参与本项目即表示你同意遵守 [行为准则](CODE_OF_CONDUCT.md)。

---

## 目录

1. [开发环境](#开发环境)
2. [本地构建与运行](#本地构建与运行)
3. [代码规范](#代码规范)
4. [如何提交 Issue](#如何提交-issue)
5. [如何提交 Pull Request](#如何提交-pull-request)
6. [测试策略](#测试策略)
7. [许可](#许可)

---

## 开发环境

本项目基于 **.NET 10.0** 与 **Avalonia UI 12 / FluentAvalonia 3.0.2**。

- 安装 [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- 任意 IDE 即可，推荐 [JetBrains Rider](https://www.jetbrains.com/rider/) 或
  [Visual Studio 2022+](https://visualstudio.microsoft.com/)（含 .NET 桌面开发）
  （Avalonia 官方也提供了对应的 [VS/Rider 插件与模板](https://docs.avaloniaui.net/)）

## 本地构建与运行

```bash
# 还原依赖
dotnet restore

# 运行（调试）
dotnet run

# 编译（Debug）
dotnet build -c Debug

# 发布到当前平台（win-x64）
dotnet publish -c Release -r win-x64 --self-contained true
```

本项目未启用格式化强制工具（无 `.editorconfig`），请人工遵守下面的规范。

## 代码规范

- **分层与测试性**：UI 层只依赖接口（`IFileScanner`、`IFileRenamer`），不 `new` 具体实现、
  不感知转换算法细节。新增功能请保持这一风格，优先通过接口与构造函数注入。
- **职责单一**：转换器只管字符转换；扫描器只管列目录和拆分文件名；重命名器只管文件系统操作。
- **XML 文档注释**：公开类型与成员用 `///` 注释，注释使用中文。
- **命名**：遵循 C# 惯例（PascalCase 方法/属性，`_camelCase` 私有字段）。
- **状态文本**：面向 UI 的中文状态文本统一放在 `RenameStatusExtensions.ToDisplay()` 一类的
  扩展方法里，避免散落在各文件中。
- **不改已有行为**：不破坏扩展名保留、未识别字符保留等既有约定。

### 建议的开发流程

1. `Fork` 本仓库并克隆到本地。
2. 从 `master` 切出特性分支，命名建议：
   - `feature/xxx`（新功能）
   - `fix/xxx`（缺陷修复）
   - `docs/xxx`（文档）
3. 以「小步、可编译」为原则提交，提交信息建议采用
   [Conventional Commits](https://www.conventionalcommits.org/zh-hans/)：
   ```
   feat: 支持 XX
   fix: 修正 XX
   docs: 更新 README
   test: 添加 XX 测试
   chore: 更新依赖
   ```

## 如何提交 Issue

1. **先搜索**：是否已有相同或相似的 Issue / PR，避免重复。
2. **Bug 报告**请尽量包含：
   - 操作步骤（Step to reproduce）
   - 期望行为与实际行为
   - 截图或错误日志
   - 环境信息（OS、.NET 版本、构建方式）
3. **功能建议**请描述动机与预期效果。
4. 语言支持中文或英文均可。

模板参考：
```markdown
**描述**
简要说明问题或需求。

**复现步骤**
1. ...
2. ...

**期望行为**
...

**实际行为**
...

**环境**
- OS：
- .NET 版本：
- 其它：
```

## 如何提交 Pull Request

1. 确认你的分支基于最新的 `master`。
2. 只做与标题相关的最小改动，避免无关格式化。
3. 若改动对用户可见（新功能、修复、破坏性变更），请同步更新 [CHANGELOG.md](CHANGELOG.md)。
4. 运行 `dotnet build` 确保能编译，并尽量添加/更新测试（见 [测试策略](#测试策略)）。
5. 在 PR 描述中说明：
   - 改动目的
   - 改动要点
   - 是否有破坏性变更
   - 是否已本地验证
6. 通过 CI（`.github/workflows/build.yml`）后，维护者会评审与合并。

## 测试策略

当前项目尚无独立测试工程。若涉及转换算法或扫描/重命名逻辑，建议新增 `xUnit` 测试项目
（`RomaToHiragana.Tests`），针对 `RomajiToHiraganaConverter` 的各类规则（基本五十音、拗音、
促音、拨音、长音、未识别字符保留等）补充用例。

## 许可

本项目基于 **GNU GPL v3.0** 许可发布（见 [LICENSE.txt](LICENSE.txt)）。提交 PR 即表示你同意
你的贡献将以该许可一并发布。
