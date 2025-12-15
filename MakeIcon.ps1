param(
  [string]$OutPath = 'assets\\finanzas.ico'
)

New-Item -ItemType Directory -Path (Split-Path -Parent $OutPath) -Force | Out-Null

Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 64,64
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$rect = New-Object System.Drawing.Rectangle 0,0,64,64
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(59,130,246)), ([System.Drawing.Color]::FromArgb(16,185,129)), 45.0
$g.FillEllipse($brush,0,0,64,64)
$font = New-Object System.Drawing.Font -ArgumentList 'Segoe UI', 32, 'Bold', 'Pixel'
$size = $g.MeasureString('$',$font)
$g.DrawString('$',$font,[System.Drawing.Brushes]::White,(64 - $size.Width)/2,(64 - $size.Height)/2 - 2)
$g.Dispose(); $brush.Dispose();

$hicon = $bmp.GetHicon()
$ico = [System.Drawing.Icon]::FromHandle($hicon)
$fs = [System.IO.File]::Open($OutPath,[System.IO.FileMode]::Create)
$ico.Save($fs)
$fs.Close(); $bmp.Dispose();
Write-Host "Icono creado en $OutPath"
