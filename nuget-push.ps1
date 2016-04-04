param([string]$apikey) 
$pkg = ls .\XUnitRemote\bin\Debug\*.nupkg | sort-object | select-object -last 1
./nuget.exe push $pkg.FullName  -ApiKey $apikey 
