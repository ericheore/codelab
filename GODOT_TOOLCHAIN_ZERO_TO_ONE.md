# Godot 工具链从零开始（小白版）

你可以把它理解成两步：

1. 安装 Godot（.NET 版）
2. 用 Godot 生成 `CampfireBugFix.pck`

`package_mod.ps1` 只负责最后打包 zip，**不会替你生成 `.pck`**。

---

## 0) 先确认你现在缺什么
在项目目录终端执行：

```powershell
cd /workspace/codelab
Test-Path ./CampfireBugFix.pck
```

- 返回 `True`：说明 pck 已存在，可直接走打包。
- 返回 `False`：说明你需要先做 Godot 导出。

---

## 1) 安装 Godot（.NET 版）

> 你必须装 **Godot .NET（Mono）版**，不是普通版。

安装后检查：

```powershell
godot --version
```

如果提示找不到命令，你可以：
- 把 Godot 安装目录加到系统 PATH；或
- 直接用 Godot 可执行文件的完整路径运行（例如 `C:\Tools\Godot\Godot_v4.x_mono.exe`）。

---

## 2) 用 Godot 打出 .pck

### 方式 A（有现成导出预设时，推荐）
在 Godot 工程根目录执行（目录里应有 `project.godot`）：

```powershell
godot --headless --export-pack "Windows Desktop" CampfireBugFix.pck
```

如果你的预设名不是 `Windows Desktop`，请改成你项目里的实际导出预设名。

### 方式 B（项目自带脚本）
如果项目里有 Godot 打包脚本（例如 `tools/build_pck.gd`），就按项目文档执行对应命令。

---

## 3) 验证 .pck 真的生成了

```powershell
Test-Path ./CampfireBugFix.pck
```

返回 `True` 后，再执行最终打包：

```powershell
pwsh ./tools/package_mod.ps1 -ModId CampfireBugFix -Version 1.1.0
```

---

## 4) 常见报错（小白定位）

### 报错 A：`godot` 不是内部或外部命令
说明 PATH 没配好。先用 Godot exe 的完整路径试。

### 报错 B：找不到 `project.godot`
说明你不在 Godot 工程目录，请 `cd` 到正确目录再执行导出。

### 报错 C：导出预设不存在
说明预设名不对。打开 Godot 编辑器看 Export 里的名字，复制粘贴到命令里。

---

## 5) 你发我这三样，我就能继续帮你
1. `godot --version` 输出
2. 你执行导出命令的完整输出
3. `Test-Path ./CampfireBugFix.pck` 的结果
