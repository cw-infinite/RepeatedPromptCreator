$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
[void][Reflection.Assembly]::LoadFrom("$PSScriptRoot\PromptStudio.exe")
$values = New-Object 'System.Collections.Generic.Dictionary[string,string]'
$values.Add('topic', 'Gardens $1 {{other}}')
$values.Add('source_text', "Line one`nLine two")
$actual = [PromptStudio.Engine]::Render('{{topic}} / {{ topic }} / {{source_text}} / {{missing}}', $values)
if ($actual -cne "Gardens `$1 {{other}} / Gardens `$1 {{other}} / Line one`nLine two / {{missing}}") { throw 'Substitution test failed' }
$names = [PromptStudio.Engine]::Names('{{ topic }} {{topic}} {{source_text}} {{invalid!}}')
if ($names.Count -ne 2) { throw 'Unique variable detection failed' }
$values['topic'] = '   '
if ([PromptStudio.Engine]::Render('{{topic}}',$values) -ne '{{topic}}') { throw 'Empty input preservation failed' }
$window = New-Object PromptStudio.MainWindow
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$timer = $window.GetType().GetField('saveTimer',$flags).GetValue($window)
$timer.Stop()
$state = $window.GetType().GetField('state',$flags).GetValue($window)
foreach ($dark in @($false,$true)) {
    $state.Dark = $dark
    [void]$window.GetType().GetMethod('ApplyTheme',$flags).Invoke($window,@())
    $window.Width = 1480
    $window.Height = 940
    $window.Content.Measure([Windows.Size]::new(1480,940))
    $window.Content.Arrange([Windows.Rect]::new(0,0,1480,940))
    $window.Content.UpdateLayout()
    $bitmap = New-Object Windows.Media.Imaging.RenderTargetBitmap(1480,940,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($window.Content)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $name = if ($dark) {'preview-dark.png'} else {'preview-light.png'}
    $stream = [IO.File]::Create((Join-Path $PSScriptRoot $name))
    $encoder.Save($stream)
    $stream.Dispose()
}
Write-Host 'PASS: repeated variables, whitespace, multiline text, literal replacement, missing values, unique names, light/dark window rendering.'

