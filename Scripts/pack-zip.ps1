Remove-Item ..\QuickLook.Plugin.CADImport.qlplugin -ErrorAction SilentlyContinue

$files = Get-ChildItem -Path ..\Build\Release\ -Exclude *.pdb,*.xml
Compress-Archive $files ..\QuickLook.Plugin.CADImport.zip
Move-Item ..\QuickLook.Plugin.CADImport.zip ..\QuickLook.Plugin.CADImport.qlplugin