#!/bin/bash
set -e

echo "🔧 Enabling pnpm..."

pnpm config set store-dir /home/vscode/.pnpm-store

echo "📦 Installing frontend dependencies (if present)..."

if [ -f "src/frontend/package.json" ]; then
  cd src/frontend
  pnpm install
  cd -
fi

echo "✅ DevContainer ready."