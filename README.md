# MapleMeter-Warm

MapleMeter 暖光版：Windows x64 游戏经验统计工具，通过屏幕 OCR 识别经验与地图。

## 下载运行

从本仓库 Releases 下载 `MapleMeter-Warm-Windows-x64.zip`，完整解压后运行 `MapleMeter-Warm.exe`。请保留同目录下的 runtime、templates、fonts 等文件。

## 本次更新

- 主界面标题改为“预计每小时经验”，辅助说明为“按最近 5 分钟效率估算”；悬浮窗同步表述。
- 数值提示说明有效记录时间、暂停排除与不足 5 分钟的计算方式。
- 地图名称和区域中的中文使用当前中文字体，英文地图名保留 Inter。
- 经验统计计算逻辑未变。

## 构建

需要 Windows x64、Windows PowerShell 或 PowerShell，以及 .NET Framework 4.x（WPF）。

首次构建前，从 Releases 下载完整运行包，将其中的 `MapleMeter-Warm` 文件夹解压到 `release/` 下，提供 OCR 运行库、模板和字体。随后执行：

```powershell
.\build.ps1
```

输出：`release/MapleMeter-Warm/MapleMeter-Warm.exe`。

运行核心回归检查：

```powershell
Start-Process .\release\MapleMeter-Warm\MapleMeter-Warm.exe -ArgumentList '--self-test', 'core-results.txt' -Wait
Get-Content core-results.txt
```

`src/` 为源码；`assets/` 为界面素材和字体；`README.txt` 包含详细功能及使用说明。`tools/subset_fonts.py` 是原开发环境的字体制作配方，普通构建不需要重新生成字体。

## 数据与第三方组件

用户设置和记录保存在 `%LOCALAPPDATA%\MapleMeter-Warm`，不包含在仓库和发布包中。

第三方组件说明及许可证随完整运行包提供，字体许可证见 `assets/fonts/`。游戏视觉素材归相应权利人所有。