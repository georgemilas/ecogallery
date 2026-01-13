# Build Lambda deployment package for password rotation (Windows PowerShell)

Write-Host "Building Lambda rotation package..." -ForegroundColor Green

# Create temporary directory
if (Test-Path build) {
    Remove-Item -Recurse -Force build
}
New-Item -ItemType Directory -Path build | Out-Null

# Install dependencies
Write-Host "Installing Python dependencies..." -ForegroundColor Yellow
pip install -r requirements.txt -t build/

# Copy Lambda function
Write-Host "Copying Lambda function..." -ForegroundColor Yellow
Copy-Item lambda_function.py build/

# Create ZIP file
Write-Host "Creating ZIP package..." -ForegroundColor Yellow
$compress = @{
    Path = "build\*"
    CompressionLevel = "Optimal"
    DestinationPath = "lambda_rotation.zip"
}
Compress-Archive @compress -Force

# Clean up
Remove-Item -Recurse -Force build

Write-Host "Lambda package created: lambda_rotation.zip" -ForegroundColor Green
$size = (Get-Item lambda_rotation.zip).Length / 1MB
Write-Host ("Size: {0:N2} MB" -f $size) -ForegroundColor Green
