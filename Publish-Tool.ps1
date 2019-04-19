$loc = Get-Location

$root = Split-Path -Parent $MyInvocation.MyCommand.Source
Set-Location $root/EhDbReleaseBuilder
$target = Get-Item ..\..\Database\tools\EhDbReleaseBuilder\
Remove-Item $target -Recurse -Exclude '*.md'
dotnet publish -c release -o $target
Remove-Item $target -Recurse -Include '*.pdb'
Remove-Item $target\EhDbReleaseBuilder.deps.json
Move-Item $target\runtimes\win-x64\native\* $target
Remove-Item $target\runtimes\win-x64\native
Move-Item $target\runtimes\win-x64\* $target
Remove-Item $target\runtimes -Recurse
Set-Location $loc