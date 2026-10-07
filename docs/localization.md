# 多语言、翻译与 RTL 文本规则

本说明适用于人工开发者与 AI。修改语言表、翻译、字体或文本组件前先阅读本文件；服务入口仍为 `Framework.Services.Localization`，不另建语言管理器。

## 语言与表格

- 公共语言类型统一为 `UnityEngine.SystemLanguage`。`GameConfig.followSystemLanguage` 决定首次默认值是否跟随系统，关闭时使用 `defaultLanguage`。玩家选择优先于 GameConfig。
- `Localization.CurrentLanguage = SystemLanguage.Arabic` 选择固定语言；`Localization.FollowSystemLanguage()` 立即读取系统语言并记住该模式，下次启动重新读取。没有后台系统语言轮询。
- Unity 2022.3 的语言均有读取路径。`Chinese` 归一为 `ChineseSimplified`，`Unknown` 回退 `English`，废弃拼写 `Hugarian` 与 `Hungarian` 同值，表头使用 `Hungarian`。表内共 42 个语言列，不为别名或未知状态另建列。
- Excel 是译文唯一来源：`Config/SheetTool/Excels/Language.xlsx`。前三行依次为字段名、类型、中文说明；字段名使用 Unity 语言名称，类型为 `string`，第四行起为数据。
- 新列留空直到有经确认的译文，不批量复制英语来假装翻译完成。运行时按“当前语言 → English → ID”回退；null 和空字符串视为缺译，空格不是缺译标记。
- 更新 Excel 后运行 `Config/SheetTool` 下的 `npm run sync`；只有 Node 时可执行等价的 `node cli.js sync`。禁止手改 `Client/Assets/Gen` 和 `Client/Assets/Resources/Config`。代码和数据须一同更新，尤其不能混用旧二进制数据与新增列后的读取器。
- 升级 Unity 并新增语言时，核对 Excel 列和 `LanguageTableAccess` 强类型映射；`LocalizationTests.EveryUnityLanguageHasAStronglyTypedTableColumn` 检查覆盖情况。
- SystemLanguage 不定义国家/地区、复数规则或货币格式。地区差异需另行设计，不能擅自发明枚举或表头。

## AI 翻译规则

1. 保存正常书写顺序的 Unicode 原文。禁止反转阿拉伯文/希伯来文、预生成连接字形或把显示结果写回 Excel。
2. 保留 ID、字段名、类型行、占位符与富文本语义。`{0}`、`{1:N0}` 等格式项的索引和格式不能改写；可按译文语序调整位置。保留转义花括号、产品名和不可翻译标识符。
3. 翻译完整模板，不把句子拆成多个翻译片段再拼接。数字、英文名称与 RTL 混排交给显示层处理，不倒序数字或替换数字体系。
4. 保持 `b/i/color/size` 标签正确嵌套、匹配原语义；不要翻译标签名、颜色代码或字号参数。不要在阿拉伯单词或 lam-alef 连字内部切换样式。
5. 显式换行用于语义分段；适应宽度交给组件。不要为某个分辨率硬塞空格或换行，也不要无说明地添加 LRM/RLM、方向覆盖、隔离等不可见字符。
6. 译文不确定时保留空单元格并说明缺口。新增列不等于翻译完成；提交前检查目标语言缺译、占位符一致性和标签结构。

## 文本组件配置

静态本地化文本使用 `TextOfEnhance`，设置 `languageId > 0`。组件通过既有语言事件刷新；回退英语时使用英语的字体和段落方向。普通 `UnityEngine.UI.Text` 和 TMP 不会自动获得本组件的处理。

| 配置 | 规则 |
| --- | --- |
| `enableRtl` | 默认开启。存在 RTL 字符时做双向排序和阿拉伯字形连接；纯中文、英文保持正常显示。 |
| `mirrorRtlAlignment` | 默认开启。RTL 段落左右对齐互换，居中不变；关闭则保留原对齐。原始 alignment 不被改写。 |
| `font` | 必须配置的基础字体，由 prefab/场景或 caller 持有；启用 RTL/字体覆盖时基础字体也使用 Dynamic Font。 |
| `rtlFont` | 可选 RTL 字体；没有语言专用字体时用于包含 RTL 字符的文本。未设置时使用基础字体，必须确认字符覆盖。 |
| `languageFonts` | 按实际显示语言配置，优先于 rtlFont 和基础字体。每种语言只配置一次，重复时第一个有效条目生效。混排字体须覆盖同一行的英文和数字。 |
| Horizontal Overflow | Wrap 按 RectTransform 宽度排版并逐行重排；Overflow 仅使用显式换行。 |
| Best Fit | 在最小/最大字号内重新测量排版。最小字号仍放不下时遵守配置的截断/溢出，不保证任意内容都能容纳。 |
| Rich Text | RTL 路径支持嵌套 b/i/color/size。RTL 中不支持 quad/material 等其它标签；此类需求使用独立图文组件。 |

RTL 与语言覆盖字体使用 **Dynamic Font**，必须包含实际字符、标点、阿拉伯 Presentation Forms、需要的变音符号和混排字符。只有字体名称相同不能证明 Player 有字体：应配置有分发许可的项目字体资源，不依赖开发机系统字体。AI 不得随意复制系统字体入库。rtlFont 为空不是字体已配置完成。

组件只借用字体，不修改或销毁字体资源；动态加载字体的 caller 在所有使用者结束后释放。排版缓存和测量 TextGenerator 由组件持有，销毁时清理。Font.textureRebuilt 在 enable 订阅、disable 释放；语言事件在 Start 订阅、destroy 释放，失活期间仍接收语言更新。

`TextOfEnhance.text` 始终是逻辑原文，直接赋值仍可用并自动判断方向。动态文案推荐 `SetRawText`，设置 `languageId = 0`，避免 Start/语言事件覆盖业务文案：

```csharp
var localization = Framework.Services.Localization;
string template = localization.GetLanguageById(messageId, out var resolvedLanguage);
string message = string.Format(template, playerName, score);
label.SetRawText(message, resolvedLanguage);
```

动态文案由业务 owner 订阅 `FrameworkEvents.LanguageChangedId` 后重新取模板、格式化、赋值，并在 cleanup point Dispose 订阅。不要把已排版的文本再次传入 SetRawText。运行时修改 rtlFont、languageFonts 等公开配置字段后调用 `RefreshTextLayout()`；字号、宽度等 Text 属性变化会在重建时重新计算。

迁移普通 Text 时保留 RectTransform、颜色、布局、字体、引用及 `.meta`，不批量重建 prefab。新文本复用框架创建菜单。关闭本地化模块只控制表读取与语言事件，不禁止组件对手工原文做 RTL 显示。

## 图片与物体

`LanguageSpriteSwitch.variants` 和 `LanguageGameObjectSwitch.variants` 使用 SystemLanguage 配置资源。每种语言只配一次，第一个有效条目生效。优先顺序：当前语言列表项 → 旧五语言字段 → 英语列表项/EN → 旧可用资源 → 首个有效列表项。旧 CN_S/CN_T/EN/JA/KO 字段保留原引用。

物体列表只放由本组件独占控制的 variant；不要包含组件自身、祖先或嵌套互为祖先的 variant。刷新时只切换必要的 active 状态，不销毁资源；订阅在组件销毁时释放。

## 显示边界与验证

- 双向算法按段落解析，实际换行后逐行重排，保持从上到下的阅读顺序。阿拉伯连接字形、常用成对括号、数字、英文片段、代理对和组合标记分别处理。
- 适用范围是 Unity 2022.3 旧 uGUI 的阿拉伯语/希伯来语静态显示。本实现不是完整 OpenType 排版引擎；复杂变音符号的精确位置取决于字体和旧 Text 渲染器。印地语等复杂文字不能仅因表内有列就声明视觉适配完成。
- 不用于 InputField 光标、选择、编辑或聊天输入。打字机效果不能直接对视觉字符串 Substring；这些功能需要独立验证的编辑/排版组件。
- 不镜像整个 UI、图片、图标、按钮顺序或 RectTransform；布局镜像按页面设计。不要用负缩放、整段 Reverse 或负行距模拟 RTL。
- 排版修改至少验证阿拉伯连字、希伯来组合标记、英文数字/括号混排、富文本、显式/自动换行、宽度变化、Best Fit、缺译英语回退，以及切回 LTR 后字体/对齐恢复。
- 涉及激活、销毁或资源 variant 时运行直接相关生命周期用例；翻译/字体修改还需在目标 Player、实际字体和最窄布局下视觉验收。自动测试不能代替母语审校或所有设备排版检查。

依赖版本、许可证和适配说明见 [第三方文本算法](../Client/Assets/Scripts/FeatherFramework/LanguageMgr/ThirdParty/THIRD-PARTY.md)。
