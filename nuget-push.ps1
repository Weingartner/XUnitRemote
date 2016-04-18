param([string]$apikey, [string]$configuration="Debug") 
rm *.nupkg
.\NuGet.exe pack -Prop Platform=AnyCPU -Prop Configuration=$configuration -symbols .\XUnitRemote\XUnitRemote.csproj
$pkg = ls .\*.nupkg | where { !$_.FullName.Contains(".symbols.") } | select-object -last 1
./nuget.exe push $pkg.FullName  -ApiKey $apikey
