#
# deploy.ps1
#

dotnet publish -r linux-arm -c Release

$ScriptPath = (Get-Item -Path ".\" -Verbose).FullName

$PasswordsFilePath = -join($ScriptPath, "\RedditFighterBotCore\passwords.xml")

[xml]$XmlDocument = Get-Content -Path $PasswordsFilePath

$username = $XmlDocument.config.machines.machine.username
$password = $XmlDocument.config.machines.machine.password
$ip = $XmlDocument.config.machines.machine.ip

$Password = ConvertTo-SecureString $password -AsPlainText -Force
$Credentials = New-Object System.Management.Automation.PSCredential ($username, $Password)

# Remove everything from the publish folder
New-SSHSession -ComputerName $ip -Credential $Credentials -KeyFile "Z:\pi_keys\pi2.ppk"
Invoke-SSHCommand -Index 0 -Command "screen -X -S monitor quit"
Invoke-SSHCommand -Index 0 -Command "rm -rf /home/pi/bot/publish/*"
Invoke-SSHCommand -Index 0 -Command "rm /home/pi/bot/passwords.xml"


# Send each file in the local build folder to the publish folder on the pi
New-SFTPSession -ComputerName $ip -Credential $Credentials -KeyFile "Z:\pi_keys\pi2.ppk"
$SftpPath = "/home/pi/bot/publish/"

$FilePath = -join($ScriptPath, "\RedditFighterBotCore\bin\Release\netcoreapp2.0\linux-arm\publish")

$files = Get-ChildItem $FilePath
For($i = 0; $i -lt $files.Length; $i++) {

	$FullFileNamePath = -join($FilePath, "\", $files[$i])
	Set-SFTPFile -SessionId 0 -LocalFile $FullFileNamePath -RemotePath $SftpPath
}

$FullFileNamePath = -join($ScriptPath, "\RedditFighterBotCore\passwords.xml")

Set-SFTPFile -SessionId 0 -LocalFile $FullFileNamePath -RemotePath "/home/pi/bot/"

Invoke-SSHCommand -Index 0 -Command "chmod 755 /home/pi/bot/publish/RedditFighterBotCore"

Invoke-SSHCommand -Index 0 -Command "cd /home/pi/bot/ && screen -S monitor -d -m ./monitor.sh"

Remove-SFTPSession -SessionId 0
Remove-SSHSession -Index 0
