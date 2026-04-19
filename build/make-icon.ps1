# Generates app.ico for BreakReminder. Draws a mug + steam on a blue rounded square.
# Renders multiple sizes (16, 32, 48, 64, 128, 256) into one multi-image .ico.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$outIco = Join-Path $PSScriptRoot '..\app.ico' | Resolve-Path -Relative -ErrorAction SilentlyContinue
if (-not $outIco) { $outIco = Join-Path $PSScriptRoot '..\app.ico' }

$sizes = 16, 32, 48, 64, 128, 256
$pngStreams = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.PixelOffsetMode = 'HighQuality'

    # Rounded-square background.
    $radius = [int]($size * 0.18)
    $bgRect = New-Object System.Drawing.RectangleF(0, 0, $size, $size)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($bgRect.X, $bgRect.Y, $d, $d, 180, 90)
    $path.AddArc($bgRect.Right - $d, $bgRect.Y, $d, $d, 270, 90)
    $path.AddArc($bgRect.Right - $d, $bgRect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($bgRect.X, $bgRect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $bgRect,
        [System.Drawing.Color]::FromArgb(255, 59, 130, 246),
        [System.Drawing.Color]::FromArgb(255, 29, 78, 216),
        90.0)
    $g.FillPath($bgBrush, $path)
    $bgBrush.Dispose()
    $path.Dispose()

    # Mug body (rounded rectangle, white).
    $mugX = [int]($size * 0.22)
    $mugY = [int]($size * 0.38)
    $mugW = [int]($size * 0.44)
    $mugH = [int]($size * 0.40)
    $mugR = [int]($size * 0.06)

    $mugPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $md = $mugR * 2
    $mugPath.AddArc($mugX, $mugY, $md, $md, 180, 90)
    $mugPath.AddArc($mugX + $mugW - $md, $mugY, $md, $md, 270, 90)
    $mugPath.AddArc($mugX + $mugW - $md, $mugY + $mugH - $md, $md, $md, 0, 90)
    $mugPath.AddArc($mugX, $mugY + $mugH - $md, $md, $md, 90, 90)
    $mugPath.CloseFigure()

    $mugBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillPath($mugBrush, $mugPath)
    $mugBrush.Dispose()
    $mugPath.Dispose()

    # Mug handle (ring on the right).
    $handleThickness = [Math]::Max(1, [int]($size * 0.05))
    $handleX = $mugX + $mugW - [int]($size * 0.02)
    $handleY = $mugY + [int]($mugH * 0.20)
    $handleW = [int]($size * 0.16)
    $handleH = [int]($mugH * 0.55)
    $handlePen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, $handleThickness)
    $handlePen.StartCap = 'Round'
    $handlePen.EndCap = 'Round'
    $g.DrawArc($handlePen, $handleX, $handleY, $handleW, $handleH, -90, 180)
    $handlePen.Dispose()

    # Steam lines (three wavy curves above mug).
    if ($size -ge 32) {
        $steamPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [Math]::Max(1, [int]($size * 0.035)))
        $steamPen.StartCap = 'Round'
        $steamPen.EndCap = 'Round'
        $baseX = $mugX + [int]($mugW * 0.15)
        $stepX = [int]($mugW * 0.25)
        $topY = [int]($size * 0.10)
        $botY = $mugY - [int]($size * 0.04)
        $amp = [Math]::Max(1, [int]($size * 0.04))
        for ($i = 0; $i -lt 3; $i++) {
            $x = $baseX + $i * $stepX
            $pts = [System.Drawing.Point[]]@(
                (New-Object System.Drawing.Point($x, $botY))
                (New-Object System.Drawing.Point(($x + $amp), ($botY - [int](($botY - $topY) * 0.35))))
                (New-Object System.Drawing.Point(($x - $amp), ($botY - [int](($botY - $topY) * 0.70))))
                (New-Object System.Drawing.Point($x, $topY))
            )
            $g.DrawCurve($steamPen, $pts, 0.5)
        }
        $steamPen.Dispose()
    }

    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pngStreams += ,$ms
}

# Assemble ICO file (ICONDIR + ICONDIRENTRY x n + PNG data blobs).
$ico = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ico)

$bw.Write([uint16]0)            # reserved
$bw.Write([uint16]1)            # type = icon
$bw.Write([uint16]$sizes.Count) # image count

$headerSize = 6 + 16 * $sizes.Count
$offset = $headerSize

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $dataLen = [int]$pngStreams[$i].Length
    $w = if ($size -ge 256) { 0 } else { $size }
    $h = if ($size -ge 256) { 0 } else { $size }

    $bw.Write([byte]$w)
    $bw.Write([byte]$h)
    $bw.Write([byte]0)      # palette
    $bw.Write([byte]0)      # reserved
    $bw.Write([uint16]1)    # color planes
    $bw.Write([uint16]32)   # bits per pixel
    $bw.Write([uint32]$dataLen)
    $bw.Write([uint32]$offset)
    $offset += $dataLen
}

foreach ($ms in $pngStreams) {
    $bw.Write($ms.ToArray())
    $ms.Dispose()
}

$bw.Flush()
[System.IO.File]::WriteAllBytes($outIco, $ico.ToArray())
$bw.Dispose()
$ico.Dispose()

Write-Host "Wrote $outIco ($((Get-Item $outIco).Length) bytes)"
