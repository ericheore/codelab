# MOD 封装指南（Windows / PowerShell）

下面这套流程可以把补丁封装成可分发的 MOD 压缩包（zip）。

## 1) 准备目录
确保根目录有这些文件：

- `src/CampfireNavigationParityPatch.cs`（你的补丁代码）
- `mod/CampfireBugFix.json`（manifest）
- `CampfireBugFix.pck`（Godot 打包产物）
- `bin/Release/net9.0/CampfireBugFix.dll`（dotnet build 后生成）

## 2) 生成 pck
你需要先用 Godot 工具链生成 `CampfireBugFix.pck`。

## 3) 一键打包
执行：

```powershell
pwsh ./tools/package_mod.ps1 -ModId CampfireBugFix -Version 1.1.0
```

成功后得到：

- `dist/CampfireBugFix-1.1.0.zip`

zip 内应包含：

- `CampfireBugFix.dll`
- `CampfireBugFix.pck`
- `CampfireBugFix.json`

## 4) 安装到游戏
把 zip 解压后，把这 3 个文件按你的 MOD Loader 规范放到 mods 目录。

## 5) 常见问题
- 提示缺少 `.pck`：先执行 Godot 打包。
- 提示缺少 `.dll`：先执行 `dotnet build -c Release`。
- 版本号不对：修改 `-Version` 参数和 `mod/CampfireBugFix.json` 保持一致。

## 6) 商店删牌焦点支持（重点）
本补丁已把商店删牌常见界面类型加入候选。
如果你版本的类型名不同，请编辑 `mod/FocusPatchTargets.txt`，按“每行一个类型名”填入真实类名。

打包后 zip 内会包含 `FocusPatchTargets.txt`（若存在），便于你后续仅改配置不改代码。

> 零基础用户建议先看：`BEGINNER_ZERO_TO_ONE.md`。

## 7) 版本适配扩展（不改代码）
如果新版本改了类名/字段名，你可以直接修改 `mod/FocusPatch*.txt` 这些配置文件后重新打包。

> 完全零基础请先看：`ZERO_BASELINE_PACK_AND_DEBUG.md`。
