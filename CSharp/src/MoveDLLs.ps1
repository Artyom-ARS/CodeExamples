
cd ..
$Files = Get-ChildItem -Path ./lib -recurse | Where {$_.extension -eq ".dll"}
cd src

$Projects = Get-ChildItem -Directory |  ? { $_.PsIsContainer -and $_.FullName -notmatch 'packages' }
foreach($projectFolder in $Projects)
 {
    if (Test-Path "./$projectFolder\bin\Debug") {
      foreach($dll in $Files) {cp $dll.fullname "./$projectFolder\bin\Debug"}
    }
	if (Test-Path "./$projectFolder\bin\Release") {
      foreach($dll in $Files) {cp $dll.fullname "./$projectFolder\bin\Release"}
    }
 }

