Add-Type -AssemblyName System.Drawing
$taskBitmap = New-Object System.Drawing.Bitmap 64,64
$taskGraphics = [System.Drawing.Graphics]::FromImage($taskBitmap)
$taskGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$taskGraphics.Clear([System.Drawing.Color]::FromArgb(16,25,36))
$taskPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(106,218,231)),4
$taskGraphics.DrawArc($taskPen,7,7,50,50,35,290)
$taskPen.Width = 5
$taskGraphics.DrawLines($taskPen,[System.Drawing.Point[]]@([System.Drawing.Point]::new(23,44),[System.Drawing.Point]::new(23,21),[System.Drawing.Point]::new(41,43),[System.Drawing.Point]::new(41,20)))
$taskIcon = [System.Drawing.Icon]::FromHandle($taskBitmap.GetHicon())
$taskStream = [System.IO.File]::Create((Join-Path (Split-Path -Parent $PSScriptRoot) 'assets\nova.ico'))
$taskIcon.Save($taskStream)
$taskStream.Dispose()
$taskIcon.Dispose()
$taskPen.Dispose()
$taskGraphics.Dispose()
$taskBitmap.Dispose()
