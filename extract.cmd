taskkill /IM Mosviewer.Service.exe
taskkill /IM Mosviewer.exe
"C:\Program Files\7-Zip\7z.exe" x Release.7z -aoa
cd Release\App
start Mosviewer.exe
cd ..\Service
start Mosviewer.Service.exe
