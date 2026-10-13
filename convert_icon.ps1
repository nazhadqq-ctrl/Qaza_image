Add-Type -AssemblyName System.Drawing

$inputPath = "C:\Users\Nazha\.gemini\antigravity-ide\brain\d925d948-2186-4c67-b0a9-aa18fb3a5136\app_icon_design_1791920039138.jpg"
$outputPath = "C:\Users\Nazha\.gemini\antigravity-ide\scratch\Pic_Qaza\app.ico"

$src = [System.Drawing.Image]::FromFile($inputPath)

# Create 256x256 high-resolution bitmap
$bmp = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.DrawImage($src, 0, 0, 256, 256)

$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)

$fs = [System.IO.File]::Create($outputPath)
$icon.Save($fs)
$fs.Close()

$g.Dispose()
$bmp.Dispose()
$src.Dispose()

Write-Output "app.ico generated successfully at: $outputPath"
