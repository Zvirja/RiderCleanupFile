param(
    [Parameter(Mandatory=$false)][string]$version = "1.0.0",
    [Parameter(Mandatory=$false)][string]$config="Debug",
    [Parameter(Mandatory=$false)][switch]$skipBuild
)

function Get-MSBuildExe() {
    $path = .\vswhere.exe -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($path) {
        $path = Join-Path $path 'MSBuild\15.0\Bin\MSBuild.exe'
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

$projectName = "AlexPovar.RiderCleanupFile"

if(-NOT $skipBuild.IsPresent) {
    $msBuildPath = Get-MSBuildExe;
    if(-not $msBuildPath) {
        Write-Host "Unable to locate MSBuild" -ForegroundColor "Red"
        return
    }

    Write-Host "Detected MSBuild: $msBuildPath" -ForegroundColor "Gray"

    & .\nuget.exe restore "..\code\$projectName.sln"
    & $msBuildPath "..\code\$projectName.sln" "/t:Build" "/p:Configuration=$config" "/verbosity:minimal"
}

# Pack Rider plugin
$tmpDirPath = ".\temp"
$pluginDirPath = Join-Path $tmpDirPath "RiderCleanupFile"
$outputDir = ".\artifacts"

if(Test-Path $tmpDirPath) {
    Remove-Item -Recurse -Force -Path $tmpDirPath 
}

New-Item -ItemType Directory -Force -Path $pluginDirPath |Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $pluginDirPath "META-INF") |Out-Null

& .\nuget.exe pack "$projectName.nuspec" -BasePath "..\code\$projectName\bin\$config\" -OutputDirectory $pluginDirPath -Version $version

$ideaPluginMetaPath = Join-Path $pluginDirPath "META-INF\plugin.xml"
Copy-Item -Path ".\idea-plugin.xml" -Destination $ideaPluginMetaPath

$pluginMetaDoc = [xml] (Get-Content $ideaPluginMetaPath)
Select-Xml -xml $pluginMetaDoc -XPath //idea-plugin/version | ForEach-Object { $_.Node.'#text' = "$version" }
$pluginMetaDoc.Save($ideaPluginMetaPath)

Push-Location $tmpDirPath
& ..\7za.exe a "..\artifacts\RiderCleanupFile.$version.zip" "RiderCleanupFile"
Pop-Location

Remove-Item -Recurse -Force -Path $tmpDirPath 