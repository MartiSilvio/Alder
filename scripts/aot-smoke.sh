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
using System.Diagnostics.CodeAnalysis;
using Alder;

int failures = 0;

void Check(string scenario, object? actual, object? expected)
{
    if (Equals(actual, expected))
        Console.WriteLine($"PASS: {scenario}");
    else
    {
        Console.Error.WriteLine($"FAIL: {scenario}: expected '{expected}', got '{actual}'");
        failures++;
    }
}

void Run(string name, Action action)
{
    try { action(); }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"CRASH: {name}: {ex.GetType().Name}: {ex.Message}");
        failures++;
    }
}

Run("Arithmetic", () =>
{
    var engine = new AlderEngine(AlderOptions.Default with { CompilationMode = CompilationMode.Interpreted });
    var result = engine.Evaluate("1 + 2 * 3");
    Check("Arithmetic (1 + 2 * 3 = 7)", result, 7);
});

Run("Math.Abs", () =>
{
    var engine = new AlderEngine(AlderOptions.Default with { CompilationMode = CompilationMode.Interpreted });
    var result = engine.Evaluate("Math.Abs(-42)");
    Check("Math.Abs(-42) = 42", result, 42);
});

Run("RegisteredMemberAccess", () =>
{
    var engine = new AlderEngine(AlderOptions.Default with { CompilationMode = CompilationMode.Interpreted });
    engine.RegisterFromType<TestDto>();
    var dto = new TestDto(42, "hello");
    engine.SetVariable("dto", dto);
    var result = engine.Evaluate("dto.Value");
    Check("Registered type member access (dto.Value = 42)", result, 42);
});

Run("StringConcat", () =>
{
    var engine = new AlderEngine(AlderOptions.Default with { CompilationMode = CompilationMode.Interpreted });
    Check("String concat", engine.Evaluate("\"hello\" + \" world\""), "hello world");
});

Run("Comparison", () =>
{
    var engine = new AlderEngine(AlderOptions.Default with { CompilationMode = CompilationMode.Interpreted });
    Check("Comparison (3 > 1)", engine.Evaluate("3 > 1"), true);
});

Run("Variable", () =>
{
    var engine = new AlderEngine(AlderOptions.Default with { CompilationMode = CompilationMode.Interpreted });
    engine.SetVariable("x", 10);
    Check("Variable (x * 2 = 20)", engine.Evaluate("x * 2"), 20);
});

Console.WriteLine(failures == 0 ? "\nALL PASS" : $"\n{failures} FAILED");
return failures == 0 ? 0 : 1;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public record TestDto(int Value, string Name);
CS

dotnet add "$APP_DIR/Smoke.csproj" reference "$ROOT_DIR/src/Alder/Alder.csproj" >/dev/null

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

output="$("$BIN_PATH" 2>&1)"
EXIT_CODE=$?
echo "$output"

if [[ $EXIT_CODE -eq 0 ]]; then
  echo "[aot-smoke] PASS"
else
  echo "[aot-smoke] FAILED (exit code $EXIT_CODE)" >&2
  exit $EXIT_CODE
fi
