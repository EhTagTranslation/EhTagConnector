$loc = Get-Location

$root = Split-Path -Parent $MyInvocation.MyCommand.Source
Set-Location $root/EhDbReleaseBuilder
Remove-Item ..\..\Database\tools\EhDbReleaseBuilder\ -Recurse -Exclude '*.md'
dotnet publish -c release -o ..\..\Database\tools\EhDbReleaseBuilder\
Remove-Item ..\..\Database\tools\EhDbReleaseBuilder\ -Recurse -Include '*.pdb'
Set-Location $loc