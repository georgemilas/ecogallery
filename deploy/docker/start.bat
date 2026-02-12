@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

if not exist .env (
    echo ERROR: .env file not found!
    echo Run setup.bat first to initialize the application.
    pause
    exit /b 1
)

REM Generate a new API_KEY for this session
for /f "delims=" %%i in ('powershell -Command "[guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')"') do set "NEW_API_KEY=%%i"
powershell -Command "(Get-Content .env) -replace '^API_KEY=.*', 'API_KEY=!NEW_API_KEY!' | Set-Content .env"

REM Load HTTP_PORT from .env for display
for /f "usebackq tokens=1,* delims==" %%a in (".env") do (
    set "line=%%a"
    if not "!line:~0,1!"=="#" if not "!line!"=="" (
        set "%%a=%%b"
    )
)

REM Start the application in detached mode
docker compose up -d

REM Show status
docker compose ps
echo ======================================
echo   EcoGallery is running!
echo ======================================

if "%HTTP_PORT%"=="" (
    echo Access the application at: http://localhost
) else if "%HTTP_PORT%"=="80" (
    echo Access the application at: http://localhost
) else (
    echo Access the application at: http://localhost:%HTTP_PORT%
)
echo Useful commands:
echo   docker compose logs -f          # View logs
echo   docker compose down             # Stop ecogallery
echo   docker compose ps               # Check status
echo ======================================
pause