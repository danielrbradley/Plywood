"C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" PlywoodCommandLine\PlywoodCommandLine.csproj /P:Configuration=Release
"C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" IISManagerTools\IISManagerTools.csproj /P:Configuration=Release
"C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" PlywoodPullService\PlywoodPullService.csproj /P:Configuration=Release
IF EXIST C:\Packages\PlywoodToolkit\NUL del C:\Packages\PlywoodToolkit\* /Q
IF NOT EXIST C:\Packages\NUL mkdir C:\Packages
IF NOT EXIST C:\Packages\PlywoodToolkit\NUL mkdir C:\Packages\PlywoodToolkit
xcopy PlywoodCommandLine\bin\Release\* C:\Packages\PlywoodToolkit /Y
xcopy "PlywoodToolkit\DefaultPullConfig.config" C:\Packages\PlywoodToolkit\DefaultPullConfig.config /Y
xcopy IISManagerTools\bin\Release\* C:\Packages\PlywoodToolkit /Y
xcopy PlywoodPullService\bin\Release\* C:\Packages\PlywoodToolkit /Y
xcopy ToolkitExtras\* C:\Packages\PlywoodToolkit /Y