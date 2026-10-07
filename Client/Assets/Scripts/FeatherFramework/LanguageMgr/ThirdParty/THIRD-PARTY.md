# 文本算法依赖

仅由现有 Localization/TextOfEnhance 使用，不增加服务或运行时包加载。源代码随项目编译，适用于 Mono/IL2CPP；未引入原生库。

## RichTextKit 双向算法

- 上游：https://github.com/toptensoftware/RichTextKit
- 固定提交：`e28a3f583a0d9b2221baab25b730f3bc5a863d35`
- 许可证：Apache-2.0，原文见 `RichTextKit/LICENSE.txt`，源文件保留版权声明。
- 引入 Bidi/BidiData、Unicode 方向/括号类型、UnicodeTrie 及其必要工具源文件，不引入 Skia/HarfBuzz 和 RichTextKit UI。
- 本地 `UnicodeClasses.cs` 仅保留 bidi 查询，把同提交的 `Topten.RichTextKit/Resources/BidiClasses.trie` 原始字节编码为 Base64 嵌入，以适配 Unity 不自动嵌入 .NET resource 的构建流程。其余上游源文件不修改算法。
- 组件为每个段落建立方向级别，在实际换行后执行行尾空白重置及按级别重排，组合标记和代理对随文字簇保留。成对括号使用同一数据表镜像，额外处理尖括号。

## Arabic Support 字形连接

- 上游：https://github.com/Konash/arabic-support-unity
- 固定提交：`2078874a4632e0fb6f3ed63a8d56fa8c971471ee`
- 许可证：MIT，原文见 `ArabicSupport/LICENSE.txt`，源文件保留版权声明。
- 本地小改动：新增 internal `ShapeLogical`，保留变音符号、不合并标记、不替换数字；在上游视觉反转之前返回字形结果；补齐孤立字形，保留 lam-alef 的 U+FFFF 占位以对应原索引，显示输出时移除占位。调用点对其共享工作缓冲加锁。
- 不使用该库原有的启发式字符串反转，双向排序交给 RichTextKit。它是连接字形替换，不是完整 OpenType 字体排版引擎。
- 上游字符映射器的惰性单例改为静态只读映射表，不新增全局服务或单例。

## 更新约定

固定上游提交与许可证一起更新。先核对本地适配，再运行 `RtlTextTests`、实际文本生命周期/布局用例，以及目标字体的视觉验收。不要用简化字符串 Reverse 替换算法，也不要删除版权声明。范围与限制见仓库 `docs/localization.md`。
