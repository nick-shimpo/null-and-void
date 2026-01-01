#!/bin/bash
# Install git hooks for Null and Void project
# Run this script from the project root directory

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
HOOKS_SOURCE="$SCRIPT_DIR/hooks"
HOOKS_TARGET="$PROJECT_ROOT/.git/hooks"

echo "Installing git hooks..."

# Check if we're in a git repository
if [ ! -d "$PROJECT_ROOT/.git" ]; then
    echo "ERROR: Not a git repository. Run 'git init' first."
    exit 1
fi

# Copy hooks
for hook in pre-commit pre-push; do
    if [ -f "$HOOKS_SOURCE/$hook" ]; then
        cp "$HOOKS_SOURCE/$hook" "$HOOKS_TARGET/$hook"
        chmod +x "$HOOKS_TARGET/$hook"
        echo "✓ Installed $hook hook"
    else
        echo "WARNING: $hook hook source not found"
    fi
done

echo ""
echo "Git hooks installed successfully!"
echo ""
echo "Hooks will run automatically on:"
echo "  - pre-commit: Format check, build, and test"
echo "  - pre-push: Full test suite with coverage check"
