Add-Type -AssemblyName System.Drawing

$inputPath = "C:\Users\Nazha\.gemini\antigravity-ide\brain\d925d948-2186-4c67-b0a9-aa18fb3a5136\app_icon_design_1791920039138.jpg"
$outputPath = "C:\Users\Nazha\.gemini\antigravity-ide\scratch\Pic_Qaza\app.ico"

$src = [System.Drawing.Image]::FromFile($inputPath)
$sizes = @(256, 128, 64, 48, 32, 16)

$pngStreams = @()

foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $sz, $sz
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($src, 0, 0, $sz, $sz)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pngStreams += $ms
}

$src.Dispose()

# Create ICO file with standard PNG frames
$fs = [System.IO.File]::Create($outputPath)
$bw = New-Object System.IO.BinaryWriter $fs

# Header: 0, 1 (icon), count
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$sizes.Count)

# Calculate initial offset for image data: 6 (header) + count * 16 (entries)
$offset = 6 + ($sizes.Count * 16)

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]
    $dataLen = $pngStreams[$i].Length

    $w = if ($sz -ge 256) { 0 } else { [byte]$sz }
    $h = if ($sz -ge 256) { 0 } else { [byte]$sz }

    $bw.Write([byte]$w)
    $bw.Write([byte]$h)
    $bw.Write([byte]0)   # ColorCount
    $bw.Write([byte]0)   # Reserved
    $bw.Write([uint16]1) # Planes
    $bw.Write([uint16]32) # BitCount
    $bw.Write([uint32]$dataLen)
    $bw.Write([uint32]$offset)

    $offset += $dataLen
}

# Write PNG payloads
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $bytes = $pngStreams[$i].ToArray()
    $bw.Write($bytes)
    $pngStreams[$i].Dispose()
}

$bw.Close()
$fs.Close()

Write-Output "Multi-resolution Windows ICO created: $((Get-Item $outputPath).Length) bytes"
