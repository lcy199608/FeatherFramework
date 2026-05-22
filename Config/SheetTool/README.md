# SheetTool

`SheetTool` 是 FeatherFramework 的本地 Excel 导表工具。它读取 `xlsx` 文件，校验常用配置规则，生成 Unity 侧 C# 配置代码，并导出 JSON 到 `Resources/Config`。

## 目标

这套工具只保留常用功能，优先保证：

- 上手简单
- 目录清晰
- 校验明确
- Unity 里一键同步

## 环境准备

1. 安装 `Node.js`。
2. 在 `Config/SheetTool` 目录执行：

```bash
npm install
```

3. 把配置表 `xlsx` 文件放进 `Config/SheetTool/Excels`。
4. 如有特殊主键、索引或行类型命名需求，再修改 [`config.json`](/Users/a112233/UnityProject/FeatherFramework/Config/SheetTool/config.json)。

## 目录约定

默认目录结构：

```text
Config/SheetTool/
  config.json
  Excels/
    Enums_UI.xlsx
    Enums_Battle.xlsx
    Language.xlsx
    RedDot.xlsx
```

`Excels/` 目录下：

- 文件名匹配 `Enums*.xlsx` 的会被当成枚举文件并自动合并
- 其他 `xlsx` 会被当成普通配置表并默认导出

不需要逐张表登记。

## 导入格式

Unity 使用一个 `ScriptableObject` 配置当前导入格式：

- `Json`
- `Bin`

配置文件路径固定为：

```text
Assets/Data/ConfigImportSettings.asset
```

如果文件不存在，第一次执行下面任一菜单时会自动创建：

- `FeatherFramework/Config/Select Import Settings`
- `FeatherFramework/Config/Sync Excel Config`

当前导入规则：

- `Validate` 只校验 Excel，不关心导入格式
- `Sync` 会读取 `ConfigImportSettings.asset` 中的格式配置
- 导入 `Json` 时，`Resources/Config` 下只保留 `.json`
- 导入 `Bin` 时，`Resources/Config` 下只保留 `.bytes`

也就是说，`Resources/Config` 里始终只保留一种格式，另一种缓存会在同步时被删除。

## 常用命令

```bash
npm run validate
npm run export
npm run sync
```

命令说明：

- `validate`：只校验 Excel，不生成文件。
- `export`：直接生成代码和 JSON。
- `sync`：先校验再导出，日常使用就执行这个。

## Unity 中的使用方式

项目里已经接了 Unity 编辑器菜单：

- `Tools/Config/Validate Excel Config`
- `Tools/Config/Sync Excel Config`
- `Tools/Config/Select Import Settings`

点击后会自动执行对应命令，并在完成后刷新 Unity 资源。

## 配置文件

主配置文件是 [`config.json`](/Users/a112233/UnityProject/FeatherFramework/Config/SheetTool/config.json)。

关键字段：

- `excelDir`：Excel 所在目录。
- `enumFilePattern`：枚举文件匹配规则，默认 `^Enums([_-].+)?\.xlsx$`。
- `enumSheetName`：枚举所在工作表名；留空时读取第一个工作表。
- `namespace`：生成代码使用的 C# 命名空间。
- `outputCodeDir`：生成的 C# 文件输出目录。
- `outputDataDir`：生成的 JSON 文件输出目录。
- `tableOverrides`：可选，用于覆盖少数表的配置。

`tableOverrides` 按表名配置，常用字段：

- `sheetName`：工作表名；留空时读取第一个工作表。
- `rowName`：生成的行数据类名，默认是 `表名 + Info`。
- `idField`：主键字段名，默认是 `id`。
- `indexes`：额外索引字段列表。

## 普通表格式

普通数据表固定使用 3 行表头：

1. 字段名
2. 字段类型
3. 注释

第 4 行开始写数据。

示例：

```text
id | name | type
int | string | enum:RedDotType
主键 | 名称 | 红点类型
1 | Root | Root
2 | Mail | RedDotTest1
```

如果文件名是 `RedDot.xlsx`，默认会生成：

- 表类：`cfg.RedDot`
- 行类：`cfg.RedDotInfo`

## 枚举表格式

枚举表固定列结构：

```text
EnumName | Name | Value | Comment
```

示例：

```text
RedDotType | Root | 1 | 根节点
RedDotType | Mail | 2 | 邮件红点
```

支持多个枚举文件，例如：

```text
Enums_UI.xlsx
Enums_Battle.xlsx
Enums_System.xlsx
```

工具会自动合并这些文件中的枚举定义。

约束规则：

- 一个枚举类型只能定义在一个枚举文件里
- 同名枚举跨文件重复定义会直接报错
- 同一个枚举里的枚举项名称或数值重复也会报错

表里如果想使用枚举字段，字段类型直接写：

```text
enum:RedDotType
```

单元格内容填写枚举项名字，而不是数字。例如：

```text
Root
Mail
```

## 支持的字段类型

- `int`
- `float`
- `string`
- `bool`
- `int[]`
- `float[]`
- `string[]`
- `bool[]`
- `enum:EnumName`
- `enum:EnumName[]`
- `ref:TableName`
- `ref:TableName[]`

## 跨表引用

如果一个字段引用另一张表，字段类型写：

```text
ref:Item
```

这表示当前字段引用 `Item.xlsx` 这张表的主键。

例如 `Monster.xlsx` 里：

```text
id | dropItemId
int | ref:Item
主键 | 掉落道具
1 | 1001
```

同时 `Item.xlsx` 里必须存在：

```text
id | name
int | string
主键 | 名称
1001 | Potion
```

工具会在导表时自动校验：

- `Item` 这张表是否存在
- `dropItemId` 填的值是否真的能在 `Item.id` 里找到

数组引用同理，写法是：

```text
ref:Item[]
```

## 当前校验

- 主键字段必须存在
- 主键唯一
- 额外索引唯一
- 类型合法
- 枚举值合法
- 引用表存在
- 引用值必须能在目标表中找到

## 产物说明

执行导表后会生成两类产物：

- C# 代码输出到 `Client/Assets/Gen`
- 当前启用格式的数据输出到 `Client/Assets/Resources/Config`

运行时通过 `cfg.Tables.Load()` 自动识别当前资源是 `JSON` 还是 `Bin` 并完成加载。
