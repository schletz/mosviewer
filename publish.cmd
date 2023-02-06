rd /S /Q Release

cd Mosviewer.App
dotnet publish -c Release -o ..\Release\App -p:PublishReadyToRun=true -p:PublishSingleFile=true -r win10-x64
cd ..
cd Mosviewer.Service
dotnet publish -c Release -o ..\Release\Service -p:PublishReadyToRun=true -p:PublishSingleFile=true -r win10-x64

cd ..
del Release.7z
"C:\Program Files\7-Zip\7z.exe" a -mx=9 Release.7z Release
