param(
    [ValidateSet('build','install','uninstall','package-zip')]
    [string]$Action = 'install',
    [ValidateSet('win-x64','win-x86','arm64')]
    [string]$Runtime = 'win-x64'
)

function Write-Info($msg){ Write-Host "[Finanzas] $msg" -ForegroundColor Cyan }
function Write-Ok($msg){ Write-Host "[Finanzas] $msg" -ForegroundColor Green }
function Write-Err($msg){ Write-Host "[Finanzas] $msg" -ForegroundColor Red }

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $RepoRoot 'Finanzas.csproj'
$DistRoot = Join-Path $RepoRoot 'dist'
$Dist = Join-Path $DistRoot $Runtime
$InstallDir = Join-Path $env:LocalAppData 'Programs\Finanzas'
$ExePath = Join-Path $InstallDir 'Finanzas.exe'
$DesktopLnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Finanzas.lnk'
$StartMenuLnk = Join-Path $env:AppData 'Microsoft\Windows\Start Menu\Programs\Finanzas.lnk'
$RegPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Finanzas'

function Invoke-BuildPublish {
    Write-Info "Publicando self-contained ($Runtime) ..."
    if (!(Test-Path $DistRoot)) { New-Item -ItemType Directory -Path $DistRoot | Out-Null }
    if (Test-Path $Dist) { Remove-Item -Recurse -Force $Dist }
    $dotnetArgs = @('publish', $Project, '-c', 'Release', '-r', $Runtime, '--self-contained', 'true', '-o', $Dist,
              '/p:PublishSingleFile=true', '/p:IncludeNativeLibrariesForSelfExtract=true')
    dotnet $dotnetArgs
    if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet publish' }
    Write-Ok "Artefactos publicados en $Dist"
}

function New-Shortcut($lnkPath, $targetPath, $iconPath){
    $ws = New-Object -ComObject WScript.Shell
    $sc = $ws.CreateShortcut($lnkPath)
    $sc.TargetPath = $targetPath
    $sc.WorkingDirectory = Split-Path -Parent $targetPath
    $sc.IconLocation = $iconPath
    $sc.Save()
}

function Install-Finanzas {
    Invoke-BuildPublish
    Write-Info "Instalando en $InstallDir ..."
    if (!(Test-Path $InstallDir)) { New-Item -ItemType Directory -Path $InstallDir | Out-Null }
    Copy-Item -Path (Join-Path $Dist '*') -Destination $InstallDir -Recurse -Force
    Write-Ok "Copiados binarios"

    Write-Info 'Creando accesos directos ...'
    New-Shortcut -lnkPath $DesktopLnk -targetPath $ExePath -iconPath "$ExePath,0"
    New-Shortcut -lnkPath $StartMenuLnk -targetPath $ExePath -iconPath "$ExePath,0"
    Write-Ok 'Accesos creados'

    Write-Info 'Registrando desinstalador ...'
    if (!(Test-Path $RegPath)) { New-Item -Path $RegPath -Force | Out-Null }
    $uninstallPs1 = Join-Path $InstallDir 'Uninstall.ps1'
    $uninstallLines = @(
        'param()',
        'try {',
        ('    $InstallDir = "' + $InstallDir + '"'),
        '    $DesktopLnk = Join-Path ([Environment]::GetFolderPath(''Desktop'')) ''Finanzas.lnk''',
        '    $StartMenuLnk = Join-Path $env:AppData ''Microsoft\Windows\Start Menu\Programs\Finanzas.lnk''',
        '    if (Test-Path $DesktopLnk) { Remove-Item $DesktopLnk -Force }',
        '    if (Test-Path $StartMenuLnk) { Remove-Item $StartMenuLnk -Force }',
        '    if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }',
        '    $RegPath = ''HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Finanzas''',
        '    if (Test-Path $RegPath) { Remove-Item $RegPath -Recurse -Force }',
        "    Write-Host 'Finanzas desinstalado'",
        '} catch {',
        '    Write-Host $_ -ForegroundColor Red',
        '    exit 1',
        '}'
    )
    Set-Content -Path $uninstallPs1 -Value $uninstallLines -Encoding UTF8

    New-ItemProperty -Path $RegPath -Name 'DisplayName' -Value 'Finanzas' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegPath -Name 'Publisher' -Value 'FinanzasApp' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegPath -Name 'DisplayVersion' -Value '1.0.0' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegPath -Name 'InstallLocation' -Value $InstallDir -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegPath -Name 'UninstallString' -Value "powershell.exe -ExecutionPolicy Bypass -File `"$uninstallPs1`"" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegPath -Name 'QuietUninstallString' -Value "powershell.exe -ExecutionPolicy Bypass -File `"$uninstallPs1`"" -PropertyType String -Force | Out-Null
    Write-Ok 'Registro completado'
}

function Uninstall-Finanzas {
    Write-Info 'Ejecutando desinstalación ...'
    $uninstallPs1 = Join-Path $InstallDir 'Uninstall.ps1'
    if (Test-Path $uninstallPs1) { powershell -ExecutionPolicy Bypass -File $uninstallPs1 }
    else {
        Write-Info 'No se encontró el script, limpiando manualmente'
        if (Test-Path $DesktopLnk) { Remove-Item $DesktopLnk -Force }
        if (Test-Path $StartMenuLnk) { Remove-Item $StartMenuLnk -Force }
        if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }
        if (Test-Path $RegPath) { Remove-Item $RegPath -Recurse -Force }
    }
    Write-Ok 'Desinstalación finalizada'
}

function Invoke-PackageZip {
    Invoke-BuildPublish
    $zipPath = Join-Path $DistRoot "Finanzas-$Runtime.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Write-Info "Empaquetando en $zipPath ..."
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($Dist, $zipPath)
    Write-Ok "ZIP creado: $zipPath"
}

switch ($Action) {
    'build' { Invoke-BuildPublish }
    'install' { Install-Finanzas }
    'uninstall' { Uninstall-Finanzas }
    'package-zip' { Invoke-PackageZip }
}
