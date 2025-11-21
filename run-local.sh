#!/bin/bash

# Load environment variables from .env file and run the app locally
# Usage: ./run-local.sh

# Check if .env file exists
if [ ! -f .env ]; then
    echo "❌ Error: .env file not found!"
    echo "Please create a .env file with your Azure SQL connection string."
    echo "You can copy .env.example: cp .env.example .env"
    exit 1
fi

# Load .env file
echo "📁 Loading environment variables from .env..."
set -a
source .env
set +a

# Check if connection string is set
if [ -z "$AZURE_SQL_CONNECTION_STRING" ]; then
    echo "❌ Error: AZURE_SQL_CONNECTION_STRING not found in .env file!"
    exit 1
fi

# Set the connection string for .NET
export ConnectionStrings__DefaultConnection="$AZURE_SQL_CONNECTION_STRING"

echo "✅ Environment variables loaded!"
echo "🚀 Starting application..."
echo ""

# Run the app with the environment variable explicitly passed
dotnet watch run
