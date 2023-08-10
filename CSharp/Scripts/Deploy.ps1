param(
    [string]$destpath,
    [string]$apppackage,
    [string]$serverurl,
    [string]$username,
    [string]$password
)
$msdeploy = 'C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe'
$url = ("{0}{1}{2}" -f 'https://', $serverurl, ':8172/msdeploy.axd')
$msdeployArgs = @( 
"-verb:sync",
"-source:contentPath=""$apppackage""",
"-dest:contentPath=""$destpath"",ComputerName=""$url"",UserName=""$username"",Password=""$password"",AuthType='Basic'",
"-enableRule:DoNotDeleteRule",
"-allowUntrusted",
"-verbose"
)
& $msdeploy $msdeployArgs 