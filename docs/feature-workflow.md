# 功能接入工作流

这是 AI coding agent 和人工开发者都必须遵循的 feature integration workflow。

## 1. 先发现，再设计

用业务语言描述需求并检索能力目录：

```powershell
./Scripts/find-capability.ps1 -Query "load character prefab"
./Scripts/find-capability.ps1 -Query "保存玩家设置"
```

阅读命中的 implementation 和至少一个真实 caller。现有模块已经覆盖责任时，扩展该 capability；不要创建平行 manager、service、singleton、resource loader、save wrapper、event bus、timer 或 pool。

语言表、翻译、字体或文本组件任务还必须阅读 [多语言与 RTL 规则](localization.md)，按其中的原文、占位符、字体覆盖和显示验收要求执行。

## 2. 选择最小接入方式

按以下顺序选择：

1. 在 game code 中组合现有 public API。
2. 向已有 framework capability 添加一个职责明确的窄操作。
3. 创建普通 domain class，并通过 constructor 注入依赖。
4. 只有出现全新 infrastructure responsibility 时才新增 framework module。

新增 module 前必须更新 `Config/framework-modules.json`，填写 responsibility、entry point 以及中英文 discovery keywords。如果职责和现有模块重叠，change description 必须解释为什么不能扩展原模块。

## 3. 选择当前推荐 API

- UI：新页面 await `OpenRoot<T>/OpenPage<T>/OpenChild<T>`；Child 指定 owner；本次打开资源在 `OnClose()` 清理，实例资源在 `OnDispose()` 清理。
- Assets：优先 `LoadAsync<T>()`；同步 `Load<T>()` 只用于允许主线程阻塞的 bootstrap。
- 关卡独占资产使用 `Assets.CreateScope()` 并明确 Dispose 时机；框架共享缓存继续由既有 owner 管理，不混用两种所有权。
- Scenes：新代码 await `Scenes.SwitchAsync(index)`，捕获失败并恢复加载界面；同目标请求自动合并。
- Events：使用 `EventId` / `EventId<T>` 和 `Subscribe()`，保存并释放 `IDisposable`。
- Timers：使用 `AfterSeconds/AfterFrames/EverySeconds/EveryFrames`，保存并释放 `TimerHandle`。
- Audio：保存 `AudioVoiceHandle`，按具体 voice 停止；不要用 clip name 表示播放实例。
- Pools：通过 `PoolMgr` spawn/despawn，不修改 `PoolToken`，不建立 feature-local pool。
- Save：通过 `Framework.Services.Save`；schema change 先设计 version 和 migration。
- Cross-feature communication：在 `FrameworkEvents` 定义 typed event，不写临时 string event name。

旧 API 的迁移关系统一见 `docs/runtime-api-migration.md`。

## 4. 保持边界和 ownership 可见

- Game code 通过 `Framework.Services` 访问基础设施，或只接收所需的显式依赖。
- `MonoBehaviour` 负责 Unity lifecycle/serialization adapter；domain rules 放在普通 C# class。
- 每个 asset、subscription、timer、audio voice 和 pooled object 都要能回答：谁创建、谁持有、在哪里清理。
- 不修改 Addressables 加载到的 prefab/ScriptableObject asset；需要运行时状态时创建 instance。
- 配置变更从 Excel/exporter source 开始，不编辑 generated output。
- 移动 Unity asset 时保留 `.meta` 和 GUID；删除 asset 时才删除对应 `.meta`。

## 5. 证明集成正确

按具体行为的影响选择最低有效验证，不要求每次模块或全量测试。局部小改动检查差异、必要的编译结果及直接相关用例；修复先验证复现用例，仅在公共行为、生命周期或依赖关系受影响时扩大范围。固定要求是核对相关框架文档，不能把文档同步要求转成全模块测试要求。

| 改动范围 | 验证方式 |
| --- | --- |
| 纯文档、注释 | 内容、链接、diff；不启动 Unity 或导表测试 |
| 局部代码小改动 | 代码差异、必要编译检查；有行为变化时只选直接相关用例 |
| 模块公共行为或依赖关系调整 | 按受影响调用路径扩大回归，必要时才跑整个模块 |
| 实际激活、销毁、场景行为 | 选择直接相关 PlayMode/生命周期用例，不默认追加全模块测试 |
| Excel schema、导表器、生成流程 | SheetTool 校验及测试；影响 C# 时补编译和使用方测试 |
| 验证脚本本身 | Scripts/test-verify.ps1 的参数、路由、报告判断测试 |
| 跨模块公共机制、合并主分支、发布 | 全量回归，发布时按目标补构建与运行验证 |

日常命令：

```powershell
./Scripts/verify.ps1                     # 仅架构检查；不是编译或测试通过
./Scripts/verify.ps1 -Module ui -UnityPath "G:/UnityEditor/2022.3.62f3/Editor/Unity.exe"
./Scripts/verify.ps1 -Module ui,ui-lifecycle -UnityPath "G:/UnityEditor/2022.3.62f3/Editor/Unity.exe"
./Scripts/verify.ps1 -Module pools,events -Plan  # 只预览，不启动测试
```

模块快捷名：ui、ui-lifecycle、pools、save、events、timers。ui 只选普通 UI 回归；ui-lifecycle 是进入 Play Mode 的生命周期用例。其他业务测试用自定义名称或正则：

```powershell
./Scripts/verify.ps1 -TestFilter "Game.Tests.CombatTests" -UnityPath "G:/UnityEditor/2022.3.62f3/Editor/Unity.exe"
# 真正位于 PlayMode 测试程序集的用例：
./Scripts/verify.ps1 -TestFilter "Game.Tests.SceneTests" -TestPlatform PlayMode -UnityPath "G:/UnityEditor/2022.3.62f3/Editor/Unity.exe"
```

Module/TestFilter 自动选择 Tests 范围，不要求 Node、不运行导表测试，也不自动猜测 Git diff 的依赖。测试筛选并不跳过 Unity 的必要编译/导入过程。完整验证和导表单独运行：

```powershell
./Scripts/verify.ps1 -Scope Full -UnityPath "G:/UnityEditor/2022.3.62f3/Editor/Unity.exe"
./Scripts/verify.ps1 -Scope SheetTool
./Scripts/test-verify.ps1
```

Node 不在 PATH 时，导表/全量命令加 `-NodePath` 或设置 `FEATHER_NODE_PATH`。`-SkipUnity` 保留为 `-Scope SheetTool` 的旧别名，不能和模块筛选混用。

Full 执行架构、SheetTool 和当前全部 EditMode 用例（含进入 Play Mode 的生命周期用例）；以后新增独立 PlayMode 程序集时，还需显式运行它们，不能把只跑 EditMode 描述为覆盖所有平台。

局部结果为 `TestResults/editmode-<模块>.xml`，自定义筛选用 custom；全量结果为 `TestResults/editmode.xml`，日志同名 .log。零个通过用例、授权失败、项目锁、编译失败或缺少结果文件均不算通过。若 Editor 正占用项目，优先在其中按相同范围筛选 Test Runner，不为小改动自动复制整个项目或关闭用户编辑器。

代码修改无法运行相关测试时，至少执行可用的编译检查，并明确测试缺口；架构检查不能代替编译。已通过的验证仅在后续改动、失败或未解决问题影响它时重跑。交付说明实际验证范围即可，不以用例总数代替覆盖说明。

## 6. 记录 public behavior

AI 修改 `Client/Assets/Scripts/FeatherFramework` 后，必须根据 diff 找出受影响的 capability，并核对相关文档。这是文档同步要求，与测试范围独立。只有文档事实发生变化才更新对应内容，不机械改写所有文档，也不为每次修改新增报告。按以下职责定位需要同步的内容：

- `docs/architecture.md`：当前架构事实与 ownership contract；
- `docs/runtime-api-migration.md`：需要旧代码适配时记录迁移方式；
- `README.md`：用户入口、示例和常用命令；
- `Config/framework-modules.json`：能力职责或 discovery keywords 变化。

如果只是内部实现修复，也要检查 `docs/architecture.md` 中对应模块的 ownership、cleanup、取消和失败行为描述是否仍然准确；无需为了没有变化的模块制造文档 diff。交付时简述相关文档已更新或核对后无需变更。`AGENTS.md` 只在协作规则变化时更新。
