@echo off
chcp 65001 >nul
title سیستەمی Pic_Qaza - سێرڤەر و دیسکتۆپ
cd /d "%~dp0"

echo ========================================================
echo        سیستەمی Pic_Qaza - ویندۆز دیسکتۆپ
echo        Database: Pic_Qaza ^| Server: 192.168.3.1
echo ========================================================
echo.
echo [1/2] دەستپێکردنی سێرڤەری بەرنامە لەسەر پۆرت 4500...

:: پشکنینی ئەوەی کە ئایا پۆرت 4500 کار دەکات
netstat -ano | findstr :4500 >nul
if %errorlevel% neq 0 (
    start /b "" node server.js
    echo چاوەڕوانی ئامادەبوونی پەیوەندی بە SQL Server...
    timeout /t 3 /nobreak >nul
) else (
    echo سێرڤەری بەرنامە پێشتر چالاک کراوە.
)

:: دۆزینەوەی مایکرۆسۆفت ئێج لە شوێنە جیاوازەکان
set "EDGE_EXE="
if exist "%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe" (
    set "EDGE_EXE=%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe"
) else if exist "%ProgramFiles%\Microsoft\Edge\Application\msedge.exe" (
    set "EDGE_EXE=%ProgramFiles%\Microsoft\Edge\Application\msedge.exe"
) else if exist "%LocalAppData%\Microsoft\Edge\Application\msedge.exe" (
    set "EDGE_EXE=%LocalAppData%\Microsoft\Edge\Application\msedge.exe"
)

:: دروستکردنی فۆڵدەری داتای جیاواز تا وەک ئەپی سەربەخۆی دیسکتۆپ کاربکات بەبێ ناونیشانی وێبگەڕ و تابلێت
set "APP_DATA_DIR=%LOCALAPPDATA%\Pic_Qaza\AppData"
if not exist "%APP_DATA_DIR%" mkdir "%APP_DATA_DIR%"

echo [2/2] کردنەوەی پەنجەرەی ئەپی دیسکتۆپ...
if defined EDGE_EXE (
    start "" "%EDGE_EXE%" --app="http://127.0.0.1:4500" --user-data-dir="%APP_DATA_DIR%" --app-id=Pic_Qaza --window-size=1300,860 --window-position=center --no-first-run --no-default-browser-check --disable-features=Translate,OptimizationHints,MediaRouter --disable-extensions
) else (
    start "" "http://127.0.0.1:4500"
)

echo.
echo ========================================================
echo  بەرنامەکە وەک پەنجەرەی دیسکتۆپ بە سەرکەوتوویی کرایەوە!
echo ========================================================
echo.
echo تکایە ئەم پەنجەرەی کۆنسۆڵە دامەخە تاوەکو بەرنامەکە کار دەکات.
node -e "setInterval(()=>{}, 1000)"
