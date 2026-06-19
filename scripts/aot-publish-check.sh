#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ "$ROOT_DIR" == /private/Users/* ]]; then
  ROOT_DIR="${ROOT_DIR#/private}"
fi

CLEAN=1
STRICT=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-clean)
      CLEAN=0
      ;;
    --strict)
      STRICT=1
      ;;
    -h|--help)
      cat <<'USAGE'
Usage: ./scripts/aot-publish-check.sh [--no-clean] [--strict]

Publishes tests/Alder.AotMatrix as NativeAOT, captures the publish log, and
summarizes actionable trim/AOT IL warning codes.

Options:
  --no-clean  Skip the clean step. Faster, but incremental publish can hide warnings.
  --strict    Exit nonzero when actionable trim/AOT IL warnings are present.
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

PROJECT="$ROOT_DIR/tests/Alder.AotMatrix/Alder.AotMatrix.csproj"
ARTIFACTS_DIR="$ROOT_DIR/artifacts/aot-matrix"
mkdir -p "$ARTIFACTS_DIR"

echo "[aot-publish-check] Restoring NativeAOT project (RID: $RID)..."
dotnet restore "$PROJECT" -r "$RID" --nologo >/dev/null

if [[ "$CLEAN" -eq 1 ]]; then
  echo "[aot-publish-check] Cleaning NativeAOT output (RID: $RID)..."
  dotnet clean "$PROJECT" -c Release -r "$RID" --nologo >/dev/null
fi

timestamp="$(date +%Y-%m-%d_%H-%M-%S)"
LOG="$ARTIFACTS_DIR/aot-publish_${timestamp}.log"

echo "[aot-publish-check] Publishing NativeAOT binary (RID: $RID)..."
if ! dotnet publish "$PROJECT" \
  -c Release \
  -r "$RID" \
  -p:SelfContained=true \
  --nologo \
  -v minimal > "$LOG" 2>&1; then
  echo "[aot-publish-check] Publish failed. Full log: $LOG" >&2
  tail -n 80 "$LOG" >&2
  exit 1
fi

warning_lines="$(grep -E 'warning IL[0-9]{4}|analysis warning IL[0-9]{4}' "$LOG" | grep -v 'warning IL2104:' || true)"
warning_count="$(printf '%s\n' "$warning_lines" | sed '/^$/d' | wc -l | tr -d ' ')"
aggregate_count="$(grep -Ec 'warning IL2104:' "$LOG" || true)"

echo "[aot-publish-check] Publish succeeded. Full log: $LOG"
echo "[aot-publish-check] Actionable trim/AOT warning lines: $warning_count"
if [[ "$aggregate_count" -gt 0 ]]; then
  echo "[aot-publish-check] Aggregate IL2104 warning lines: $aggregate_count"
fi

if [[ "$warning_count" -gt 0 ]]; then
  echo "[aot-publish-check] Warning codes:"
  printf '%s\n' "$warning_lines" | grep -Eo 'IL[0-9]{4}' | sort | uniq -c | sort -nr

  if [[ "$STRICT" -eq 1 ]]; then
    echo "[aot-publish-check] Strict mode failed because actionable trim/AOT warnings are present." >&2
    exit 4
  fi
fi
