@echo off
setlocal enabledelayedexpansion

echo ======================================
echo   EcoGallery - First Time Setup
echo ======================================
echo.
echo This setup uses pre-built Docker images.
echo No source code or build tools required.
echo.

cd /d "%~dp0"

if not exist docker-compose.yml (
    echo ERROR: docker-compose.yml not found!
    echo Download it from the EcoGallery releases page.
    pause
    exit /b 1
)

REM ---- .env file setup ----
if not exist .env (
    if not exist .env.example (
        echo ERROR: .env.example not found!
        echo Download it from the EcoGallery releases page.
        pause
        exit /b 1
    )
    echo No .env file found. Creating from .env.example...
    copy .env.example .env > nul
)

REM Check if API_KEY needs to be generated
for /f "tokens=2 delims==" %%i in ('findstr /B "API_KEY=" .env') do set "EXISTING_API_KEY=%%i"
if "!EXISTING_API_KEY!"=="CHANGE_ME_TO_A_SECURE_RANDOM_STRING" (
    for /f "delims=" %%i in ('powershell -Command "[guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')"') do set "API_KEY=%%i"
    powershell -Command "(Get-Content .env) -replace 'API_KEY=CHANGE_ME_TO_A_SECURE_RANDOM_STRING', 'API_KEY=!API_KEY!' | Set-Content .env"
    echo Generated API_KEY automatically.
) else if "!EXISTING_API_KEY!"=="" (
    for /f "delims=" %%i in ('powershell -Command "[guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')"') do set "API_KEY=%%i"
    echo API_KEY=!API_KEY!>> .env
    echo Generated API_KEY automatically.
) else (
    echo Using existing API_KEY from .env
)

REM Check if POSTGRES_PASSWORD needs to be generated
for /f "tokens=2 delims==" %%i in ('findstr /B "POSTGRES_PASSWORD=" .env') do set "EXISTING_DB_PASS=%%i"
if "!EXISTING_DB_PASS!"=="CHANGE_ME_TO_SECURE_DB_PASSWORD" (
    for /f "delims=" %%i in ('powershell -Command "[guid]::NewGuid().ToString('N')"') do set "DB_PASS=%%i"
    powershell -Command "(Get-Content .env) -replace 'POSTGRES_PASSWORD=CHANGE_ME_TO_SECURE_DB_PASSWORD', 'POSTGRES_PASSWORD=!DB_PASS!' | Set-Content .env"
    echo Generated POSTGRES_PASSWORD automatically.
) else if "!EXISTING_DB_PASS!"=="" (
    for /f "delims=" %%i in ('powershell -Command "[guid]::NewGuid().ToString('N')"') do set "DB_PASS=%%i"
    echo POSTGRES_PASSWORD=!DB_PASS!>> .env
    echo Generated POSTGRES_PASSWORD automatically.
) else (
    echo Using existing POSTGRES_PASSWORD from .env
)

REM Check if ADMIN_PASSWORD needs to be prompted
for /f "tokens=2 delims==" %%i in ('findstr /B "ADMIN_PASSWORD=" .env') do set "EXISTING_ADMIN_PASS=%%i"
if "!EXISTING_ADMIN_PASS!"=="CHANGE_ME_TO_SECURE_ADMIN_PASSWORD" (
    :ask_password
    set /p "ADMIN_PASS=Enter admin password: "
    if "!ADMIN_PASS!"=="" (
        echo Password cannot be empty. Try again.
        goto ask_password
    )
    powershell -Command "(Get-Content .env) -replace 'ADMIN_PASSWORD=CHANGE_ME_TO_SECURE_ADMIN_PASSWORD', 'ADMIN_PASSWORD=!ADMIN_PASS!' | Set-Content .env"
) else if "!EXISTING_ADMIN_PASS!"=="" (
    :ask_password2
    set /p "ADMIN_PASS=Enter admin password: "
    if "!ADMIN_PASS!"=="" (
        echo Password cannot be empty. Try again.
        goto ask_password2
    )
    echo ADMIN_PASSWORD=!ADMIN_PASS!>> .env
) else (
    echo Using existing ADMIN_PASSWORD from .env
)

REM Check if PICTURES_PATH needs to be prompted
for /f "tokens=2 delims==" %%i in ('findstr /B "PICTURES_PATH=" .env') do set "EXISTING_PICS_PATH=%%i"
if "!EXISTING_PICS_PATH!"=="/path/to/your/pictures" (
    :ask_pictures
    set /p "PICS_PATH=Enter absolute path to your pictures directory: "
    if not exist "!PICS_PATH!" (
        echo Directory '!PICS_PATH!' does not exist. Try again.
        goto ask_pictures
    )
    REM Convert backslashes to forward slashes for Docker
    set "PICS_PATH_DOCKER=!PICS_PATH:\=/!"
    powershell -Command "(Get-Content .env) -replace 'PICTURES_PATH=/path/to/your/pictures', 'PICTURES_PATH=!PICS_PATH_DOCKER!' | Set-Content .env"
) else if "!EXISTING_PICS_PATH!"=="" (
    :ask_pictures2
    set /p "PICS_PATH=Enter absolute path to your pictures directory: "
    if not exist "!PICS_PATH!" (
        echo Directory '!PICS_PATH!' does not exist. Try again.
        goto ask_pictures2
    )
    REM Convert backslashes to forward slashes for Docker
    set "PICS_PATH_DOCKER=!PICS_PATH:\=/!"
    echo PICTURES_PATH=!PICS_PATH_DOCKER!>> .env
) else (
    echo Using existing PICTURES_PATH from .env
)
echo.

REM Load .env
echo Loading environment from .env...
for /f "usebackq tokens=1,* delims==" %%a in (".env") do (
    set "line=%%a"
    if not "!line:~0,1!"=="#" if not "!line!"=="" (
        set "%%a=%%b"
    )
)

REM ---- Validate required variables ----
set HAS_ERROR=0

if "%API_KEY%"=="CHANGE_ME_TO_A_SECURE_RANDOM_STRING" (
    echo ERROR: API_KEY is not set or still using placeholder value!
    set HAS_ERROR=1
)
if "%API_KEY%"=="" (
    echo ERROR: API_KEY is not set!
    set HAS_ERROR=1
)

if "%POSTGRES_PASSWORD%"=="CHANGE_ME_TO_SECURE_DB_PASSWORD" (
    echo ERROR: POSTGRES_PASSWORD is not set or still using placeholder value!
    set HAS_ERROR=1
)
if "%POSTGRES_PASSWORD%"=="" (
    echo ERROR: POSTGRES_PASSWORD is not set!
    set HAS_ERROR=1
)

if "%ADMIN_PASSWORD%"=="CHANGE_ME_TO_SECURE_ADMIN_PASSWORD" (
    echo ERROR: ADMIN_PASSWORD is not set or still using placeholder value!
    set HAS_ERROR=1
)
if "%ADMIN_PASSWORD%"=="" (
    echo ERROR: ADMIN_PASSWORD is not set!
    set HAS_ERROR=1
)

if "%PICTURES_PATH%"=="/path/to/your/pictures" (
    echo ERROR: PICTURES_PATH is not set or still using placeholder value!
    set HAS_ERROR=1
)
if "%PICTURES_PATH%"=="" (
    echo ERROR: PICTURES_PATH is not set!
    set HAS_ERROR=1
)

if %HAS_ERROR%==1 (
    echo.
    echo Please fix the errors above in .env and run this script again.
    pause
    exit /b 1
)

echo Configuration validated.
echo.

REM ---- Pull pre-built images ----
echo ======================================
echo   Pulling Docker images...
echo ======================================
docker compose pull
echo.
echo Images pulled successfully.
echo.

REM ---- Initialize database ----
echo ======================================
echo   Initializing database...
echo ======================================
echo.
echo Starting PostgreSQL...
docker compose up -d postgres
echo Waiting for PostgreSQL to be healthy...
:wait_pg
timeout /t 2 /nobreak > nul
docker compose exec postgres pg_isready -U "%POSTGRES_USER%" > nul 2>&1
if errorlevel 1 goto wait_pg
echo PostgreSQL is ready.
echo.

echo Creating database schema...
docker compose run --rm service create-db -pw "%ADMIN_PASSWORD%"
echo.
echo Database initialized.
echo.

REM ---- Run initial sync (foreground) ----
echo ======================================
echo   Starting initial sync...
echo ======================================
echo This will scan your pictures directory and populate the database.
echo Depending on the size of your collection, this may take a while.
echo Press Ctrl+C to stop (you can resume later with: docker compose run --rm service sync -f /pictures^)
echo.
docker compose run --rm service sync -f /pictures

echo.
echo ======================================
echo   Setup Complete!
echo ======================================
echo.
echo Admin credentials:
echo   Username: admin
echo   Password: (the password you entered above^)
echo.
echo To start the application run:
echo   start.bat
echo ======================================
echo.
pause