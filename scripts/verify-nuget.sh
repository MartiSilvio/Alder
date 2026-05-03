#!/usr/bin/env bash
set -euo pipefail

dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
rm -rf artifacts/packages
dotnet pack src/Alder/Alder.csproj --configuration Release --output artifacts/packages
dotnet run --project tests/Alder.PackageVerification/Alder.PackageVerification.csproj -- artifacts/packages
