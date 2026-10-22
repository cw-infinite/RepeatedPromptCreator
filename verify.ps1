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
if ([PromptStudio.Engine]::Clean('  ",;:Hello, world.;:"  ',63) -cne 'Hello, world') { throw 'Combined edge cleanup failed' }
if ([PromptStudio.Engine]::Clean('..Hello, world..',1) -cne 'Hello, world') { throw 'Period cleanup failed' }
if ([PromptStudio.Engine]::Clean(" `tHello world`r`n",32) -cne 'Hello world') { throw 'Whitespace cleanup failed' }
if ([PromptStudio.Engine]::Clean('.,;:',0) -cne '.,;:') { throw 'Cleanup must be opt-in' }
if ([PromptStudio.Engine]::Clean('.,;:',63) -cne '') { throw 'All-punctuation input failed' }
$cleanup = New-Object 'System.Collections.Generic.Dictionary[string,int]'
$cleanup.Add('topic',63)
$values['topic'] = '  "Hello, world."  '
if ([PromptStudio.Engine]::Render('Topic: {{topic}}. Again: {{topic}}!', $values, $cleanup) -cne 'Topic: Hello, world. Again: Hello, world!') { throw 'Per-variable cleanup or template preservation failed' }
if ($values['topic'] -cne '  "Hello, world."  ') { throw 'Original input was modified' }
Add-Type -AssemblyName System.Web.Extensions
$serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$item = New-Object PromptStudio.PromptItem
$item.Cleanup = $cleanup
$roundtrip = $serializer.Deserialize($serializer.Serialize($item),[PromptStudio.PromptItem])
if ($roundtrip.Cleanup['topic'] -ne 63) { throw 'Cleanup persistence failed' }
$legacy = $serializer.Deserialize('{"Name":"Old","Template":"{{topic}}","Values":{}}',[PromptStudio.PromptItem])
if ($null -eq $legacy.Cleanup) { throw 'Legacy workspace compatibility failed' }
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$timer = $window.GetType().GetField('saveTimer',$flags).GetValue($window)
$timer.Stop()
$state = $window.GetType().GetField('state',$flags).GetValue($window)
$current = $window.GetType().GetField('current',$flags).GetValue($window)
$firstName = [PromptStudio.Engine]::Names($current.Template) | Select-Object -First 1
if ($firstName) {
    $current.Cleanup[$firstName] = 63
    $window.GetType().GetField('loading',$flags).SetValue($window,$true)
    [void]$window.GetType().GetMethod('RebuildInputs',$flags).Invoke($window,@())
    $window.GetType().GetField('loading',$flags).SetValue($window,$false)
}
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
Write-Host 'PASS: substitution, edge cleanup, original/template preservation, persistence, legacy compatibility, light/dark window rendering.'

