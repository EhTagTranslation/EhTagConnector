$loc = Get-Location

$root = Split-Path -Parent $MyInvocation.MyCommand.Source
Set-Location $root/EhDbReleaseBuilder
dotnet publish -c release -o ..\..\Database\tools\EhDbReleaseBuilder\

Set-Location $loc