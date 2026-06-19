#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ "$ROOT_DIR" == /private/Users/* ]]; then
  ROOT_DIR="${ROOT_DIR#/private}"
fi

uname_s="$(uname -s)"
uname_m="$(uname -m)"
PUBLISH=1

while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-build)
      PUBLISH=0
      ;;
    -h|--help)
      cat <<'USAGE'
Usage: ./scripts/aot-matrix.sh [--no-build]

Publishes tests/Alder.AotMatrix as NativeAOT and runs the compatibility
matrix against ValidExpressions and ValidAsyncExpressions.

Options:
  --no-build  Reuse an existing published NativeAOT binary.
USAGE
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      exit 2
      ;;
  esac
  shift
done

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

BIN_PATH="$ROOT_DIR/tests/Alder.AotMatrix/bin/Release/net8.0/$RID/publish/Alder.AotMatrix"

if [[ "$PUBLISH" -eq 1 ]]; then
  echo "[aot-matrix] Publishing NativeAOT binary (RID: $RID)..."

  if ! dotnet publish "$ROOT_DIR/tests/Alder.AotMatrix/Alder.AotMatrix.csproj" \
    -c Release \
    -r "$RID" \
    -p:SelfContained=true \
    --nologo \
    -v quiet > /dev/null 2>&1; then
    echo "[aot-matrix] NativeAOT publish failed. Re-running with visible output..." >&2
    dotnet publish "$ROOT_DIR/tests/Alder.AotMatrix/Alder.AotMatrix.csproj" \
      -c Release \
      -r "$RID" \
      -p:SelfContained=true \
      --nologo \
      -v minimal
    exit 1
  fi
elif [[ -x "$BIN_PATH" ]]; then
  echo "[aot-matrix] Using existing NativeAOT binary (RID: $RID)..."
fi

if [[ ! -x "$BIN_PATH" ]]; then
  echo "AOT binary not found at $BIN_PATH" >&2
  exit 3
fi

TEST_DATA="$ROOT_DIR/tests/Alder.Test/TestData"
matrix_count="$(
  find "$TEST_DATA/ValidExpressions" "$TEST_DATA/ValidAsyncExpressions" \
    -name '*.csx' ! -name '*.roslyn.csx' ! -name '*.ignore.csx' 2>/dev/null |
    wc -l |
    tr -d ' '
)"

echo "[aot-matrix] Running against $matrix_count matrix expressions..."
echo

set +e
AOT_REPORT_DIR="$ROOT_DIR" "$BIN_PATH" "$TEST_DATA"
matrix_status=$?
set -e

if [[ "$matrix_status" -ne 0 ]]; then
  echo
  echo "[aot-matrix] Matrix completed with failures (exit $matrix_status). See representative failures and full report above."
fi

exit "$matrix_status"
