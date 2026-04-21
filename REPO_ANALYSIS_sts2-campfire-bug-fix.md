# sts2-campfire-bug-fix 仓库解析

- 仓库地址：`https://github.com/lcff0614/sts2-campfire-bug-fix`
- 观察时间（UTC）：2026-04-19

## 1. 这个仓库是做什么的
这是一个 **Slay the Spire 2（推测）Mod 热修复仓库**，核心是修复“Campfire 升级选卡”相关的手柄/焦点导航异常。

仓库主入口代码是 `src/CampfireUpgradeBugFix.cs`，通过 Harmony Patch 注入到 `NDeckUpgradeSelectScreen`：
- `OnCardClicked` 后置补丁
- `get_DefaultFocusedControl` 后置补丁
- `get_FocusedControlFromTopBar` 后置补丁

## 2. 技术栈与项目形态
从仓库结构和配置看，这是 **Godot + C#** 的 Mod 项目：

- C# 作为逻辑层（Harmony 打补丁）
- GDScript 作为打包脚本（生成 `.pck`）
- PowerShell 作为发布脚本

关键文件：
- `CampfireUpgradeBugFix.csproj`：`net9.0`，依赖 `Lib.Harmony 2.3.6`，并引用本地 `sts2.dll`
- `tools/build_pck.gd`：构建 `CampfireBugFix.pck`
- `tools/build_release.ps1`：聚合 dll/pck/manifest
- `CampfireBugFix.json`：mod 元数据（id/version/description）

## 3. 核心修复思路（代码行为）
`OnCardClicked` 的补丁做了几件关键事：

1. 通过反射拿到私有 `_grid` 和 `_selectedCards`
2. 把 `FocusBehaviorRecursive` 设回 `Inherited`
3. 首次选卡时手动把焦点关系“种”回 card holder
4. `ActiveScreenContext.Instance.Update()` 刷新焦点上下文
5. 然后 `Viewport.GuiReleaseFocus()` 主动释放当前 GUI 焦点

这是一种“**看上去焦点消失，但方向键导航上下文仍可继续**”的兼容性处理。

另外两个 getter 补丁用于兜底：
- 当原返回值为空时，回退到 `_grid` 上对应焦点属性，避免焦点链断裂。

## 4. 代码质量与风险点评
优点：
- 通过反射跨版本取字段/方法，兼容性较强。
- 有较完整日志，线上定位问题方便。
- 把“视觉焦点状态”和“导航上下文”分离处理，针对性较强。

潜在风险：
- 反射依赖私有成员命名（如 `_grid`、`_selectedCards`），游戏版本变更易失效。
- 代码 namespace 与日志标识中存在 `...BugFix2` 命名，和仓库名 `CampfireBugFix` 不完全一致，后续维护可能混淆。
- `build_release.ps1` 目前内容非常短，看起来像脚本片段，建议补充参数、复制步骤、压缩发布包与错误处理。

## 5. 发布状态快照
- 仓库提交历史非常小（当前仅 1 次提交）
- 已有 release：`1.0.0`（页面显示 2026-04-18）
- 语言占比：C# / GDScript / PowerShell

## 6. 可改进建议
1. 增加 README：
   - 复现步骤
   - 受影响游戏版本
   - 安装方式（dll/pck/manifest 放置位置）
   - 手柄与键鼠验证矩阵
2. 给 Harmony patch 增加“目标签名变化”检测日志（启动时自检）。
3. 将反射访问封装为可缓存的委托，降低每次点击的反射开销。
4. CI 自动构建：
   - 产出 zip（json + dll + pck）
   - 自动附加到 release

## 7. 一句话结论
这是一个 **体量小但定位精准** 的焦点导航热修复 Mod：
主修复点是“选卡后焦点可见状态与导航上下文失配”，通过 Harmony + 反射 + Focus 机制重建来恢复可操作性。

## 8. 本次改进实现（升级/删牌/变形统一支持）
针对你提出的需求，本地新增了 `src/CampfireNavigationParityPatch.cs`，核心点如下：

1. **从“单界面”升级为“多界面”统一补丁**
   - 目标类型扩展为：
     - `NDeckUpgradeSelectScreen`
     - `NDeckRemoveCardSelectScreen`
     - `NDeckTransformCardSelectScreen`

2. **使用 `TargetMethods()` 动态搜集目标方法**
   - 对每个界面尝试注入：
     - `OnCardClicked`
     - `get_DefaultFocusedControl`
     - `get_FocusedControlFromTopBar`
   - 某些版本缺失类型/方法时自动跳过，降低版本耦合。

3. **点击卡牌后的导航重建逻辑复用到所有目标界面**
   - 反射拿 `_grid` / `_selectedCards`
   - `FocusBehaviorRecursive => Inherited`
   - 首次选卡时补焦点邻接关系
   - `ActiveScreenContext.Instance.Update()`
   - `Viewport.GuiReleaseFocus()`

4. **getter 兜底逻辑通用化**
   - 当原 getter 返回 `null` 时，回退 `_grid.DefaultFocusedControl` / `_grid.FocusedControlFromTopBar`。

> 注意：删牌/变形界面的真实类型名若与上面不同，需要把字符串数组中的类型名替换成你当前游戏版本里的实际名称。

## 9. 原补丁原理（为什么“选中后还能导航到卡组”）
先把焦点问题拆成两层：

1. **可见焦点（UI 上看起来谁被选中）**
2. **导航焦点图（方向键/手柄按键下一步跳到哪）**

这个 bug 的关键在于：
- 选中一张卡后，界面上的“可见焦点”可能发生了变化甚至被清空；
- 但只要“导航焦点图”还在，方向键依然可能把焦点导向卡组区域。

所以原补丁不是简单“把焦点固定到一个控件”，而是做了三步组合拳：

- **重建焦点关系**：补齐 card holder 与 grid 的邻接关系（上/下方向）。
- **刷新上下文**：调用 `ActiveScreenContext.Instance.Update()` 让当前 UI 导航上下文重新计算。
- **释放当前 GUI 焦点**：`Viewport.GuiReleaseFocus()`，把“当前可见焦点”放开，让导航系统按关系图重新接管。

这样就能出现“看上去焦点被释放了，但方向键仍然能正确跳转”的效果。

## 10. 新补丁改进原理（升级 -> 升级/删牌/变形统一）
### 10.1 从“单点修复”变成“同构界面批量修复”
升级、删牌、变形这三类界面在交互模型上是同构的：
- 都是“卡牌选择列表 + 顶部/周边控件 + 方向键导航”
- 都可能在 `OnCardClicked` 后出现焦点链断裂

因此新补丁把原先只作用于升级界面的逻辑，提升为一个“同构界面通用模板”：
- 统一拦截点击后事件
- 统一做焦点关系重建
- 统一做 getter 兜底

### 10.2 动态目标发现（`TargetMethods()`）
新补丁不直接写死类型引用，而是：
- 用字符串列出候选类型名
- 运行时通过 `AccessTools.TypeByName` 查找
- 找到后再收集三个目标方法

好处：
- 某个类型在某版本不存在时，补丁不会因编译期依赖而崩。
- 能更平滑地跨版本工作（至少“可加载”层面更稳）。

### 10.3 两层兜底
- **第一层（点击后修复）**：主动重建导航关系并刷新上下文。
- **第二层（getter 兜底）**：当默认焦点 getter 返回 `null` 时，从 `_grid` 取回默认值。

这意味着即便第一层因为版本细节部分失效，仍有机会靠第二层避免“完全无焦点”。

## 11. 实现可靠性评估（真实可用性角度）
### 11.1 可靠性增强点
1. **弱依赖反射**：类型/方法缺失时“跳过而非崩溃”。
2. **异常隔离**：补丁内部 `try/catch` 吞异常，避免影响主流程。
3. **逻辑分层**：点击后重建 + getter 兜底，降低单点失败概率。

### 11.2 仍然存在的风险
1. **私有字段名漂移风险**：`_grid`、`_selectedCards` 若改名，修复链会降级。
2. **行为语义漂移风险**：即便方法名还在，内部时序变化也可能让修复效果减弱。
3. **类型名不一致风险**：删牌/变形真实 screen 类型名可能与假设不一致。

### 11.3 提升到“工程级可靠”的建议
1. **启动自检日志**
   - 打印每个目标类型/方法是否已挂补丁；
   - 打印 `_grid` / `_selectedCards` 是否解析成功。
2. **版本白名单/黑名单**
   - 对已验证版本开启完整修复；
   - 对未知版本自动降级到最安全路径（仅 getter 兜底）。
3. **埋点统计（可选）**
   - 记录“点击后方向键首次导航成功率”，便于比较不同版本效果。
4. **可配置类型名**
   - 把目标 type name 放到配置文件，避免每次改游戏版本都要改代码重编。

## 12. 一句话结论（面向你当前诉求）
你要的“升级/删牌/变形都能在选卡后继续导航到卡组”在机制上是同一类焦点链修复问题；
新补丁通过“动态挂载 + 焦点关系重建 + getter 兜底”实现了跨三个界面的统一处理，
在兼容性和容错性上比原先单界面补丁明显更可靠，但最终效果仍取决于目标版本的私有字段/类型命名是否保持一致。

## 13. 术语直白解释
- **私有字段命名**：指类里 `private` 成员变量的名字，比如 `_grid`、`_selectedCards`。这些名字不是对外 API，游戏更新时可能被改名。
- **类型命名**：指类本身的名字，比如 `NDeckUpgradeSelectScreen`、`NDeckRemoveCardSelectScreen`。如果版本更新改了类名，按旧名字反射就找不到。

所以我前面说“私有字段/类型命名风险”，本质就是：**补丁依赖了这些字符串名字，一旦官方改名，反射定位就会失效或部分失效**。
