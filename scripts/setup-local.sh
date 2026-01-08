#!/bin/bash
#
# Sets up the local development environment for Dynamis Reference App.
#
# This script:
# - Verifies prerequisites (.NET, Node.js, Azure Functions Core Tools)
# - Creates local.settings.json from template
# - Creates .env from template
# - Restores NuGet packages
# - Installs npm packages
# - Applies database migrations
#
# Usage: ./scripts/setup-local.sh [--skip-migrations] [--force]

set -e

SKIP_MIGRATIONS=false
FORCE=false

# Parse arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        --skip-migrations) SKIP_MIGRATIONS=true ;;
        --force) FORCE=true ;;
        *) echo "Unknown parameter: $1"; exit 1 ;;
    esac
    shift
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
FUNCTIONS_PATH="$ROOT_DIR/src/Dynamis.Functions"
WEBAPI_PATH="$ROOT_DIR/src/Dynamis.WebApi"
FRONTEND_PATH="$ROOT_DIR/src/frontend"

echo "================================="
echo "Dynamis Reference App Setup"
echo "================================="
echo ""

# Check prerequisites
echo "Checking prerequisites..."

# Check .NET SDK
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "  [OK] .NET SDK: $DOTNET_VERSION"
else
    echo "  [ERROR] .NET SDK not found. Please install .NET 10 SDK."
    exit 1
fi

# Check Node.js
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    echo "  [OK] Node.js: $NODE_VERSION"
else
    echo "  [ERROR] Node.js not found. Please install Node.js 20+."
    exit 1
fi

# Check Azure Functions Core Tools
if command -v func &> /dev/null; then
    FUNC_VERSION=$(func --version)
    echo "  [OK] Azure Functions Core Tools: $FUNC_VERSION"
else
    echo "  [WARNING] Azure Functions Core Tools not found."
    echo "           Install with: npm install -g azure-functions-core-tools@4"
fi

echo ""

# Create local.settings.json for Functions
if [ -d "$FUNCTIONS_PATH" ]; then
    echo "Setting up Functions configuration..."
    LOCAL_SETTINGS="$FUNCTIONS_PATH/local.settings.json"
    LOCAL_SETTINGS_EXAMPLE="$FUNCTIONS_PATH/local.settings.example.json"

    if [[ -f "$LOCAL_SETTINGS" ]] && [[ "$FORCE" != true ]]; then
        echo "  [SKIP] local.settings.json already exists"
    elif [[ -f "$LOCAL_SETTINGS_EXAMPLE" ]]; then
        cp "$LOCAL_SETTINGS_EXAMPLE" "$LOCAL_SETTINGS"
        echo "  [OK] Created local.settings.json from template"
        echo "       Please update the connection string in local.settings.json"
    else
        echo "  [WARNING] local.settings.example.json not found"
    fi
fi

# Create .env for Frontend
if [ -d "$FRONTEND_PATH" ]; then
    echo "Setting up frontend configuration..."
    ENV_FILE="$FRONTEND_PATH/.env"
    ENV_EXAMPLE="$FRONTEND_PATH/.env.example"

    if [[ -f "$ENV_FILE" ]] && [[ "$FORCE" != true ]]; then
        echo "  [SKIP] .env already exists"
    elif [[ -f "$ENV_EXAMPLE" ]]; then
        cp "$ENV_EXAMPLE" "$ENV_FILE"
        echo "  [OK] Created .env from template"
    else
        echo "  [WARNING] .env.example not found"
    fi
fi

echo ""

# Restore NuGet packages
echo "Restoring NuGet packages..."
cd "$API_PATH"
if dotnet restore --verbosity minimal; then
    echo "  [OK] NuGet packages restored"
else
    echo "  [ERROR] Failed to restore NuGet packages"
fi
cd "$ROOT_DIR"

echo ""

# Install npm packages
echo "Installing npm packages..."
cd "$FRONTEND_PATH"
if npm install --silent; then
    echo "  [OK] npm packages installed"
else
    echo "  [ERROR] Failed to install npm packages"
fi
cd "$ROOT_DIR"

echo ""

# Apply migrations
if [[ "$SKIP_MIGRATIONS" != true ]]; then
    echo "Applying database migrations..."
    cd "$API_PATH"
    if dotnet ef database update 2>/dev/null; then
        echo "  [OK] Database migrations applied"
    else
        echo "  [WARNING] Failed to apply migrations. Ensure the connection string is configured."
    fi
    cd "$ROOT_DIR"
else
    echo "Skipping database migrations"
fi

echo ""
echo "================================="
echo "Setup Complete!"
echo "================================="
echo ""
echo "Next steps:"
echo "  1. Update the connection string in src/api/local.settings.json"
echo "  2. Start the backend: cd src/api && func start"
echo "  3. Start the frontend: cd src/frontend && npm run dev"
echo ""
