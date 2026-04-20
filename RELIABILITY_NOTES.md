# 可靠性说明与排障信息

为避免主观臆测，这个补丁在运行时会输出自检日志：

- 哪些 screen 类型被找到/没找到
- 哪些目标方法可被补丁/缺失
- 关键反射点（`_grid`、`CardHolder`、`ActiveScreenContext`、`Viewport.GuiReleaseFocus`）是否可用

## 你遇到问题时请反馈这些信息
1. 游戏版本号
2. MOD Loader 版本
3. 完整日志中包含 `[CampfireNavigationParityPatch]` 的行
4. 复现步骤（在哪个界面、按了哪些键、预期和实际）

> 这样我可以基于证据定位，而不是靠猜。

## 商店删牌无效时的最小排障
请先看日志里是否出现：
- `未找到类型: NShop...`
- `缺少方法: OnCardClicked / OnDeckCardClicked / OnCardSelected`

如果出现，请把真实类型名补到 `mod/FocusPatchTargets.txt` 再试。

## 新增可扩展配置（无需改代码）
你现在可以按版本差异直接改这些文件：
- `mod/FocusPatchTargets.txt`（目标界面类型）
- `mod/FocusPatchClickMethods.txt`（点击方法）
- `mod/FocusPatchGetterMethods.txt`（焦点 getter）
- `mod/FocusPatchGridFields.txt`（grid 字段名）
- `mod/FocusPatchSelectedFields.txt`（已选卡字段名）
- `mod/FocusPatchCardHolderProps.txt`（CardHolder 属性名）
- `mod/FocusPatchFallbackGridProps.txt`（grid 焦点回退属性）
- `mod/FocusPatchBehavior.txt`（行为开关，如是否仅首次选卡重建焦点）
