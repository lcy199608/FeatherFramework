# 快速开始一个游戏项目

目标：从当前 FeatherFramework 开始，跑通“主菜单 → 进入游戏场景 → 保存并读取一次数据”。本文的 Unity 路径以项目目录 `Client` 为起点；终端命令默认在仓库根目录执行。

## 直接运行现成示例

打开 `Assets/Scenes/Starter/Menu.unity` 并 Play，点击 Start / Continue → Collect coin & save → Return to menu，再次进入应恢复金币数；退出 Play 后重新运行也能读取。场景已加入 Build Settings，原 Main 场景保持不变。

示例代码在 `Assets/Scripts/Demo/Starter`：StarterEntry 组合已有服务并持有关卡 AssetScope，两个 Panel 使用 Root 导航和异步场景切换。资源在 `Assets/Res/UI/Starter*Panel.prefab` 与 `Assets/Res/Starter/Level.txt`。地址沿用已登记的 Res 文件夹规则；如项目改为逐个登记资源，需要登记这三项资源的同名地址。

示例只向当前存档槽位写入 `FeatherStarter.Progress`，不覆盖其他业务字段。示例数据含 Version：0 按旧 Coins 字段兼容读取，1 为当前结构，更高版本拒绝覆盖。正式游戏应为自己的数据定义迁移，不能把示例版本直接当作整个游戏的版本。

资源缺失时可执行 `FeatherFramework/Examples/Create Starter Example`。工具创建缺失项，对已有示例只更新场景路径引用，并将 Default 层的 UI 节点设为 UI 层；保留其他编辑内容。示例以完整场景路径切换，Build Settings 调序不影响它。正式项目可移除示例场景、对应脚本、资源及 meta；示例测试位于 Demo/StarterTests 独立程序集，不再影响框架测试编译。

## 1. 保留完整的项目结构

新项目沿用下面的相对位置：

```text
MyGame/
  Client/                 Unity 项目，在 Unity Hub 中打开这里
  Config/SheetTool/       Excel 与导表工具
  Config/framework-modules.json
  Scripts/               初始化、能力检索、验证
  docs/
  AGENTS.md
  README.md
```

可以从已提交的框架版本创建新仓库，也可以复制当前工作区。当前这轮修复尚未自动提交；只克隆旧提交不会带走未提交的修改。复制时保留资源和 `.meta`，不必复制 `Library`、`Temp`、`Logs`、`obj`、`node_modules` 等缓存。新仓库独立管理版本历史。

不要只复制 `Client`：Unity 的 Excel 菜单按相对路径寻找外层 `Config/SheetTool`。

## 2. 安装依赖并完成第一次导表

- Unity：项目版本为 **2022.3.62f3**，安装目标平台对应的 Build Support。
- Node.js：**18 或更高**，确保 `node` 和 `npm` 在 PATH 中。
- 使用 Unity Hub 打开 `MyGame/Client`，等待包解析与脚本导入结束。

在 `MyGame` 根目录执行：

```powershell
./Scripts/bootstrap.ps1
npm --prefix ./Config/SheetTool run sync -- --format json
./Scripts/verify.ps1 -Scope SheetTool
```

`bootstrap.ps1` 执行 `npm ci`。首次建议使用 JSON，便于检查输出内容。

Unity 内也可以使用 `FeatherFramework/Config/Sync Excel Config`。菜单读取 `Assets/Data/ConfigImportSettings.asset` 中的格式；命令行通过 `--format json` 或 `--format bin` 显式选择。两种入口使用时应选择相同格式。

## 3. 跑通现有启动流程

1. 在 Player Settings 中确定新项目的 Company Name 和 Product Name。当前存档加密使用 Product Name 参与派生，因此应尽早确定，已有对外存档后改名需要兼容处理。
2. 检查 `Assets/Res/GameConfig.asset`，开发时开启日志，设置默认语言与帧率。
3. 保留 `Assets/Res/UI/UICanvas.prefab`，其根组件必须是 Canvas，并包含 `BottomLayer`、`MiddleLayer`、`TopLayer`、`SystemLayer`。
4. 打开 `Window/Asset Management/Addressables/Groups`。当前 `Assets/Res` 文件夹已登记为地址 `Res`；检查资源地址能解析到 `Res/GameConfig.asset` 与 `Res/UI/UICanvas.prefab`。若单独登记文件，不要将地址留成不匹配的 `Assets/...`。
5. 编辑器首次调试可选 `Use Asset Database (fastest)` Play Mode Script。
6. 打开 `Assets/Scenes/Main.unity` 并 Play，确认 Console 没有初始化异常。也可打开 `Assets/Scenes/Demo/RedDot.unity` 查看已有红点示例。

`FrameworkHost` 会在第一个场景加载前自动创建并跨场景保留，不需要手工添加 manager 或 Canvas 实例。当前 Main 场景没有完整主菜单，只跑通初始化时不会自动出现玩法界面。

不使用本地化或红点时，在 GameConfig 关闭 enableLocalization / enableRedDots 后重新运行。对应服务和表数据不会加载；保留 Excel schema、生成类型及枚举，不要手删生成文件。

## 4. 创建两个场景

继续使用 `Main.unity` 作为菜单场景，另建 `Assets/Scenes/Game.unity`，放置一个能明确看出已进入游戏的对象或文字。

打开 `File/Build Settings`，按顺序加入并勾选：

| Build index | 场景 | 用途 |
| --- | --- | --- |
| 0 | Main | 主菜单 |
| 1 | Game | 玩法场景 |

仓库已有 Starter 示例场景。按本节手工建立正式项目时，将 Main/Game 放到前两项；后文索引 1 指向 Game，不能直接沿用示例场景的索引。示例自身使用稳定路径，不受该调整影响。

## 5. 制作第一个菜单 Panel

在临时 Canvas 下制作一个铺满屏幕的 UI 根对象，命名为 `MainMenuPanel`，根对象使用 RectTransform。在它下面添加一个 Unity UI Button，命名为 `BtnStart`。

将 Panel 保存为 `Assets/Res/UI/MainMenuPanel.prefab`。临时设计用 Canvas 不需要保存到运行场景，否则会与框架 Canvas 重复。

在 **Project 窗口选中这个 prefab 资源**，运行 `FeatherFramework/生成或刷新UI脚本`。生成器以选中对象为根，场景中应选中具体 Panel 而非整个 Canvas；兄弟节点名称必须唯一，节点名称不能包含斜线。

工具会生成：

```text
Assets/Scripts/Game/UI/MainMenuPanel.cs
Assets/Scripts/Game/UI/MainMenuPanel.Bindings.g.cs
```

等待编译后，将 `MainMenuPanel` 脚本挂到 prefab 根节点并保存。工具生成代码，不自动挂载组件。

将业务文件 `MainMenuPanel.cs` 改为以下内容，保留生成的 `.Bindings.g.cs`：

```csharp
using UnityEngine;

public partial class MainMenuPanel : PanelBase
{
    public override UIType Type => UIType.Root;

    public override void OnInit()
    {
        BindGeneratedReferences();
        _BtnStart.onClick.AddListener(EnterGame);
    }

    public override void OnShow() { }
    public override void OnHide() { }

    public override void OnDispose()
    {
        if (_BtnStart != null)
            _BtnStart.onClick.RemoveListener(EnterGame);
    }

    private void EnterGame()
    {
        var services = Framework.Services;
        services.Save.LoadData(0);
        int visits = services.Save.GetData("GameVisits", 0) + 1;
        services.Save.SetData("GameVisits", visits, true);
        Debug.Log($"进入游戏次数：{visits}");

        services.UI.Close(Handle);
        services.Scenes.LoadSceneAsync(1, null);
    }
}
```

`_BtnStart` 来自生成文件，要求子对象名称确实为 `BtnStart`。生成器目前支持 `Btn`、`Img`、`Txt`、`Tran` 前缀，分别绑定 Button、Image、传统 Unity UI Text、Transform。TMP 文本不在当前自动绑定范围内，可自行使用序列化引用。

Panel 的初始化放在 `OnInit()`，不要依赖 `Awake()` 先执行。关闭页面使用 UI service，避免自行 `Destroy` 或 `SetActive` 导致导航状态不一致。

## 6. 从场景入口打开菜单

创建 `Assets/Scripts/Game/Bootstrap/GameBootstrap.cs`：

```csharp
using UnityEngine;

public sealed class GameBootstrap : FrameworkBehaviour
{
    private async void Start()
    {
        if (!Framework.IsReady)
        {
            Debug.LogError("框架初始化失败，请检查此前的 Console 错误。");
            return;
        }

        try
        {
            await Services.UI.OpenRoot<MainMenuPanel>();
        }
        catch (System.OperationCanceledException)
        {
            // 退出流程或切换 Root 时，旧打开请求会取消。
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
```

在 Main 场景新建空物体 `GameBootstrap`，挂载该脚本并保存场景。Game 场景不挂此菜单入口。

从 Main Play：应看到菜单；点击按钮后，Console 输出进入次数并切换 Game 场景。停止后再次从 Main Play，次数应递增。这就验证了 UI 加载、按钮绑定、存档重读和场景切换。

示例的 `LoadData(0)` 表示打开或创建槽位 0，**不会清空旧进度**。以后实现“新游戏”时再明确决定是否删除某个槽位；“继续游戏”直接加载即可。示例数据使用该项目的实际存档，正式制作存档结构时应移除示例键或更换测试槽位。

## 7. 开始编写游戏功能

UI 面板通过 Type 声明角色：主界面为 Root，背包等独立页面为 Page，依附背包的物品详情为 Child。生成模板默认 Page。常用调用如下（需要先制作并挂载对应 Panel prefab）：

```csharp
var ui = Framework.Services.UI;
var bag = await ui.OpenPage<BagPanel>();
var detail = await ui.OpenChild<ItemDetailPanel>(bag, itemData);
ui.Close(detail); // 只关闭这个详情实例
ui.Close(bag);    // 同时关闭它的全部 Child，包括仍在加载的请求
```

在 BagPanel 内可以直接调用 `await OpenChild<ItemDetailPanel>(itemData)`。打开另一 Page 会暂时隐藏背包和详情，`ui.Back()` 返回时恢复；已主动关闭的 Child 不再恢复。Page 关闭后实例可缓存，本次打开建立的订阅、timer 在 `OnClose()` 清理，OnInit 建立的实例资源在 OnDispose 清理。

业务代码集中到 `Assets/Scripts/Game`，按功能分目录：

```text
Game/
  Bootstrap/
  UI/
  Player/
  Battle/
  Inventory/
```

先实现游戏规则的普通 C# 类；MonoBehaviour 负责输入、场景引用与 Unity 生命周期；Panel 负责显示和交互。需要框架能力时，通过 `Framework.Services`，或在创建普通 C# 对象时传入所需依赖。

| 需求 | 入口 |
| --- | --- |
| 显示/隐藏 UI | `Framework.Services.UI` |
| 加载资源 | `Framework.Services.Assets` |
| 保存进度 | `Framework.Services.Save` |
| 读取配置 | `Framework.Services.Config.Tables` |
| 订阅消息 | `Framework.Services.Events.Subscribe(...)` |
| 延迟/循环任务 | `Framework.Services.Timers` |
| 场景切换 | `Framework.Services.Scenes` |
| 播放音频 | `Framework.Services.Audio` |
| 对象复用 | `Framework.Services.Pools` |

使用事件、Timer、音频时保存返回的订阅或 handle，在 owner 的清理点释放或停止。资源加载不是引用计数 lease，共享资源要明确释放责任。

配置表放在 `Config/SheetTool/Excels`，日常修改后执行：

```powershell
npm --prefix ./Config/SheetTool run sync -- --format json
```

业务读取示例：`Framework.Services.Config.Tables.Item.TryGet(itemId, out var item)`。不要手改 `Assets/Gen` 或 `Assets/Resources/Config`。

新增功能前先搜索：

```powershell
./Scripts/find-capability.ps1 -Query "保存玩家设置"
```

给 AI 的任务可以具体到：“在 Game/Inventory 中实现背包领域逻辑，组合已有 Config、Save、Events 和 UI，补测试；先检索能力目录，不增加全局 manager。”已有能力足够时，业务工作不需要修改框架源码。

## 8. 验证与第一次打包

需要检查 UI 公共行为时，可在仓库根目录运行 UI 回归：

```powershell
./Scripts/verify.ps1 -Module ui -UnityPath "G:/UnityEditor/2022.3.62f3/Editor/Unity.exe"
```

局部小改动优先选择直接相关的 -TestFilter，用例范围按行为影响判断，不因改动 UI 文件就运行整个模块。替换为本机 Unity 路径。若 Editor 正占用项目，在它的 Test Runner 中筛选对应测试；无需为了小修改关闭编辑器或复制整个项目。涉及实际激活、销毁时选择 `-Module ui,ui-lifecycle`。只有导表改动才额外运行 `-Scope SheetTool`。

发布/首次打包前运行 `./Scripts/verify.ps1 -Scope Full -UnityPath "G:/UnityEditor/2022.3.62f3/Editor/Unity.exe"`，执行架构、导表及当前全部 EditMode 测试（含进入 Play Mode 的生命周期测试）。脚本不会替你打包或验证完整玩法。无参数默认仅做架构检查；纯文档/注释修改不启动 Unity。完整选择规则见 `feature-workflow.md`。

首次打包前，在 Addressables Groups 执行 `Build > New Build > Default Build Script`，再从 Build Settings 构建目标平台。编辑器的 `Use Asset Database` 模式不能代替实际资源构建。运行构建产物，检查菜单、切场景和退出后读档。

## 常见卡点

| 现象 | 优先检查 |
| --- | --- |
| 框架未就绪 | GameConfig/Canvas 地址、生成配置是否存在、此前初始化异常 |
| 找不到 UI prefab | 地址是否为 `Res/UI/MainMenuPanel.prefab`，类名和 prefab 名是否一致 |
| UI 显示失败 | prefab 根节点是否有 PanelBase 子类与 RectTransform |
| 找不到 `_BtnStart` | 是否生成绑定文件、按钮命名和 Button 组件是否正确 |
| 错误生成了 Canvas 脚本 | 是否直接选中了目标 Panel 根节点，而不是 Canvas |
| 场景索引无效 | Build Settings 是否启用了对应场景；使用数字索引的自定义入口还需核对索引 |
| 导表工具找不到目录 | 是否完整保留仓库结构，而不只是 Client |
| 编辑器正常，打包缺资源 | Addressables 内容是否为当前目标平台构建 |

更详细的生命周期与边界见 `architecture.md`、`feature-workflow.md` 和 `runtime-api-migration.md`。


## Windows 验证包

编辑器批处理入口 StarterBuild.BuildWindows 构建 Addressables 和 Windows Development Player，输出到 TestResults/Windows。验证包临时使用 FeatherFrameworkVerification 产品名隔离存档，构建结束恢复项目产品名与脚本后端。VerificationTone.wav 是验证音频，通过 Audios/VerificationTone 地址播放。

运行验证包加 -feather-smoke-write 自动验证启动、场景往返、保存和音频播放/停止；随后加 -feather-smoke-read 验证进程重启读档。该自动流程只在 Development Build 或 Editor 中编译，且普通启动不会执行。设置 FEATHER_SMOKE_SCREENSHOT 可输出 UI 相机渲染图；其他目标平台仍需各自构建验收。
