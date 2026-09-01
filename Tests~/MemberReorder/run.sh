#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
dotnet build fixture/fixture.csproj -v q --nologo
dotnet build probe.csproj -v q --nologo
cp unitystub/bin/Debug/netstandard2.0/UnityEngine.CoreModule.dll bin/Debug/net7.0/
cp unitystub/bin/Debug/netstandard2.0/UnityEngine.CoreModule.dll .
dotnet bin/Debug/net7.0/probe.dll
