# FeatherFramework

一个面向 AI 主导、人工协作开发的精简 Unity 中小型项目框架。运行时只有一个组合入口 `Framework.Services`，并通过机器可读能力目录和架构检查避免重复造轮子、绕过框架与低复用实现。

## 功能特点

- **UI 框架**: 支持 Root/Page/Child 导航、Child 归属与异步取消，管理层级、缓存及 `OnInit/OnShow/OnHide/OnClose/OnDispose` 生命周期，支持预制体生成脚本。
- **音频管理**: 通过 `AudioVoiceHandle` 管理具体播放实例，区分 BGM/effect channel，并支持延迟与 Fade。
- **配置表读取**: 基于本地 Excel 的跨平台导表工具，生成 Unity 可直接加载的配置代码与 JSON 数据。
- **数据持久化**: 提供 slot、dirty flush、损坏字段 fallback 和旧存档兼容边界。
- **红点系统**: 配置驱动的层级红点，数量向上聚合并支持 disposable subscription。
- **多语言模块**: 支持文本、Sprite、GameObject variant 和一致的 fallback。
- **跨平台日志Log**: 使用了Log Viewer插件。
- **常用插件集成**: 包括DOTween、Odin、EasySave等。
- **能力发现与防重复实现**: 新功能开发前可检索已有模块，验证脚本会阻止游戏代码绕过统一资源、存档、场景与服务边界。

## 统一入口

场景组件可继承 `FrameworkBehaviour`，通过一个明确入口使用框架能力：

```csharp
public sealed class ShopEntry : FrameworkBehaviour
{
    public System.Threading.Tasks.Task<PanelHandle> Open() => Services.UI.OpenPage<ShopPanel>();
}
```

普通 C# 业务类优先使用构造函数接收所需依赖，不依赖全局状态。新增功能前先执行能力检索，确认应当复用或扩展哪个模块。

Panel 用 `Type => UIType.Root / Page / Child` 声明角色。通过 `await UI.OpenRoot<T>()`、`await UI.OpenPage<T>(data)`、`await UI.OpenChild<T>(owner, data)` 打开；返回的 handle 用于 `UI.Close(handle)`。`UI.Back()` 先关闭当前页面的 Child，再返回上一 Page。

一次性初始化放在 `OnInit()`；本次打开建立的订阅和 timer 在 `OnClose()` 清理，实例资源在 `OnDispose()` 清理。Root/Page 关闭后缓存，Child 随 owner 关闭并销毁。旧调用方式继续兼容，规则见 [Runtime API 迁移说明](docs/runtime-api-migration.md)。

## 开发与验证

- 快速开始项目：[`docs/quick-start.md`](docs/quick-start.md)
- 可运行示例：打开 `Client/Assets/Scenes/Starter/Menu.unity`，体验菜单、场景切换、保存与恢复；代码位于 `Client/Assets/Scripts/Demo/Starter`。
- 架构说明：[`docs/architecture.md`](docs/architecture.md)
- 功能接入流程：[`docs/feature-workflow.md`](docs/feature-workflow.md)
- Runtime API 迁移说明：[`docs/runtime-api-migration.md`](docs/runtime-api-migration.md)
- AI/自动化开发约束：[`AGENTS.md`](AGENTS.md)
- 框架代码变更必须核对相关文档，事实或用法变化时及时同步；未变化则无需改写，也不新增逐次修改报告。文档同步不要求运行整个模块测试。
- 查找已有能力：`./Scripts/find-capability.ps1 -Query "保存玩家设置"`
- 仅检查架构：`./Scripts/check-architecture.ps1`
- 架构检查规则修改后：`./Scripts/test-architecture.ps1`（隔离样例，不启动 Unity）
- 安装导表工具依赖：`./Scripts/bootstrap.ps1`
- 日常轻量检查：`./Scripts/verify.ps1`（仅架构，不启动 Unity/导表测试）
- UI 模块测试：`./Scripts/verify.ps1 -Module ui -UnityPath <Unity.exe>`
- UI 及实际生命周期：`./Scripts/verify.ps1 -Module ui,ui-lifecycle -UnityPath <Unity.exe>`
- 自定义测试类/方法：`./Scripts/verify.ps1 -TestFilter "Game.Tests.CombatTests" -UnityPath <Unity.exe>`
- 全量回归（跨模块公共机制修改、合并主分支/发布前）：`./Scripts/verify.ps1 -Scope Full -UnityPath <Unity.exe>`
- 导表工具验证：`./Scripts/verify.ps1 -Scope SheetTool`（旧 `-SkipUnity` 仍支持）
- 预览执行范围：在命令后加 `-Plan`，不启动测试
- Node 不在 `PATH`：增加 `-NodePath <node.exe>` 或设置 `FEATHER_NODE_PATH`

纯文档/注释修改不启动 Unity。局部小改动检查差异、必要编译结果和直接相关用例，可用 `-TestFilter` 精确到方法；模块筛选是批量验证选项，不是每次修改的必跑步骤。公共行为、生命周期或依赖关系变化时才扩大回归范围。模块快捷名包含 ui、ui-lifecycle、pools、save、events、timers。测试脚本自身修改运行 `./Scripts/test-verify.ps1`，它使用模拟进程检查筛选和结果判定，不运行 Unity。
