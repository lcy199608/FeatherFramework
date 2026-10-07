# FeatherFramework 架构说明

## 设计目标

FeatherFramework 是面向 Unity 中小型项目的精简 runtime。框架只有一个 composition root（组合根）、一个可发现的 service surface（服务入口），并通过 asmdef 建立明确边界。游戏功能保持为普通 C# 代码；`MonoBehaviour` 主要承担 Unity lifecycle 和 serialized reference 的适配。

该设计同时服务于 AI 和人工开发：

- 唯一公共入口：`Framework.Services`；
- 机器可读能力目录：`Config/framework-modules.json`；
- 自动检查业务代码是否绕过统一基础设施；
- 模块依赖和 owner/cleanup point 可见，不依赖分散的全局 singleton；
- generated code、vendor code、framework runtime 和 game code 分离。

框架以“最小基础设施”为边界：只提供项目已经需要的资源、UI、存档、配置、事件、Timer、场景、Pool、音频及可选本地化/红点能力。没有实际 caller、性能问题或生命周期问题时，不新增平行 Manager、通用容器或更重的第三方框架。

## Runtime composition

`FrameworkHost` 在首个 scene 前自动创建，是唯一 composition root。初始化成功后才发布不可变的 `FrameworkServices`。

```text
FrameworkHost
  -> Addressables + GameConfig
  -> Events, Save, Config
  -> Timers, Scenes, Assets, Pools
  -> Localization, RedDots
  -> Audio, UI
  -> Framework.Services
```

`FrameworkHost.State` 包含 `NotStarted`、`Initializing`、`Ready` 和 `Failed`。失败时可读取 `InitializationError` 并调用 `RetryInitialize()`。不要在 static constructor 中访问 `Framework.Services`。Scene component 可继承 `FrameworkBehaviour`；普通 domain class 应通过 constructor 只接收实际需要的依赖。

红点树与必需 Canvas 校验完成后才发布服务。Canvas 在 inactive staging parent 下实例化；缺少 prefab、Canvas 组件或 layer 会使初始化失败。Host 设置 Ready 并发布服务后才激活 Canvas，因此其 `Awake/OnEnable` 不会观察到部分初始化的服务。

Host 销毁与初始化失败共用关闭顺序：UI → Audio → Localization/RedDots → Save → Timers → Pools → Scenes → host coroutines → Events → Assets，最后清空服务入口。Panel 清理期间仍可访问已发布的服务；单个清理步骤异常会记录日志并继续后续步骤。音频和 UI 的关闭操作幂等，避免失败后重试时旧组件延迟销毁影响新服务。

所有 service 的构造函数和 composition-root 初始化方法只对 `Feather.Runtime` 开放。业务程序集只能通过 `Framework.Services` 使用能力，不能自行 `new ResMgr`、`new TimerMgr` 或创建第二套资源、存档、事件等服务。

## 能力发现

`Config/framework-modules.json` 是能力清单的 source of truth。设计功能前先搜索：

```powershell
./Scripts/find-capability.ps1 -Query "load a prefab"
./Scripts/find-capability.ps1 -Query "保存玩家设置"
```

命中后阅读 implementation 和至少一个 caller。`Scripts/check-architecture.ps1` 会检查业务代码直接使用 Addressables、Resources、Easy Save、SceneManager 或另建 singleton 等越界行为。

该脚本是基于文本规则的轻量检查，忽略注释和字符串并报告行号；只扫描 gameCodeRoots 内的业务代码，排除 Editor 和测试程序集，不替代编译或职责审查，也不对插值表达式等进行完整语义分析。业务类可以使用 Manager/Mgr 后缀；是否重复建设基础设施应依据能力目录和实际职责判断，不能仅凭类名拒绝。

## Ownership 与生命周期

关闭与清空是不同操作。Host 关闭后，Assets、Timers、Pools、Events、Scenes、Save 的新增工作或写入被拒绝；订阅 Dispose、handle 清理和重复 Shutdown 安全。已取出的池对象仍由 caller 持有，关闭后归还会销毁该对象。Localization/RedDots 同样不再接收服务调用。Audio 的播放入口返回无效 handle 并报告未初始化，UI 返回取消 Task。

### Assets

- Addressables 是第一方 runtime asset 的唯一来源；业务代码使用 `Framework.Services.Assets`。
- `ResMgr` 会按 address + type 合并并发请求、缓存成功结果并隔离 callback exception。
- `LoadAsync<T>()` 是默认选择；`Load<T>()` 会 `WaitForCompletion()`，仅用于 bootstrap 或明确允许阻塞的路径。
- 业务代码使用带类型的 `ReleaseRes<T>()`；全量释放和无类型兼容入口只由 framework host 使用。
- 当前缓存不是 reference-counted lease。一个 address 应有明确 owner，释放前必须确认没有其他 consumer 继续使用。
- 关卡独占资源可使用 `Assets.CreateScope()`，通过 scope.LoadAsync 加载，在所有实例和使用者结束后 Dispose，统一释放成功缓存并取消待完成回调。作用域按 address + type 独占，不允许与普通缓存或其他 scope 混用同一资源；需要共享时通过同一 owner 分发结果。Host 关闭也会释放尚存的 scope。
- UI prefab、音频等框架共享资源仍使用普通缓存，UI 关闭、音频播放结束和 Pool.Clear 不自动释放其资源缓存。常驻资源由 host 最终释放；不要把仍被框架缓存实例使用的资源放进关卡 scope。

### Scenes

- 新代码 await `Scenes.SwitchAsync(buildIndex)` 或 `SwitchAsync("Assets/Scenes/Game.unity")`，加载失败通过 Task 异常反馈。目标必须在 Build Settings 中启用。
- 同目标的并发异步请求返回同一 Task；其他目标请求失败，不启动第二次加载。同步 LoadScene 也不能插入正在进行的异步切换。
- 旧 `LoadSceneAsync(index, onComplete)` 继续可用，可选第三个 onError 接收失败；未提供时记录异常。重复调用的各个成功回调仍各执行一次。
- Host Update 发布当前加载进度，完成时发布最后一次进度。Host 关闭取消等待结果并解绑完成回调，但 Unity 已开始的底层场景加载不能撤销；不承诺 rollback。

### UI

- `Framework.Services.UI` 负责 panel 的加载、层级、激活、隐藏、缓存和销毁；Panel 不自行改变 active state。
- 新功能使用 `OpenRoot<T>(data = null)`、`OpenPage<T>(data = null)`、`OpenChild<T>(owner, data = null)`，返回可 await 的 `Task<PanelHandle>`。Panel 内的 `OpenChild<T>(data)` 自动使用自身 Handle。旧 ShowUI/ShowUIAsync 继续兼容。
- 泛型 UI 入口检测同一短名称对应不同组件类型的冲突，并在导航前报错；现有按短名称寻址规则保持兼容。owner 更换绘制层级时，已打开 Child 会重新计算 Layer；显式覆盖 Layer 的 Child 保留自己的规则。
- `PanelBase.Type` 声明 Root/Page/Child；`Layer` 独立声明绘制层级，默认 Root 在 Bottom、Page 在 Middle、Child 继承 owner 实际使用的层级（含旧 ShowUI 的显式 UILayer 参数）并排在它上面。输入阻挡仍由 prefab 的 UI/raycast 设置负责，不自动创建模态遮罩。
- 同时只有一个当前 Root。打开 Page 保留 Root，暂时隐藏上一 Page 及其 Child；关闭栈顶 Page 恢复上一 Page 和仍打开的 Child。`Back()` 优先关闭当前 Page（没有 Page 时为 Root）最后打开且可见的 Child，再关闭栈顶 Page。
- Child 必须属于当前 service 中已打开的具体 Root/Page handle，不允许使用已关闭的旧 handle、其他 service 的 handle 或 Child 作为 owner。同类 Child 可有多个独立实例，精确操作使用各自 handle；归属不依赖 Transform 父子关系。
- `Close(handle)` 先使 handle 失效，再关闭 Child，最后清理 owner；关闭 Root 会关闭该流程中的 Page。owner 暂时隐藏时 Child 隐藏，期间加载完成的 Child 也等待 owner 恢复；主动关闭的 Child 不会随返回重新出现。
- `OnClose()` 每次打开结束一次，清理本次打开建立的订阅、timer 等。Root/Page 关闭后缓存实例，再打开获得新的 handle；Child 关闭即销毁。`HideUI/HideAllUI` 是兼容关闭入口；按名关闭会关闭同名的全部 Child，精确关闭使用 handle。
- 同一 Root/Page 重复打开复用实例及有效 handle，不增加重复栈记录，更新 data 并执行 OnShow。新 Page 请求按发起顺序提交；加载或初始化失败保留当前页面，Task 以异常结束。
- 切换 Root 取消旧流程中的新 API 请求。owner 在 Child 加载前登记，owner 关闭、请求被替代和 service 关闭均立即使相关 Task 取消；晚到回调不会复活 UI。先 await Root 成功，再为新流程打开 Page。取消是 UI 层失效控制，不擅自释放共享 ResMgr 缓存。
- `PanelBase.OnInit()` 每个 instance 只调用一次，`OnShow()` / `OnHide()` 可重复调用，`OnDispose()` 是最终 cleanup point。
- UI 相机由 UIMgr 与 Canvas 分别持有、在关闭时清理；相机不挂在其渲染的 Screen Space Camera Canvas 下，避免 Canvas 尺寸变化反向改变相机 Transform。
- 新 Panel 在 inactive staging parent 下实例化并执行 `OnInit()`，随后设置层级与 transform、激活并执行 `OnShow()`。`OnInit()` 不应依赖 `Awake()` 已执行；需要共享的初始化放在 `OnInit()`。
- 打开流程在同一 UIMgr 内分为实例准备、类型与 owner 校验、导航提交。恢复每个 Child 前重新检查 owner 可见性，避免 OnShow 导航后继续显示旧页面的 Child。
- `HideAllUI()` 使当前待完成请求失效；Root 显示时使更早的打开请求失效，晚完成的旧 Root 不能覆盖新 Root。同步打开、缓存命中和异步完成共用请求编号与生命周期路径。
- 面板先失活再调用 `OnHide()`，防止清理重入重复隐藏。显式移除、UI 关闭和 host 关闭都会通过幂等入口执行 `OnDispose()`；`OnHide/OnDispose` 异常不会阻断其他面板清理。批量隐藏/移除期间不接受生命周期回调重新打开 UI。
- 本次打开建立的资源在 `OnClose()` 释放；`OnInit()` 建立、跨关闭缓存的实例资源在 `OnDispose()` 释放。暂时被下一 Page 遮挡不执行 OnClose。清理回调内的打开请求被拒绝；关闭期间的 Shutdown 等当前关闭清理完成后再销毁实例。
- 旧 IsRoot 映射为 Root；旧 IsStackable=true 保持无 owner 的兼容叠加行为，不自动映射为 Child。新面板只声明 Type，旧面板可逐步迁移。

### Events

- 新代码使用 `EventId` / `EventId<T>`，框架级定义集中在 `FrameworkEvents`。
- `Subscribe()` 返回 `IDisposable`；owner 必须在 disable、destroy 或 dispose 时释放。
- 每次 Subscribe 拥有独立注册身份，Clear 前的旧订阅不能移除 Clear 后使用同一委托的新订阅。
- 单个 listener 抛异常只记录日志，不会中断其他 listener。
- 业务代码只能使用 `EventId` / `EventId<T>`；字符串事件 API 已收回到 runtime 内部，不再扩大旧 caller。

### Timers

- 新代码使用 `AfterSeconds()`、`AfterFrames()`、`EverySeconds()`、`EveryFrames()`。
- TimerHandle.IsValid 查询实际注册状态，完成或取消后为 false；AfterFrames(0) 在下一帧执行，回调中新建 timer 不会改变原 handle 身份。
- 保存返回的 `TimerHandle`，在 owner cleanup point 调用 `Dispose()`。
- 秒 timer 在执行快照中的回调前重新检查注册状态；同帧其他回调取消的 timer 不再执行，回调中新建的秒 timer 等到下一次 Tick。
- 秒和帧使用不同 API；frame interval 必须是整数，循环 interval 必须大于 0。

### Audio

- `PlayAudio()` / `PlayLoopAudio()` 返回 `AudioVoiceHandle`，用于停止一次具体播放。
- BGM channel 同时只保留一个 active voice；effect voice 可并发。
- 播放实例必须由 `AudioVoiceHandle` 持有和停止；业务代码不按 clip name 批量停止 voice。
- 延迟与 Fade 使用非缩放时间；AudioListener.pause 时保留暂停 voice，不将其当成已结束音效回收。时间和音量参数拒绝 NaN/Infinity，音频类型拒绝未定义枚举值；完成 voice 使用复用列表收集。
- AudioAction 默认在销毁时停止其 voice；continueOneShotAfterDestroy 可显式把单次音效交给 AudioMgr 自然结束，循环音效不能脱离 owner。
- 延迟播放、Fade 和异步 clip load 都由 voice lifecycle 管理。
- Host 关闭时先停止 voice、取消 Fade，并销毁所拥有的 AudioSource，再释放资产；关闭后的异步 clip 回调不会重新启动播放。

### Pools

- pooled instance 由 `PoolToken.Key` 绑定到稳定 pool key，不能依赖可修改的 `GameObject.name` 归池。
- 地址加载池按 address 区分；`GetCloneObj()` 的模板池按模板对象身份区分，同名不同模板互不混用。克隆已有 pooled instance 时沿用其原池身份。Token 同时绑定创建它的 PoolMgr；模板池的 `Key` 是会话内标识，不可持久化或当作资源地址使用。
- 只有 `PoolMgr` 生成并带 `PoolToken` 的对象可以调用 `PushObj()`；重复归还会报错。
- 首次创建在 inactive staging parent 下完成 Token 设置，与复用共用激活路径；无论模板 activeSelf 如何，返回有效实例均已激活。
- spawn 时先恢复 parent/transform，再激活对象，确保 `OnEnable()` 观察到最终状态。
- `Clear()` 清理池内对象；已经交给 caller 的 active object 仍由 caller 负责。
- 取用时跳过已被外部销毁的池内对象；没有有效对象时重新创建。仍建议业务使用归池接口管理生命周期。

### Saved data

- 现有 ciphertext 是兼容合同，不能静默更换算法或 key derivation。
- `sys`、`data_<slot>` 是同一个 ES3 文件内的 key。删除槽位通过 `DeleteKey` 完成，不删除整个文件；删除成功后再清理当前槽位与更新索引。删除数据失败保留原内存状态；若后续索引写入失败，dirty 状态保留，可重试删除或显式 flush。
- `SaveImmediately=true` 即使值相同也会 flush 对应存储域的既有 dirty data；相同且已干净的数据不重复写盘。
- Easy Save 的 `ES3Defaults.asset` 类型扫描列表必须包含 `EasySave3`，以匹配当前 asmdef 边界；不改变存档 key、密文或序列化布局。
- 切换 slot 前会 flush dirty data；单字段解密/反序列化失败会记录错误并回退默认值。
- 读取/解码失败保留原文件并进入 IsReadOnly，ReadError 暴露原因；禁止写入、删档和切槽。外部恢复原文件后可 RetryRead；不提供自动覆盖损坏数据的入口。存在待提交数据时不允许重新初始化。
- Host 暂停、正常退出和关闭时提交 dirty data，失败记录日志；关键游戏节点仍应显式 ApplyChangesToDatabase 或立即保存，强制终止进程不保证收到生命周期回调。
- schema change 必须带 version、migration strategy 和旧格式兼容读取。
- 业务代码不得直接调用 Easy Save。

### Localization 与 RedDots

- GameConfig 的 enableLocalization / enableRedDots 默认 true，启动时生效。关闭后不创建对应服务，ConfigMgr 跳过 Language / RedDot 的数据加载；生成的对应表对象为空，其他表照常加载。
- `Services.HasLocalization` / `HasRedDots` 查询可用性；关闭时直接访问对应服务会给出明确异常。已有本地化组件保留 Inspector 内容、不订阅语言事件。红点示例关闭后不建立订阅。
- 这是运行时开关，不是代码裁剪。保留 Excel schema、生成类型和枚举；若需移除 schema，必须同时调整所有强类型引用并重新导表，不能手删生成文件。

- `LanguageMgr.CurrentLanguage` getter 无写盘副作用；设置相同语言不会重复发布事件。
- 文本、Sprite 和 GameObject variant 使用 English/可用资源 fallback，避免缺失翻译时空白。
- 语言类型统一为 `SystemLanguage`，`Chinese` 映射简体，`Unknown` 回退英语；Excel 覆盖 42 个实际语言列。跟随系统是独立模式。存档通过版本化的新 key 兼容旧五语言数字值，GameConfig 保留隐藏旧序列化字段用于一次性迁移。
- `GetLanguageById(id, out resolvedLanguage)` 返回逻辑原文和实际回退语言。`TextOfEnhance` 持有 RTL 排版/测量缓存，按实际行宽重排后绘制，不修改表数据、原文或字体资产；销毁时释放测量器和语言订阅，Font 回调在 disable 释放。字体由配置者持有。
- 图片和物体组件的 `variants` 按语言配置，保留旧五语言资源字段；已显示的相同物体不因重复刷新而重新激活。文本配置、翻译和视觉验收边界见 [多语言与 RTL 规则](localization.md)。
- 红点节点名称、路径、父子关系和旧回调写入口收回到 runtime，业务读取只读视图并通过 RedDotSystem 修改；中间节点路径使用实际完整路径，累计数量饱和到 int.MaxValue 防止溢出。
- RedDot 数量会 clamp 到 `>= 0`；新 listener 使用 `SubscribeRedDotNode()` 返回的 `IDisposable`。

### Generated configuration

- Excel 是 source of truth。
- `Config/SheetTool` 在导出前检查 C# 标识符、生成名称冲突和 int32/float32 范围。先在同级临时目录生成全部产物，提交异常时回滚；保留现有资源 GUID，格式切换迁移对应 meta，移除过期产物及 meta。文件系统强制中断仍需检查备份，不承诺断电级事务。
- 二进制 FCFG 后以 -1 标识格式版本 1，旧无版本格式仍可读取；未知版本、负长度、超过剩余字节或 1,000,000 的长度及尾部多余数据被拒绝，字符串使用严格 UTF-8。
- Unity 导表菜单异步等待子进程，提供取消和两分钟超时；编译域重载/退出时终止工具。
- `Client/Assets/Gen` 与 `Client/Assets/Resources/Config` 只能重新生成，不能手工编辑。

## Assembly boundaries

```text
EasySave3                         vendor persistence
Feather.UnityLogsViewer          vendor diagnostics
Feather.Config.Generated         generated config types
Feather.Runtime                  first-party runtime
Feather.Editor                   first-party editor tooling
Feather.Tests.EditMode           framework tests
Feather.Starter.Tests.Editor     isolated optional starter example tests
Assembly-CSharp                  project-specific game code
```

Game assembly 可以依赖 `Feather.Runtime`，runtime 不能依赖 game feature。只有当 domain code 已达到值得缩短编译范围的规模时，才新增 game asmdef。

`Feather.Tests.EditMode` 通过 friend assembly 访问现有服务的 internal 构造和初始化入口。存档测试使用独立临时文件；UI 测试通过 internal prefab-loading delegates 控制完成顺序，生产入口仍使用 `ResMgr`。测试能力不对业务程序集开放。

## Removed scope

旧 component-ID activation/save subsystem、通用 FSM、`MonoMgr` 和 generic singleton base 已移除。它们没有有效 caller，且会与当前 service boundary 重复。真实业务需要 state machine 时，应在 game code 中实现 domain-specific plain C# state machine。

## 优化准入

后续优化按真实问题触发，不为抽象而抽象：

- 现有独占 AssetScope 无法满足真实跨 owner 共享需求时，再考虑 `AssetLease` 或引用计数。
- 页面导航出现新的实际需求时，扩展已有 UIType/PanelHandle 导航，不另建 UI manager。
- 存档格式第一次发生不兼容变化时，建立明确的 schema version 和 migration。
- 场景切换已有并发限制与失败反馈；复杂多场景调度或失败回退按实际玩法需求扩展。
- 只有需要定位线上性能或生命周期泄漏时，才增加诊断统计。

当前目标是减少 API 入口、保持 ownership 清晰和便于测试，而不是继续扩大框架功能面。
