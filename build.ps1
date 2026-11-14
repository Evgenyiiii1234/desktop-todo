<#
    一键编译 DesktopTodo（待办矩阵）
    使用 Windows 自带的 .NET Framework 编译器，不需要安装 Visual Studio 或 .NET SDK。

    用法：
        powershell -ExecutionPolicy Bypass -File .\build.ps1
#>
param(
    [switch]$Debug
)

$ErrorActionPreference = "Stop"
$root    = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcFile = Join-Path $root "src\TodoFloat.cs"
$icoFile = Join-Path $root "src\app.ico"
$outDir  = Join-Path $root "dist"
$outExe  = Join-Path $outDir "DesktopTodo.exe"

if (-not (Test-Path $srcFile)) { throw "找不到源码：$srcFile" }

# 1. 找 csc.exe
$cands = @(
    (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
    (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
)
$csc = $cands | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "找不到 .NET Framework 4.x 的 csc.exe（Windows 10/11 默认自带）。"
}

# 2. 编译
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$cscArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/codepage:65001",
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/out:$outExe"
)
if ($Debug) { $cscArgs += "/debug+"; $cscArgs += "/define:DEBUG" } else { $cscArgs += "/optimize+" }
if (Test-Path $icoFile) { $cscArgs += "/win32icon:$icoFile" }
$cscArgs += $srcFile

Write-Host "编译中…" -ForegroundColor Cyan
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "编译失败（csc 退出码 $LASTEXITCODE）" }

$size = [math]::Round((Get-Item $outExe).Length / 1KB, 1)
Write-Host "完成：$outExe  ($size KB)" -ForegroundColor Green
