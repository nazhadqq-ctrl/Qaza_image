# Staging and creating payload.zip
$sourceDir = "C:\Users\Nazha\.gemini\antigravity-ide\scratch\Pic_Qaza"
$stageDir = Join-Path $sourceDir "staging_build"
$zipPath = Join-Path $sourceDir "payload.zip"

if (Test-Path $stageDir) { Remove-Item -Recurse -Force $stageDir }
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

New-Item -ItemType Directory -Path $stageDir | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stageDir "public\uploads") | Out-Null

# Copy root files
$filesToCopy = @("server.js", "db.js", "package.json", "package-lock.json", "Launcher.cs", "Pic_Qaza.exe", "Start_Pic_Qaza.bat", "README.md")
foreach ($f in $filesToCopy) {
    Copy-Item (Join-Path $sourceDir $f) (Join-Path $stageDir $f)
}

# Copy public assets
Copy-Item (Join-Path $sourceDir "public\index.html") (Join-Path $stageDir "public\index.html")
Copy-Item (Join-Path $sourceDir "public\style.css") (Join-Path $stageDir "public\style.css")
Copy-Item (Join-Path $sourceDir "public\app.js") (Join-Path $stageDir "public\app.js")
Copy-Item (Join-Path $sourceDir "public\uploads\.gitkeep") (Join-Path $stageDir "public\uploads\.gitkeep") -ErrorAction SilentlyContinue

# Copy node_modules
Copy-Item -Recurse (Join-Path $sourceDir "node_modules") (Join-Path $stageDir "node_modules")

# Compress staging directory to payload.zip
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stageDir, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

# Clean up staging
Remove-Item -Recurse -Force $stageDir

Write-Output "Payload zip created successfully. Size: $((Get-Item $zipPath).Length / 1MB) MB"
