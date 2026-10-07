# Runtime API 迁移说明

本文集中说明旧代码如何迁移到当前框架 API。旧 API 仅用于识别和迁移旧代码；当前业务代码应使用窄的 typed API。

## Service construction

service 由 `FrameworkHost` 统一创建。`ResMgr`、`TimerMgr`、`SceneMgr`、`PoolMgr`、`LanguageMgr`、`RedDotSystem` 等构造函数，以及 `ConfigMgr`、`AudioMgr`、`UIMgr` 的初始化方法，不对业务程序集开放。业务代码只消费 `Framework.Services`，避免产生第二套 manager 或不完整的依赖图。

| 旧访问方式 | 当前访问方式 |
| --- | --- |
| `ResMgr.Instance` | `Framework.Services.Assets` |
| `EventCenter.Instance` | `Framework.Services.Events` |
| `SaveDataMgr.Instance` | `Framework.Services.Save` |
| `ConfigMgr.Instance` | `Framework.Services.Config` |
| `TimerMgr.Instance` | `Framework.Services.Timers` |
| `SceneMgr.Instance` | `Framework.Services.Scenes` |
| `PoolMgr.Instance` | `Framework.Services.Pools` |
| `AudioMgr.Instance` | `Framework.Services.Audio` |
| `UIMgr.Instance` | `Framework.Services.UI` |
| `LanguageMgr.Instance` | `Framework.Services.Localization` |
| `RedDotSystem.Instance` | `Framework.Services.RedDots` |

Scene component 可继承 `FrameworkBehaviour` 使用 `Services`；普通 C# class 通过 constructor 接收窄依赖。`FrameworkHost` 在首个 scene 前自动创建，不需要手工放置 manager object。

旧 `MonoMgr`、generic singleton base、component-ID activation/save layer 和通用 FSM 已移除。协程使用 `MonoBehaviour` 生命周期，plain class 接收 timer 或所需依赖；存档统一使用 Save，有业务需求时在 game code 中实现领域状态机。

## UI

- 新页面使用可 await 的 `OpenRoot<T>(data = null)`、`OpenPage<T>(data = null)`、`OpenChild<T>(owner, data = null)`。返回 PanelHandle，使用 `Close(handle)` 精确关闭、`Back()` 返回。旧 ShowUI/ShowUIAsync 继续兼容。
- 新面板覆盖 `Type => UIType.Root / Page / Child`，层级通过 Layer 属性声明。旧 IsRoot=true 可逐步迁移为 Root；IsStackable=true 仍为兼容自由叠加，不能直接当成 Child，迁移时必须同时改为带 owner 的 OpenChild 调用。
- Child 只能归属已打开的 Root/Page，不能使用已关闭的旧 handle。同类 Child 可有多个独立实例；旧按名/类型关闭会关闭同名全部实例，新代码使用 handle。Panel 内可调用 `OpenChild<T>(data)` 自动绑定自身。
- Child 默认继承 owner 实际使用的层级，包括旧 ShowUI 显式传入的 UILayer。恢复期间若 owner 或其他 Child 的 OnShow 切换页面，不会继续显示已隐藏 owner 的 Child；调用方式无需调整。
- Page 被覆盖只隐藏，返回时恢复它和仍打开的 Child。关闭 owner 同时关闭 Child 并取消待完成的加载。Root/Page 缓存实例，关闭后重新打开获得新的 handle；Child 关闭即销毁。
- 新 Page 请求按发起顺序提交；切换 Root 取消旧流程的新 API 请求。先 await Root 完成，再为它打开 Page。Task 取消表示请求已被关闭、替代或所属流程退出；业务异步入口处理 OperationCanceledException，加载或初始化失败正常处理异常。
- `PanelBase.OnInit()` 只用于一次性初始化；重复显示和隐藏使用 `OnShow()` / `OnHide()`。
- `OnClose()` 清理本次打开建立的 subscription、timer 等，暂时隐藏不会调用它；OnInit 建立并需要跨关闭保留的实例资源继续在 OnDispose 清理。HideUI/HideAllUI 现在也会执行 OnClose。
- `ShowUI()` 暂时保留，主要用于启动阶段和兼容旧代码。
- `HideAllUI()` 现在同时取消当前待完成的打开请求。Root 切换后，较早发起的异步页面不会再弹出；需要在新 Root 上显示的弹窗，建议在 Root 的完成回调中打开。
- 新实例在 inactive 状态执行 `OnInit()`，不再假定 `Awake()` 先于 `OnInit()`；请把 Panel 所需的一次性初始化放在 `OnInit()`。`OnHide()` 在对象失活后执行，不应依赖 `activeSelf` 仍为 true。
- UI/host 关闭会自动执行每个 Panel 的 `OnDispose()`，且至多一次。清理异常被隔离；清理回调内重新打开 UI 的请求被拒绝，新 API 返回取消 Task。
- `OnInit()` 失败的部分实例也会执行最终清理，不会进入导航流程。
- ShowUIQueue 保留按数组顺序逐个打开的用途，开始队列会关闭当前 Page 栈并保留 Root，关闭当前栈顶后继续队列；新项目优先显式打开和返回。

## Audio

- `PlayAudio()` 和 `PlayLoopAudio()` 现在返回 `AudioVoiceHandle`。需要停止某一次播放时，应保存 handle 并调用 `Stop()`。
- `StopAudio(string)` 不再对业务程序集开放；需要停止具体播放时保存 `AudioVoiceHandle` 并调用 `Stop()`。
- BGM channel 同时只保留一个活动 voice。
- host 关闭会停止所有 voice、清理 AudioSource，再释放资产；业务继续通过 voice handle 管理日常播放。

## Timer and events

- 新 timer 使用 `AfterSeconds()`、`AfterFrames()`、`EverySeconds()` 或 `EveryFrames()`，并保存返回的 `TimerHandle`。
- 新框架事件使用 `EventId` / `EventId<T>`；原 string overload 已收回到 runtime 内部。
- `Subscribe()` 返回的 `IDisposable` 必须由 owner 在 cleanup point 释放。
- Timer 的同帧取消立即生效：即使 timer 已在本次 Tick 快照中，被其他回调 Dispose 后也不再执行。
- 旧 `CreateNewTimer(..., isSecond)` 改用上述秒/帧 API；`AddEventListener` / `RemoveEventListener` 改为 `Subscribe()` / `Dispose()`，`EventTrigger` / string Publish 改为 `Publish(EventId<T>, payload)`。
- 红点旧 `SetRedDotNodeCallBack` 改用 `SubscribeRedDotNode()`，并在 owner 清理时 Dispose 返回的订阅。

## Pool and save

- 只有由 `PoolMgr` 生成、并带有 `PoolToken` 的实例可以归池；不要修改 token。
- 切换 save slot 会先 flush 当前 dirty data，避免延迟保存的数据丢失。
- 存档加密格式没有变更，现有存档保持可读。单个损坏字段会记录错误并回退调用方提供的默认值。
- 删除槽位现在移除共享 ES3 文件里的 `data_<id>` key；重新加载已删除槽位得到默认数据。不会删除其他槽位或系统设置，也无需迁移旧 ciphertext。历史版本已经从索引移除但留下的数据，可再次指定槽位 ID 删除。
- 删除数据失败保留当前内存状态；数据删除成功而索引写入失败时，dirty 索引可通过重试删除或 `ApplyChangesToDatabase()` 提交。磁盘上可能暂时存在旧索引，但已删除数据不会重新写回。不会自动扫描或清理未指定的历史槽位。
- 相同值的 `SaveImmediately=true` 请求会提交此前尚未写盘的同一存储域数据；已干净时仍不重复写盘。
- `GetCloneObj()` 以模板身份区分池，不能再通过同名模板共享对象。`PoolToken.Key` 对模板池是非持久化的会话内标识；业务不要把它解析为名称或地址。
- Easy Save 类型扫描配置增加 `EasySave3`，修复 asmdef 拆分后首次保存无法发现 primitive serializer 的问题；原路径和加密设置保持不变。

- 业务代码使用带类型的 `ReleaseRes<T>()`；全量释放由 `FrameworkHost` 负责。

## Localization 与文本

- `LanguageMgr.SupportedLanguage` 已移除；`CurrentLanguage` 和资源 variant 统一使用 `UnityEngine.SystemLanguage`。例如原 `SupportedLanguage.Japanese` 改为 `SystemLanguage.Japanese`；原 `Default` 操作改为 `Localization.FollowSystemLanguage()`。`Unknown` 表示英语回退，不表示跟随系统。
- 旧 `GameConfig.language` 公共字段改为 `followSystemLanguage` 和 `defaultLanguage`。原字段名作为隐藏 int 保留，反序列化时按旧 0–5 数值一次性迁移；已有 asset 不需要手工重写 YAML。之后由正常 Unity 保存落盘。
- 旧存档 `LanguageSaveData` 的数字仍按 Default/CN_S/CN_T/English/Japanese/Korean 读取，写入新键 `LanguageSaveData.v1`，其中记录 Version=1、FollowSystem 和语言名称。旧 key 保留，密文和 SaveDataMgr 布局不变；只读存档不自动写入。未知新版记录不会在初始化时覆盖。
- `GetLanguageById(id)` 保持返回逻辑原文。新增 `GetLanguageById(id, out resolvedLanguage)` 用于缺译后正确选择字体和方向。空英语也会回退 ID。
- `LanguageSpriteSwitch` / `LanguageGameObjectSwitch` 新增 `variants`；旧五语言字段保留原引用，新列表匹配优先。不要把控制器自身或祖先配置为 variant。
- `TextOfEnhance.text` 保留逻辑原文，RTL 仅在生成显示文本时转换。动态格式文本使用 `SetRawText(message, resolvedLanguage)`；字体、对齐、富文本限制和翻译规范见 [多语言与 RTL 规则](localization.md)。已有 TextOfEnhance 不需要替换组件或重建 prefab；普通 Text 不会自动获得这些能力。

## Validation

- `Scripts/verify.ps1` 支持 `-NodePath` 或 `FEATHER_NODE_PATH`。
- 验证默认行为调整：无参数现在仅检查架构，不运行 Node 或 Unity；原 `-UnityPath ...` 全量命令需改为 `-Scope Full -UnityPath ...`。只传 UnityPath 会报错提示选择范围，避免旧脚本静默跳过 Unity。
- 日常使用 `-Module ui` 等快捷名或 `-TestFilter` 筛选测试，自动选择 Tests 范围并跳过导表；ui-lifecycle 单独进入 Play Mode。导表用 `-Scope SheetTool`，旧 `-SkipUnity` 保留为该范围的别名。范围与筛选冲突会报错，不能把 Full 静默缩成局部测试。
- `-Plan` 只预览执行范围。局部结果写到 `TestResults/editmode-<模块>.xml`（自定义筛选为 custom），全量写到 editmode.xml，各自日志同名 .log。历史 unity-verify.log 改为 editmode.log。
- 测试筛选没有命中任何通过用例、未生成 XML 或运行失败都会报错，不能把空运行记为成功。纯文档修改不要求 Unity 或导表测试。
- 完整 Unity 验证现在要求生成有效的 `editmode.xml`；Unity licensing、项目锁或测试启动失败不会再被误报为通过。
- `-runTests` 不再搭配 `-quit`；由 Test Framework 写出结果并自行退出，避免启动后直接退出而漏跑测试。

## Framework initialization

- `FrameworkHost.State` 提供 `NotStarted/Initializing/Ready/Failed` 状态。
- 初始化失败时读取 `InitializationError`；环境问题恢复后可调用 `RetryInitialize()`。
- `Framework.Services` 只能在 `Framework.IsReady` 或 `FrameworkReadyId` 之后访问。
- 必需 Canvas 缺失或结构不合法时进入 Failed，可在恢复资源后重试。Canvas 的组件在服务发布后才激活。

Host 按 UI、Audio、Localization/RedDots、Save、Timers、Pools、Scenes、coroutines、Events、Assets 的顺序关闭服务，最后清空服务入口；Panel 清理期间公共服务仍可用。

## Scene、资源作用域与可选模块

- 场景旧两参数 LoadSceneAsync 继续兼容，可增加第三个失败回调；新代码使用 await SwitchAsync(index)。同目标并发合并，不同目标并发明确失败；同步加载不能插入正在进行的异步加载。调用方应处理失败、恢复按钮状态。
- 现有普通资源缓存行为不变。关卡独占资产可改用 Assets.CreateScope() 返回的 scope.LoadAsync，结束时 Dispose。scope 不接管已在普通缓存或其他 scope 中的资源；不要用它加载由 UI、Audio、Pool 缓存持有的共享资源。
- GameConfig 新增 enableLocalization / enableRedDots，默认启用以兼容旧资产。仅主动关闭时，业务需检查 HasLocalization / HasRedDots；关闭的服务访问会抛出明确异常。对应配置表跳过加载、返回空表，但 schema 和生成类型仍保留。
- 导表生成的 Tables.Load() 兼容原调用；可选 shouldLoad 谓词让未选中的表保持空集合，不访问其数据资源。

## 生命周期和工具边界

- 保留服务对象引用不能绕过 Host 关闭：资源、timer、对象池取用、事件发布/订阅、存档写入等在关闭后被拒绝。Clear/ReleaseAll 不等于 Shutdown；释放订阅和 handle 仍安全。
- SaveDataMgr 新增 IsReadOnly、ReadError、RetryRead。读取失败时禁止修改原档；恢复文件后再重试，正常退出和暂停会提交延迟写入。关键进度仍需显式提交，旧密文和 key 派生不变。
- TimerHandle.IsValid 现在表示仍注册；AfterFrames(0) 改为下一帧执行，不再在创建调用内同步执行。
- Pool 首次创建现在和复用一样返回激活实例，OnEnable 可观察到 PoolToken 与最终 Transform；首次初始化仍不要依赖 Awake 先于 Token 设置。
- AudioAction 销毁时默认停止 voice；需要音效在对象销毁后继续，显式启用 continueOneShotAfterDestroy（仅非循环）。Fade 使用非缩放时间；NaN/Infinity 参数报错。
- 泛型 UI 同名异类冲突会提前报错，面板类名仍需全局唯一；owner 层级改变会更新现有 Child 的 Layer。
- RedDotNode 的结构和旧回调不再允许业务写入；改用 RedDotSystem.SetInvoke / SubscribeRedDotNode / RemoveRedDotFromTree，读取 dicChildren 不受影响。
- 示例使用完整场景路径，Build Settings 调序不需修改数字索引；示例测试已移入独立程序集，可连同示例移除。
- UI 生成器以选中对象为根，拒绝含斜线或同名兄弟节点的歧义路径，处理字符串转义、C# 关键字及同名 prefab 冲突。不会覆盖已有业务脚本。
- 配置二进制新增版本标记，生成的读取器兼容旧二进制；升级 exporter 后同步重新生成读取器与数据。导出错误保留旧产物，生成类型冲突和数值越界在导出前报告。
