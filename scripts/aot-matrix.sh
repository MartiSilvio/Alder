#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ "$ROOT_DIR" == /private/Users/* ]]; then
  ROOT_DIR="${ROOT_DIR#/private}"
fi

uname_s="$(uname -s)"
uname_m="$(uname -m)"

case "$uname_s/$uname_m" in
  Darwin/arm64) RID="osx-arm64" ;;
  Darwin/x86_64) RID="osx-x64" ;;
  Linux/x86_64) RID="linux-x64" ;;
  Linux/aarch64) RID="linux-arm64" ;;
  *)
    echo "Unsupported: $uname_s/$uname_m" >&2
    exit 2
    ;;
esac

echo "[aot-matrix] Publishing NativeAOT binary (RID: $RID)..."

dotnet publish "$ROOT_DIR/tests/CsEval.AotMatrix/CsEval.AotMatrix.csproj" \
  -c Release \
  -r "$RID" \
  -p:SelfContained=true \
  --nologo \
  -v quiet > /dev/null 2>&1

BIN_PATH="$ROOT_DIR/tests/CsEval.AotMatrix/bin/Release/net8.0/$RID/publish/CsEval.AotMatrix"
if [[ ! -x "$BIN_PATH" ]]; then
  echo "AOT binary not found at $BIN_PATH" >&2
  exit 3
fi

TEST_DATA="$ROOT_DIR/tests/CsEval.Test/TestData/ValidExpressions"

echo "[aot-matrix] Running against $(find "$TEST_DATA" -name '*.csx' ! -name '*.roslyn.csx' | wc -l | tr -d ' ') expressions..."
echo

AOT_REPORT_DIR="$ROOT_DIR" "$BIN_PATH" "$TEST_DATA"
