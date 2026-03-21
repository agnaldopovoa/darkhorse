#!/usr/bin/env bash

set -euo pipefail

echo "Removing all files ignored by .gitignore..."

# Preview (optional)
echo "Files to be removed:"
git clean -X -d -n

# Ask for confirmation
read -p "Proceed with deletion? (y/N): " confirm
if [[ "$confirm" != "y" && "$confirm" != "Y" ]]; then
    echo "Aborted."
    exit 0
fi

# Perform deletion
git clean -X -d -f

echo "Done."
