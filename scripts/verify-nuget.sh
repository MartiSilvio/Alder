#!/usr/bin/env bash
set -euo pipefail

dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build --framework net8.0

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*)
    dotnet test tests/Alder.Test/Alder.Test.csproj --configuration Release --no-build --framework net472
    ;;
  *)
    if command -v mono >/dev/null 2>&1; then
      dotnet test tests/Alder.Test/Alder.Test.csproj --configuration Release --no-build --framework net472
    else
      echo "Skipping net472 runtime tests: .NET Framework test execution requires Windows or mono." >&2
    fi
    ;;
esac

case "$(uname -s)/$(uname -m)" in
  Darwin/*|Linux/*)
    ./scripts/aot-publish-check.sh --strict
    ;;
  *)
    echo "Skipping NativeAOT warning gate: unsupported host platform." >&2
    ;;
esac

rm -rf artifacts/packages
dotnet pack src/Alder/Alder.csproj --configuration Release --output artifacts/packages
dotnet run --project tests/Alder.PackageVerification/Alder.PackageVerification.csproj -- artifacts/packages
