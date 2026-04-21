# 如何拓展“商店删牌多选”能力（基于当前补丁）

> 这是基于现有 GitHub 项目思路做的工程化拓展说明：核心是每次选卡后都重新种焦点关系并释放焦点，让方向键继续可导航。

## 1) 关键开关
编辑 `mod/FocusPatchBehavior.txt`：

```txt
reseed_only_first_selection=false
```

含义：
- `true`：只在第一次选卡后重建焦点（更保守）
- `false`：每次选卡后都重建焦点（更接近商店多选复现需求）

## 2) 商店界面类型适配
如果日志提示找不到商店类型，请编辑 `mod/FocusPatchTargets.txt`，把你版本里的真实 shop purge screen 类型名加进去。

## 3) 点击方法适配
如果日志提示缺少点击方法，请编辑 `mod/FocusPatchClickMethods.txt`，补充实际方法名。

## 4) 为什么这能拓展商店场景
在商店删牌界面，问题通常不是“不能选牌”，而是“选完后一按方向键焦点链断了”。
这个补丁在每次选卡后都执行：
1. 找到 grid/cardHolder 并重建邻接；
2. 刷新 `ActiveScreenContext`；
3. 调用 `GuiReleaseFocus` 让方向键重新接管。

所以当 `reseed_only_first_selection=false` 时，第二张、第三张之后也会继续重建导航链，提升连续操作成功率。

## 5) 你需要知道的边界
- 如果游戏逻辑强制“最多只能删 1 张”，那需要额外 patch 业务限制（不是焦点补丁本身能完全解决的）。
- 如果版本把字段/属性重命名，需要通过 `FocusPatch*.txt` 配置补齐候选。
