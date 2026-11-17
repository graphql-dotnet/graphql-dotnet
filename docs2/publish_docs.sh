#!/usr/bin/env bash
set -euo pipefail

if [ -z "${1:-}" ]; then
  echo "ERROR: Please provide a version, for example: npm run deploy -- 8.0.0" >&2
  exit 1
fi

cd -- "$(dirname -- "${BASH_SOURCE[0]}")"

echo "Generating documentation for version $1"
npm run build

echo "Publishing"
npm exec -- gh-pages -d dist --dotfiles -b master \
  -r git@github.com:graphql-dotnet/graphql-dotnet.github.io.git \
  -m "Documentation update for $1"
