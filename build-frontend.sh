#!/bin/bash

# Build and Deploy Frontend Script for Temenos Alert Manager

echo "Building Temenos Alert Manager Frontend..."

# Navigate to frontend directory
cd "$(dirname "$0")/src/TemenosAlertManager.Web"

# Install dependencies if node_modules doesn't exist
if [ ! -d "node_modules" ]; then
    echo "Installing frontend dependencies..."
    npm install
fi

# Build the frontend
echo "Building frontend for production..."
npm run build

# Check if build was successful
if [ $? -ne 0 ]; then
    echo "Frontend build failed!"
    exit 1
fi

# Copy built files to API wwwroot
echo "Copying built files to API project..."
API_WWWROOT="../TemenosAlertManager.Api/wwwroot"

# Create wwwroot directory if it doesn't exist
mkdir -p "$API_WWWROOT"

# Remove old files
rm -rf "$API_WWWROOT"/*

# Copy new files
cp -r dist/* "$API_WWWROOT/"

echo "Frontend build and deployment completed successfully!"
echo "Files copied to: $API_WWWROOT"