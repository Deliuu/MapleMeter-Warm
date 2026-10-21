# 开发与构建说明

## 从源码构建

需要 Windows x64、PowerShell 和 .NET Framework 4.x 的 C# 编译器及 WPF 引用。

1. 克隆本仓库。
2. 下载完整 Release ZIP，将其中的 `MapleMeter-Warm` 文件夹解压到仓库的 `release/` 下，用于提供运行依赖。
3. 在仓库根目录执行：

```powershell
.\build.ps1
```

输出为 `release/MapleMeter-Warm/MapleMeter-Warm.exe`。普通构建不需要重新生成字体；`tools/subset_fonts.py` 仅作为字体制作配方保留，依赖原始字体输入和 fontTools。

核心逻辑自检：

```powershell
Start-Process .\release\MapleMeter-Warm\MapleMeter-Warm.exe -ArgumentList '--self-test', 'core-results.txt' -Wait
Get-Content .\core-results.txt
```

完整发布包使用 `tools/package.ps1` 按明确的文件清单打包，避免误带截图、日志和用户记录。

