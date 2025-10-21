dotnet build .\SignalRClientGenerator\ -c:Release # required when GeneratePackageOnBuild=true
dotnet pack .\SignalRClientGenerator\ --no-build -c:Release --output $env:USERPROFILE\.nuget\local\
remove-item $env:USERPROFILE\.nuget\packages\SignalRClientGenerator, .\Sample\Client\bin\, .\Sample\Client\obj\ -force -recurse -ErrorAction Ignore
dotnet restore .\Sample\Client\ --force
dotnet build .\Sample\Client\ --no-restore --no-incremental