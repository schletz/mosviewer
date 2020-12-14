rd /S /Q Release
dotnet publish -c Release -o Release -p:PublishReadyToRun=true -p:PublishSingleFile=true -r win10-x64
dotnet dev-certs https --trust
del Release.7z
"C:\Program Files\7-Zip\7z.exe" a -mx=9 Release.7z Release
