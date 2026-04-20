# 零基础：从打包到调试（一步一步照做）

你不需要懂代码，照着做就行。

---

## A. 先准备（只做一次）

### 1) 安装软件
你需要两个工具：

1. **.NET SDK 9**（用来编译 dll）
2. **PowerShell 7 (`pwsh`)**（用来运行脚本）

安装后，打开终端输入：

```bash
dotnet --version
pwsh --version
```

如果能看到版本号，就说明安装好了。

---

## B. 打包（每次改完配置后都这样做）

### 1) 进入项目目录

```bash
cd /workspace/codelab
```

### 2) 运行一键打包

```bash
pwsh ./tools/package_mod.ps1 -ModId CampfireBugFix -Version 1.1.0
```

### 3) 看成功标志
你看到类似下面的文字就算成功：

- `[6/6] 完成: dist/CampfireBugFix-1.1.0.zip`

### 4) 如果失败，先看这三类常见报错
1. `缺少 .pck`：先生成 `CampfireBugFix.pck`
2. `缺少 .dll`：确认 `dotnet build -c Release` 可成功
3. `manifest 路径` 报错：确认 `mod/CampfireBugFix.json` 在项目里

---

## C. 安装到游戏

1. 打开 `dist/` 目录
2. 找到 `CampfireBugFix-1.1.0.zip`
3. 解压
4. 把解压出的文件按你的 mod loader 规范放到 mods 目录

---

## D. 你只需要做的“测试动作”（零基础版）

进游戏后，按下面顺序测：

1. 进商店删牌界面
2. 选一张牌
3. 按方向键（↑↓←→）
4. 看焦点是否能重新跳到牌组

然后重复 2~4 多次。

---

## E. 出问题时，你只要给我这 4 样东西

### 1) 你刚才运行打包命令的完整终端输出
就是从你输入命令开始到结束的全部文字。

### 2) 游戏日志里包含这个关键词的行
关键词：

```txt
[CampfireNavigationParityPatch]
```

### 3) 你的配置文件
把 `mod/FocusPatch*.txt` 全部发我。

### 4) 复现步骤
用一句话写：

> 我在 xxx 界面，先点了 xx，再按 xx，预期是 xx，实际是 xx

---

## F. 一键收集调试包（推荐，最省事）

你可以直接运行：

```bash
pwsh ./tools/collect_debug_bundle.ps1 -Version 1.1.0 -GameLogPath "<你的游戏日志路径>"
```

执行后会生成：

- `debug_bundle/debug_bundle_*.zip`

把这个 zip 发我就行，我会自己看。
