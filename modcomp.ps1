Set-Location .\NewPlugin
dotnet build
Copy-Item .\bin\Debug\net46\NewPlugin.dll C:\Games\Steam\steamapps\common\Subnautica\BepInEx\plugins
