#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ "$ROOT_DIR" == /private/Users/* ]]; then
  ROOT_DIR="${ROOT_DIR#/private}"
fi
TMP_BASE="$ROOT_DIR/.tmp"
mkdir -p "$TMP_BASE"
TMP_DIR="$(mktemp -d "$TMP_BASE/aot-smoke.XXXXXX")"
APP_DIR="$TMP_DIR/Smoke"

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

uname_s="$(uname -s)"
uname_m="$(uname -m)"

case "$uname_s/$uname_m" in
  Darwin/arm64) RID="osx-arm64" ;;
  Darwin/x86_64) RID="osx-x64" ;;
  Linux/x86_64) RID="linux-x64" ;;
  Linux/aarch64) RID="linux-arm64" ;;
  *)
    echo "Unsupported host for auto-RID detection: $uname_s/$uname_m" >&2
    echo "Set RID manually by editing scripts/aot-smoke.sh." >&2
    exit 2
    ;;
esac

echo "[aot-smoke] Temp dir: $TMP_DIR"
echo "[aot-smoke] RID: $RID"

dotnet new console --output "$APP_DIR" --framework net8.0 --no-restore >/dev/null

cat > "$APP_DIR/Program.cs" <<'CS'
using CsEval;

var engine = new CsEvalEngine(CsEvalOptions.Default with
{
    CompilationMode = CompilationMode.Interpreted
});

var result = engine.Evaluate("1 + 2 * 3");
Console.WriteLine(result);
CS

dotnet add "$APP_DIR/Smoke.csproj" reference "$ROOT_DIR/src/CsEval/CsEval.csproj" >/dev/null

dotnet publish "$APP_DIR/Smoke.csproj" \
  -c Release \
  -r "$RID" \
  -p:PublishAot=true \
  -p:SelfContained=true \
  -v minimal

BIN_PATH="$APP_DIR/bin/Release/net8.0/$RID/publish/Smoke"
if [[ ! -x "$BIN_PATH" ]]; then
  echo "AOT binary not found at $BIN_PATH" >&2
  exit 3
fi

output="$($BIN_PATH)"
echo "[aot-smoke] Output: $output"

if [[ "$output" != "7" ]]; then
  echo "AOT smoke failed: expected '7', got '$output'" >&2
  exit 4
fi

echo "[aot-smoke] PASS"
