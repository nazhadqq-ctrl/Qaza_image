$targetDir = "C:\Users\Nazha\OneDrive\Desktop\qaza_image"
$targetExe = Join-Path $targetDir "Pic_Qaza.exe"
$iconPath = Join-Path $targetDir "app.ico"

# 1. Place shortcut in User Pinned TaskBar folder
$taskbarFolder = Join-Path $env:APPDATA "Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"
if (Test-Path $taskbarFolder) {
    $shortcutPath = Join-Path $taskbarFolder "Pic_Qaza.lnk"
    $ws = New-Object -ComObject WScript.Shell
    $shortcut = $ws.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $targetExe
    $shortcut.WorkingDirectory = $targetDir
    $shortcut.IconLocation = "$iconPath,0"
    $shortcut.Description = "سیستەمی تۆمارکردنی وێنەی پسووڵە - Pic_Qaza"
    $shortcut.Save()
    Write-Output "Copied shortcut to User Pinned TaskBar folder: $shortcutPath"
}

# 2. Try Shell.Application Verbs pinning
try {
    $shell = New-Object -ComObject Shell.Application
    $folder = $shell.Namespace($targetDir)
    $item = $folder.ParseName("Pic_Qaza.exe")
    $verbs = $item.Verbs()
    $found = $false
    foreach ($v in $verbs) {
        $name = $v.Name.Replace("&", "")
        if ($name -match "taskbar|تاسک|شريط|pin" -and $name -notmatch "unpin") {
            $v.DoIt()
            $found = $true
            Write-Output "Triggered verb: $($v.Name)"
            break
        }
    }
    if (-not $found) {
        Write-Output "Available verbs: $(($verbs | ForEach-Object { $_.Name }) -join ' | ')"
    }
} catch {
    Write-Output "Verb pinning note: $($_.Exception.Message)"
}
