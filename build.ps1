$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /optimize+ /out:"$PSScriptRoot\PromptStudio.exe" @refs "$PSScriptRoot\App.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Host 'Built PromptStudio.exe'
