# Build script for Pic_Qaza - Setup_Pic_Qaza.exe installer
$sourceDir = "C:\Users\Nazha\.gemini\antigravity-ide\scratch\Pic_Qaza"
$stageDir = Join-Path $sourceDir "staging_build"
$zipPath = Join-Path $sourceDir "payload.zip"
$installerExe = Join-Path $sourceDir "Setup_Pic_Qaza.exe"
$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

Write-Output "[1/6] Cleaning temp files..."
if (Test-Path $stageDir) { Remove-Item -Recurse -Force $stageDir }
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

Write-Output "[2/6] Compiling Launcher.cs -> Pic_Qaza.exe..."
$launcherIcon = "/win32icon:" + (Join-Path $sourceDir "app.ico")
$launcherOut = "/out:" + (Join-Path $sourceDir "Pic_Qaza.exe")
$launcherSrc = Join-Path $sourceDir "Launcher.cs"
& $cscPath /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll $launcherIcon $launcherOut $launcherSrc
if ($LASTEXITCODE -ne 0) {
    Write-Error "Compilation of Pic_Qaza.exe FAILED"
    exit 1
}

Write-Output "[3/6] Staging updated files..."
New-Item -ItemType Directory -Path $stageDir | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stageDir "public\uploads") | Out-Null

$filesToCopy = @(
    "server.js",
    "db.js",
    "app_config.json",
    "package.json",
    "package-lock.json",
    "Launcher.cs",
    "Pic_Qaza.exe",
    "Start_Pic_Qaza.bat",
    "README.md",
    "app.ico"
)
foreach ($f in $filesToCopy) {
    $src = Join-Path $sourceDir $f
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $stageDir $f)
    }
}

Copy-Item (Join-Path $sourceDir "public\index.html") (Join-Path $stageDir "public\index.html")
Copy-Item (Join-Path $sourceDir "public\style.css") (Join-Path $stageDir "public\style.css")
Copy-Item (Join-Path $sourceDir "public\app.js") (Join-Path $stageDir "public\app.js")
if (Test-Path (Join-Path $sourceDir "public\favicon.ico")) {
    Copy-Item (Join-Path $sourceDir "public\favicon.ico") (Join-Path $stageDir "public\favicon.ico")
}
if (Test-Path (Join-Path $sourceDir "public\uploads\.gitkeep")) {
    Copy-Item (Join-Path $sourceDir "public\uploads\.gitkeep") (Join-Path $stageDir "public\uploads\.gitkeep")
}

Write-Output "Copying node_modules..."
Copy-Item -Recurse (Join-Path $sourceDir "node_modules") (Join-Path $stageDir "node_modules")

Write-Output "Copying portable node.exe runtime..."
$nodePath = "C:\Program Files\nodejs\node.exe"
if (-not (Test-Path $nodePath)) {
    $nodeCmd = Get-Command node.exe -ErrorAction SilentlyContinue
    if ($nodeCmd) { $nodePath = $nodeCmd.Source }
}
if (Test-Path $nodePath) {
    Copy-Item $nodePath (Join-Path $stageDir "node.exe")
    Write-Output "Bundled portable node.exe into application package."
} else {
    Write-Warning "Could not find node.exe to bundle!"
}

Write-Output "[4/6] Creating payload.zip..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
[System.IO.Compression.ZipFile]::CreateFromDirectory($stageDir, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Remove-Item -Recurse -Force $stageDir

$zipSizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Output "payload.zip created: $zipSizeMB MB"

Write-Output "[5/6] Compiling Setup_Pic_Qaza.exe..."
$iconArg = "/win32icon:" + $sourceDir + "\app.ico"
$outArg = "/out:" + $installerExe
$resArg = "/resource:" + $zipPath + ",payload.zip"
$srcArg = $sourceDir + "\SetupWizard.cs"

& $cscPath /target:winexe $outArg $iconArg $resArg /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.FileSystem.dll /r:System.IO.Compression.dll $srcArg

if ($LASTEXITCODE -ne 0) {
    Write-Error "Compilation of Setup_Pic_Qaza.exe FAILED"
    exit 1
}

$exeSizeMB = [math]::Round((Get-Item $installerExe).Length / 1MB, 2)
Write-Output "Setup_Pic_Qaza.exe compiled successfully: $exeSizeMB MB"

Write-Output "[6/6] Copying installer to target locations..."
$qazaFolder = "C:\Users\Nazha\OneDrive\Desktop\qaza_image"
if (-not (Test-Path $qazaFolder)) {
    New-Item -ItemType Directory -Path $qazaFolder | Out-Null
}
Copy-Item $installerExe (Join-Path $qazaFolder "Setup_Pic_Qaza.exe") -Force
Write-Output "Copied to: $qazaFolder\Setup_Pic_Qaza.exe"

$desktop1 = "C:\Users\Nazha\OneDrive\Desktop"
$desktop2 = "C:\Users\Nazha\Desktop"

if (Test-Path $desktop1) {
    Copy-Item $installerExe (Join-Path $desktop1 "Setup_Pic_Qaza.exe") -Force
    Write-Output "Copied to: $desktop1\Setup_Pic_Qaza.exe"
}
if ((Test-Path $desktop2) -and ($desktop2 -ne $desktop1)) {
    Copy-Item $installerExe (Join-Path $desktop2 "Setup_Pic_Qaza.exe") -Force
    Write-Output "Copied to: $desktop2\Setup_Pic_Qaza.exe"
}

# Clean up zip
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

Write-Output "===================================================="
Write-Output "  Setup_Pic_Qaza.exe build COMPLETE!"
Write-Output "===================================================="
