@echo off

set version=%1

dotnet restore .\NetJSON
dotnet pack .\NetJSON\NetJSON.csproj --output ..\nupkgs --configuration Release /p:PackageVersion=%version% --include-symbols --include-source
