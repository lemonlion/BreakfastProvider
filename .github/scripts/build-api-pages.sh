#!/usr/bin/env bash
# Builds the Pages site's landing page and API section into <site-dir>: every contract as a UI page and as the
# documents behind it, each one click from the landing page and from its own page. The documents are the ones the
# component tests committed to docs/ (CI's drift gate keeps them in step with the service).
#
# usage, from the repository root:  .github/scripts/build-api-pages.sh <site-dir>
set -euo pipefail

site=${1:?usage: build-api-pages.sh <site-dir>}
api="$site/api"
mkdir -p "$api/grpc/protos"

# The landing page, the UI shells and the bar they open with.
cp .github/pages/index.html "$site/index.html"
cp .github/pages/api/*.html .github/pages/api/*.css "$api/"

# The renderers, pinned. -f fails the build on a failed download instead of publishing a broken page.
npm=https://cdn.jsdelivr.net/npm
curl -fsSL "$npm/@scalar/api-reference@1.72.3/dist/browser/standalone.js" -o "$api/scalar.js"
curl -fsSL "$npm/@asyncapi/react-component@3.2.1/browser/standalone/index.js" -o "$api/asyncapi-standalone.js"
curl -fsSL "$npm/@asyncapi/react-component@3.2.1/styles/default.min.css" -o "$api/asyncapi.css"
curl -fsSL "$npm/graphql-voyager@2.1.0/dist/voyager.standalone.js" -o "$api/voyager.standalone.js"
curl -fsSL "$npm/graphql-voyager@2.1.0/dist/voyager.css" -o "$api/voyager.css"

# Every contract's documents, at predictable URLs: api/<contract>.json, and the source the contract is written in.
cp docs/openapi.json docs/asyncapi.json docs/grpc.json docs/graphql.json docs/schema.graphql "$api/"

# The gRPC page is the one the service serves at /grpc/, so its relative links expect v1.json and
# protos/breakfast.proto beside it. On Pages its bar also leads back to the landing page.
cp docs/grpc.json "$api/grpc/v1.json"
cp src/BreakfastProvider.Api/Protos/breakfast.proto "$api/grpc/protos/breakfast.proto"
sed 's#<nav class="contract-bar">#&<a href="../../">← Breakfast Provider</a>#' docs/grpc.html > "$api/grpc/index.html"
grep -q '<a href="../../">' "$api/grpc/index.html" \
  || { echo "::error::docs/grpc.html has no <nav class=\"contract-bar\"> for the link back to the landing page"; exit 1; }

echo "Built $site/index.html and $api/"
