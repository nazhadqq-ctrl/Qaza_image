@echo off
chcp 65001 >nul
title سیستەمی Pic_Qaza - سێرڤەر و دیسکتۆپ
cd /d "%~dp0"

echo ========================================================
echo        سیستەمی Pic_Qaza - ویندۆز دیسکتۆپ
echo        Database: Pic_Qaza ^| Server: NAZHAD
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

:: دۆزینەوەی مایکرۆسۆفت ئێج لە هەردوو شوێنی پرۆگرام فایڵس
set "EDGE_EXE="
if exist "%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe" (
    set "EDGE_EXE=%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe"
) else if exist "%ProgramFiles%\Microsoft\Edge\Application\msedge.exe" (
    set "EDGE_EXE=%ProgramFiles%\Microsoft\Edge\Application\msedge.exe"
) else if exist "%LocalAppData%\Microsoft\Edge\Application\msedge.exe" (
    set "EDGE_EXE=%LocalAppData%\Microsoft\Edge\Application\msedge.exe"
)

echo [2/2] کردنەوەی پەنجەرەی بەرنامەی دیسکتۆپ...
if defined EDGE_EXE (
    start "" "%EDGE_EXE%" --app="http://localhost:4500" --window-size=1260,840
) else (
    start "" "http://localhost:4500"
)

echo.
echo ========================================================
echo  بەرنامەکە بە سەرکەوتوویی کرایەوە!
echo  ئەگەر پەنجەرەی بەرنامە نەکراوە، لە وێبگەڕ ئەمە بکەرەوە:
echo  http://localhost:4500  یان  http://127.0.0.1:4500
echo ========================================================
echo.
echo تکایە ئەم پەنجەرەی کۆنسۆڵە دامەخە تاوەکو بەرنامەکە کار دەکات.
node -e "setInterval(()=>{}, 1000)"
