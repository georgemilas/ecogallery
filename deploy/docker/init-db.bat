@echo off
setlocal enabledelayedexpansion

echo ==================================
echo EcoGallery Database Initialization
echo ==================================
echo.

REM Load .env file if it exists
if exist .env (
    echo Loading environment from .env file...
    for /f "usebackq tokens=1,* delims==" %%a in (".env") do (
        set "line=%%a"
        REM Skip comments and empty lines
        if not "!line:~0,1!"=="#" if not "!line!"=="" (
            set "%%a=%%b"
        )
    )
    echo.
) else (
    echo ❌ ERROR: .env file not found!
    echo Please copy .env.example to .env and configure it
    pause
    exit /b 1
)

REM Validate required environment variables
if "%API_KEY%"=="CHANGE_ME_TO_A_SECURE_RANDOM_STRING" (
    echo ❌ ERROR: API_KEY is not set or still using placeholder value!
    echo Please edit .env file and set a secure API_KEY
    pause
    exit /b 1
)

if "%POSTGRES_PASSWORD%"=="CHANGE_ME_TO_SECURE_DB_PASSWORD" (
    echo ❌ ERROR: POSTGRES_PASSWORD is not set or still using placeholder value!
    echo Please edit .env file and set a secure POSTGRES_PASSWORD
    pause
    exit /b 1
)

if "%ADMIN_PASSWORD%"=="CHANGE_ME_TO_SECURE_ADMIN_PASSWORD" (
    echo ❌ ERROR: ADMIN_PASSWORD is not set or still using placeholder value!
    echo Please edit .env file and set a secure ADMIN_PASSWORD
    pause
    exit /b 1
)

if "%API_KEY%"=="" (
    echo ❌ ERROR: API_KEY is not set!
    echo Please edit .env file and set a secure API_KEY
    pause
    exit /b 1
)

if "%POSTGRES_PASSWORD%"=="" (
    echo ❌ ERROR: POSTGRES_PASSWORD is not set!
    echo Please edit .env file and set a secure POSTGRES_PASSWORD
    pause
    exit /b 1
)

if "%ADMIN_PASSWORD%"=="" (
    echo ❌ ERROR: ADMIN_PASSWORD is not set!
    echo Please edit .env file and set a secure ADMIN_PASSWORD
    pause
    exit /b 1
)

if "%PICTURES_PATH%"=="/path/to/your/pictures" (
    echo ❌ ERROR: PICTURES_PATH is not set or still using placeholder value!
    echo Please edit .env file and set the path to your pictures directory
    pause
    exit /b 1
)

if "%PICTURES_PATH%"=="" (
    echo ❌ ERROR: PICTURES_PATH is not set!
    echo Please edit .env file and set the path to your pictures directory
    pause
    exit /b 1
)

echo ✅ Configuration validated
echo.
REM Wait for PostgreSQL to be ready (already handled by healthcheck, but double-check)
echo Waiting for PostgreSQL to be ready...
timeout /t 2 /nobreak > nul

REM Create database schema with custom admin password
echo Creating database schema...
echo Using admin password from ADMIN_PASSWORD environment variable
docker-compose run --rm service create-db -pw "%ADMIN_PASSWORD%"

if errorlevel 1 (
    echo.
    echo ❌ Database initialization failed!
    pause
    exit /b 1
)

echo.
echo ✅ Database schema created successfully!
echo.
echo ==================================
echo Admin User Created:
echo ==================================
echo Username: admin
echo Password: ^(your custom ADMIN_PASSWORD^)
echo.
echo ==================================
echo Next Steps:
echo ==================================
echo 1. Sync your pictures:
echo    docker-compose run --rm service dotnet GalleryService.dll sync /pictures
echo.
echo 2. Access the application at http://localhost
echo ==================================
echo.
pause
