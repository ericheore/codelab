# 完整从零开始（不是只有 Godot）

你要的是完整流程，我给你一条龙：

1. 安装基础工具
2. 准备源码
3. 生成 `.pck`（Godot）
4. 生成 `.dll`（dotnet）
5. 打包 zip
6. 安装到游戏
7. 游戏内验证
8. 出错后一键收集调试包

---

## 0) 你电脑里要有这些东西

### 必装
- Git
- .NET SDK 9
- PowerShell 7 (`pwsh`)
- Godot **.NET/Mono** 版

### 选装（看你使用的模组加载器）
- 对应游戏的 Mod Loader

---

## 1) 检查工具是否安装成功
打开终端执行：

```bash
git --version
dotnet --version
pwsh --version
godot --version
```

只要每条都显示版本号，就可以继续。

---

## 2) 下载源码并进入目录

```bash
git clone <你的仓库地址> codelab
cd codelab
```

你现在应该能看到：
- `mod/`
- `src/`
- `tools/`
- 多个 `.md` 文档

---

## 3) 先生成 `.pck`（Godot 步骤）

### 3.1 先判断是否已经有 pck
```bash
Test-Path ./CampfireBugFix.pck
```

- `True`：跳到第 4 步
- `False`：继续 3.2

### 3.2 在 Godot 工程目录导出
（目录里需要有 `project.godot`）

```bash
godot --headless --export-pack "Windows Desktop" CampfireBugFix.pck
```

### 3.3 再次确认
```bash
Test-Path ./CampfireBugFix.pck
```

---

## 4) 生成 `.dll`（dotnet 步骤）

```bash
dotnet build -c Release
```

成功后应有类似文件：
- `bin/Release/net9.0/CampfireBugFix.dll`

---

## 5) 一键打包最终 zip

```bash
pwsh ./tools/package_mod.ps1 -ModId CampfireBugFix -Version 1.1.0
```

成功标志：
- `[6/6] 完成: dist/CampfireBugFix-1.1.0.zip`

---

## 6) 安装到游戏
1. 打开 `dist/`
2. 找到 `CampfireBugFix-1.1.0.zip`
3. 解压
4. 按你的 Mod Loader 规则放入 mods 目录

---

## 7) 进游戏验证
建议最小验证：

1. 进商店删牌界面
2. 选中一张牌
3. 按方向键
4. 看焦点是否回到牌组
5. 连续重复几次

---

## 8) 出错时你只要做一条命令

```bash
pwsh ./tools/collect_debug_bundle.ps1 -Version 1.1.0 -GameLogPath "你的游戏日志路径"
```

执行后会生成：
- `debug_bundle/debug_bundle_*.zip`

把这个 zip 发我，我来继续定位。

---

## 9) 常见卡点（零基础最常见）

### 卡点 A：`godot` 命令不存在
- 说明 Godot 没加 PATH
- 先用 Godot exe 的完整路径执行

### 卡点 B：找不到 `project.godot`
- 你不在 Godot 工程目录
- 切到正确目录再导出

### 卡点 C：`缺少 .pck`
- 你直接打包了，但还没做 Godot 导出

### 卡点 D：`缺少 .dll`
- 先执行 `dotnet build -c Release`

### 卡点 E：进游戏没效果
- 先检查 `mod/FocusPatch*.txt` 是否被打进 zip
- 再把 debug bundle 发我
