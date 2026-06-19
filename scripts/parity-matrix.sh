#!/usr/bin/env bash
set -euo pipefail

# Three-way value-parity matrix: Roslyn vs Alder-JIT vs Alder-AOT.
#
# Publishes tests/Alder.AotMatrix as a NativeAOT binary (the AOT worker), then
# runs tests/Alder.ParityHarness (normal net8) which evaluates every corpus
# expression three ways and writes a CSV to artifacts/parity/. Exits non-zero if
# any expression's value or type disagrees across the three engines.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ "$ROOT_DIR" == /private/Users/* ]]; then
  ROOT_DIR="${ROOT_DIR#/private}"
fi

PUBLISH=1
FILTER=""
LIMIT=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-build) PUBLISH=0 ;;
    --filter) FILTER="$2"; shift ;;
    --limit) LIMIT="$2"; shift ;;
    -h|--help)
      cat <<'USAGE'
Usage: ./scripts/parity-matrix.sh [--no-build] [--filter <substr>] [--limit <n>]

Publishes the NativeAOT worker and runs the three-way parity matrix
(Roslyn / Alder-JIT / Alder-AOT) over ValidExpressions + ValidAsyncExpressions.

Options:
  --no-build       Reuse an existing published NativeAOT worker binary.
  --filter <substr> Only run expressions whose path contains <substr>.
  --limit <n>      Run at most <n> expressions (quick smoke runs).

Output CSV: artifacts/parity/parity_<timestamp>.csv  (git-ignored)
Exit code:  0 = all match, 1 = at least one mismatch.
USAGE
      exit 0
      ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
  shift
done

uname_s="$(uname -s)"
uname_m="$(uname -m)"
case "$uname_s/$uname_m" in
  Darwin/arm64) RID="osx-arm64" ;;
  Darwin/x86_64) RID="osx-x64" ;;
  Linux/x86_64) RID="linux-x64" ;;
  Linux/aarch64) RID="linux-arm64" ;;
  *) echo "Unsupported: $uname_s/$uname_m" >&2; exit 2 ;;
esac

WORKER_PROJECT="$ROOT_DIR/tests/Alder.AotMatrix/Alder.AotMatrix.csproj"
BIN_PATH="$ROOT_DIR/tests/Alder.AotMatrix/bin/Release/net8.0/$RID/publish/Alder.AotMatrix"

if [[ "$PUBLISH" -eq 1 ]]; then
  # Clean first. Incremental publish can silently keep a stale Alder.dll / stale
  # source-generator output, so the published native binary would not reflect
  # source changes — a parity run against a stale binary is worse than none.
  echo "[parity] Cleaning worker + Alder (avoids stale AOT binary)..."
  dotnet clean "$WORKER_PROJECT" -c Release -r "$RID" --nologo -v quiet > /dev/null 2>&1 || true
  dotnet clean "$ROOT_DIR/src/Alder/Alder.csproj" -c Release --nologo -v quiet > /dev/null 2>&1 || true

  echo "[parity] Publishing NativeAOT worker (RID: $RID)..."
  if ! dotnet publish "$WORKER_PROJECT" \
    -c Release -r "$RID" -p:SelfContained=true --nologo -v quiet > /dev/null 2>&1; then
    echo "[parity] NativeAOT publish failed. Re-running with visible output..." >&2
    dotnet publish "$WORKER_PROJECT" -c Release -r "$RID" -p:SelfContained=true --nologo -v minimal
    exit 1
  fi
fi

if [[ ! -x "$BIN_PATH" ]]; then
  echo "[parity] AOT worker not found at $BIN_PATH (run without --no-build)." >&2
  exit 3
fi

TEST_DATA="$ROOT_DIR/tests/Alder.Test/TestData"
TIMESTAMP="$(date +%Y-%m-%d_%H-%M-%S)"
OUT="$ROOT_DIR/artifacts/parity/parity_${TIMESTAMP}.csv"

EXTRA=()
[[ -n "$FILTER" ]] && EXTRA+=(--filter "$FILTER")
[[ -n "$LIMIT" ]] && EXTRA+=(--limit "$LIMIT")

echo "[parity] Running harness..."
dotnet run -c Release --project "$ROOT_DIR/tests/Alder.ParityHarness/Alder.ParityHarness.csproj" -- \
  --bin "$BIN_PATH" --testdata "$TEST_DATA" --out "$OUT" ${EXTRA[@]+"${EXTRA[@]}"}
