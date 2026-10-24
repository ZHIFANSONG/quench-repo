# Generates quench.ico (multi-size, PNG-compressed frames) and logo.png
Add-Type -AssemblyName System.Drawing

$outDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function New-Frame([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $pad = [double]$size * 0.02
    $r = [double]$size * 0.225
    $rect = New-Object System.Drawing.RectangleF($pad, $pad, ($size - 2 * $pad), ($size - 2 * $pad))
    $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $gp.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $gp.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $gp.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $gp.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $gp.CloseFigure()

    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 22, 27, 34),
        [System.Drawing.Color]::FromArgb(255, 12, 15, 20),
        45.0)
    $g.FillPath($bg, $gp)

    $penW = [Math]::Max(1.0, [double]$size * 0.035)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90, 45, 212, 191), [single]$penW)
    $g.DrawPath($pen, $gp)

    # power glyph: ring with a gap at top + stem
    $cx = $size / 2.0
    $cy = $size * 0.545
    $rad = $size * 0.235
    $ringPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 45, 212, 191), [single]([Math]::Max(1.2, $size * 0.075)))
    $ringPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $ringPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $rr = $rad * 2
    $g.DrawArc($ringPen, [single]($cx - $rad), [single]($cy - $rad), [single]$rr, [single]$rr, -60, 300)

    $stemPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 45, 212, 191), [single]([Math]::Max(1.2, $size * 0.075)))
    $stemPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $stemPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($stemPen, [single]$cx, [single]($cy - $rad * 1.42), [single]$cx, [single]($cy - $rad * 0.18))

    $ringPen.Dispose(); $stemPen.Dispose(); $pen.Dispose(); $bg.Dispose(); $gp.Dispose(); $g.Dispose()
    return $bmp
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = @()
foreach ($s in $sizes) {
    $bmp = New-Frame $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames += , @{ Size = $s; Bytes = $ms.ToArray() }
    if ($s -eq 256) { $bmp.Save((Join-Path $outDir 'logo.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose(); $ms.Dispose()
}

# Build ICO container (PNG payloads are valid for Vista+)
$icoPath = Join-Path $outDir 'quench.ico'
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)                 # reserved
$bw.Write([UInt16]1)                 # type = icon
$bw.Write([UInt16]$frames.Count)     # count
$offset = 6 + 16 * $frames.Count
foreach ($f in $frames) {
    $dim = if ($f.Size -ge 256) { 0 } else { $f.Size }
    $bw.Write([byte]$dim)            # width
    $bw.Write([byte]$dim)            # height
    $bw.Write([byte]0)               # palette
    $bw.Write([byte]0)               # reserved
    $bw.Write([UInt16]1)             # planes
    $bw.Write([UInt16]32)            # bpp
    $bw.Write([UInt32]$f.Bytes.Length)
    $bw.Write([UInt32]$offset)
    $offset += $f.Bytes.Length
}
foreach ($f in $frames) { $bw.Write($f.Bytes) }
$bw.Flush(); $bw.Dispose(); $fs.Dispose()

Write-Host ("ico  : {0} ({1} bytes)" -f $icoPath, (Get-Item $icoPath).Length)
Write-Host ("logo : {0}" -f (Join-Path $outDir 'logo.png'))
