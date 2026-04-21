# 你下载完这些文件后，下一步做什么（超简版）

> 你现在看到 `mod/`、`src/`、`tools/` 和一堆 `.md`，说明源码下载成功了。

## 1) 先打开终端到这个文件夹
在文件夹空白处右键，打开终端（PowerShell）。

## 2) 先确认工具装好了
执行：

```powershell
dotnet --version
pwsh --version
```

如果两条都能显示版本号，继续下一步。

## 3) 一键打包（最关键）
执行：

```powershell
pwsh ./tools/package_mod.ps1 -ModId CampfireBugFix -Version 1.1.0
```

成功标志（看到这行就成功）：

```text
[6/6] 完成: dist/CampfireBugFix-1.1.0.zip
```

## 4) 把 mod 装进游戏
- 打开 `dist` 文件夹
- 找到 `CampfireBugFix-1.1.0.zip`
- 解压后按你的 mod loader 规则放到 mods 目录

## 5) 进游戏测试
- 进入商店删牌界面
- 选一张牌后按方向键
- 观察焦点能否回到牌组

## 6) 失败就一键收集调试包给我
```powershell
pwsh ./tools/collect_debug_bundle.ps1 -Version 1.1.0 -GameLogPath "你的游戏日志路径"
```

执行后会得到：
- `debug_bundle/debug_bundle_*.zip`

把这个 zip 发我，我来继续帮你处理。

> 还没生成 `.pck` 的话，请先看：`GODOT_TOOLCHAIN_ZERO_TO_ONE.md`。

> 如果你要的是完整从零到可用，请先看：`COMPLETE_ZERO_TO_ONE_FULL.md`。
