param([string]$apikey) 
.\NuGet.exe pack -symbols .\XUnitRemote\XUnitRemote.csproj
$pkg = ls .\*.nupkg | sort-object | where { !$_.FullName.Contains(".symbols.") } | select-object -last 1
./nuget.exe push $pkg.FullName  -ApiKey $apikey
