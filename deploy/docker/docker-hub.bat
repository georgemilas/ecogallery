@echo off
setlocal enabledelayedexpansion

echo ======================================
echo   EcoGallery - Docker Hub Setup
echo ======================================
echo.

cd /d "%~dp0"

REM ---- .env file setup ----
echo.

REM ---- Ensure Git LFS files are pulled (ONNX models etc.) ----
echo Pulling Git LFS files...
git -C ..\.. lfs pull
echo.

REM ---- Build all images ----
echo ======================================
echo   Building Docker images...
echo ======================================
REM docker compose down --rmi all --volumes --remove-orphans
REM docker compose down service --rmi all --volumes --remove-orphans
REM docker compose down valbum --rmi all --volumes --remove-orphans
REM docker compose down cleanup --rmi all --volumes --remove-orphans

docker compose down 
docker compose down service 
docker compose down valbum 
docker compose down cleanup 

docker compose build
docker compose build service
docker compose build valbum
docker compose build cleanup
echo.
echo Build complete.
echo.

REM ---- tag images ----
echo ======================================
echo   Tagging Docker images...
echo ======================================
echo.
REM -----my gmail oauth - user gmilas 
docker login  
docker tag ecogallery-api gmilas/ecogallery-api:latest
docker tag ecogallery-frontend gmilas/ecogallery-frontend:latest  
docker tag ecogallery-nginx gmilas/ecogallery-nginx:latest
docker tag ecogallery-postgres gmilas/ecogallery-postgres:latest
docker tag ecogallery-service gmilas/ecogallery-service:latest
docker tag ecogallery-sync gmilas/ecogallery-sync:latest
docker tag ecogallery-face gmilas/ecogallery-face:latest
docker tag ecogallery-geo gmilas/ecogallery-geo:latest
docker tag ecogallery-valbum gmilas/ecogallery-valbum:latest
docker tag ecogallery-cleanup gmilas/ecogallery-cleanup:latest

echo.
echo Tagging completed.
echo.

REM ---- Run initial sync (foreground) ----
echo ======================================
echo   Push images to Docker Hub...
echo ======================================
echo.
docker push gmilas/ecogallery-api:latest
docker push gmilas/ecogallery-frontend:latest  
docker push gmilas/ecogallery-nginx:latest
docker push gmilas/ecogallery-postgres:latest
docker push gmilas/ecogallery-service:latest
docker push gmilas/ecogallery-sync:latest
docker push gmilas/ecogallery-face:latest
docker push gmilas/ecogallery-geo:latest
docker push gmilas/ecogallery-valbum:latest
docker push gmilas/ecogallery-cleanup:latest
echo.
echo Images pushed to Docker Hub.
echo.

echo.
echo ======================================
echo   Docker Hub Setup Complete!
echo ======================================
echo.
pause