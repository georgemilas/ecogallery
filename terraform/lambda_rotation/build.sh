#!/bin/bash
# Build Lambda deployment package for password rotation

set -e

echo "Building Lambda rotation package..."

# Create temporary directory
rm -rf build
mkdir -p build

# Install dependencies
pip install -r requirements.txt -t build/

# Copy Lambda function
cp lambda_function.py build/

# Create ZIP file
cd build
zip -r ../lambda_rotation.zip .
cd ..

# Clean up
rm -rf build

echo "Lambda package created: lambda_rotation.zip"
echo "Size: $(du -h lambda_rotation.zip | cut -f1)"
