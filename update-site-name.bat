@echo off
cd SiteNameUtility
echo Building Update Site Name Utility...
dotnet build UpdateSiteName.csproj -o bin

if %ERRORLEVEL% NEQ 0 (
    echo Build failed. Press any key to exit...
    pause >nul
    exit /b %ERRORLEVEL%
)

echo.
echo Running Update Site Name Utility...
echo.
bin\UpdateSiteName.exe

echo.
echo Done. Press any key to exit...
pause >nul 