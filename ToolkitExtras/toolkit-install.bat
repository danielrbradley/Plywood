IF EXIST "%programfiles%\Plywood\PlywoodPullService.exe" c:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe "%programfiles%\Plywood\PlywoodPullService.exe" /u
IF EXIST "%programfiles%\Plywood" del "%programfiles%\Plywood\*" /Q
IF NOT EXIST "%programfiles%\Plywood" mkdir "%programfiles%\Plywood"
xcopy * "%programfiles%\Plywood" /Y /EXCLUDE:install-excludes.txt
c:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /username=.\ServiceAccountUsername /password=p4ssw0rd /unattended "%programfiles%\Plywood\PlywoodPullService.exe"
net start "Plywood Server Synchronisation"
exit