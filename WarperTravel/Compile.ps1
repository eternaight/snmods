$pluginFolder = Split-Path -Path $PSScriptRoot -Leaf
Set-Location "C:\Users\eterna_dark\Documents\GitHub\snmods\$($pluginFolder)"
dotnet build
New-Item -ItemType Directory -Force -Path "C:\Games\Steam\steamapps\common\Subnautica\BepInEx\plugins\$($pluginFolder)"
Copy-Item ".\bin\Debug\net472\$($pluginFolder).dll" "C:\Games\Steam\steamapps\common\Subnautica\BepInEx\plugins\$($pluginFolder)\$($pluginFolder).dll"
Copy-Item ".\bin\Debug\net472\$($pluginFolder).dll" "C:\Users\eterna_dark\Documents\GitHub\snmods\lib\$($pluginFolder).dll"
$b = Get-Date
Write-Output "$($pluginFolder) build complete at $b"
