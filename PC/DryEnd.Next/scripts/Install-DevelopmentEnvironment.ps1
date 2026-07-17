[CmdletBinding()]
param(
    [switch]$IncludePowerShell7,
    [switch]$IncludeSqlTools,
    [switch]$SkipNodeUpgrade
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-Administrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $currentPrincipal = New-Object `
        -TypeName Security.Principal.WindowsPrincipal `
        -ArgumentList $currentIdentity

    $administratorRole = [Security.Principal.WindowsBuiltInRole]::Administrator
    if (-not $currentPrincipal.IsInRole($administratorRole)) {
        throw 'Run this script from a PowerShell window opened as Administrator.'
    }
}

function Assert-WingetAvailable {
    if (-not (Get-Command 'winget.exe' -ErrorAction SilentlyContinue)) {
        throw 'winget is required. Install or repair Microsoft App Installer before continuing.'
    }
}

function Test-WingetPackageInstalled {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Id
    )

    & winget.exe list `
        --source winget `
        --accept-source-agreements `
        --id $Id `
        --exact *> $null

    return ($LASTEXITCODE -eq 0)
}

function Install-WingetPackage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Id,

        [Parameter(Mandatory = $true)]
        [string]$DisplayName
    )

    if (Test-WingetPackageInstalled -Id $Id) {
        Write-Host "[OK] $DisplayName is already installed." -ForegroundColor Green
        return
    }

    Write-Host "[INSTALL] $DisplayName ($Id)" -ForegroundColor Cyan

    & winget.exe install `
        --source winget `
        --accept-source-agreements `
        --accept-package-agreements `
        --disable-interactivity `
        --silent `
        --id $Id `
        --exact

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install $DisplayName. winget exit code: $LASTEXITCODE"
    }
}

function Update-ProcessPath {
    $machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$machinePath;$userPath"
}

function Get-NodeMajorVersion {
    $nodeCommand = Get-Command 'node.exe' -ErrorAction SilentlyContinue
    if ($null -eq $nodeCommand) {
        return $null
    }

    $versionText = (& $nodeCommand.Source --version 2>$null | Select-Object -First 1)
    if ($versionText -match '^v(?<Major>\d+)\.') {
        return [int]$Matches.Major
    }

    return $null
}

function Install-Node24Lts {
    $nodeMajorVersion = Get-NodeMajorVersion

    if ($nodeMajorVersion -eq 24) {
        Write-Host '[OK] Node.js 24 is already installed.' -ForegroundColor Green
        return
    }

    if ($nodeMajorVersion -and $SkipNodeUpgrade) {
        Write-Warning "Node.js $nodeMajorVersion is installed. Upgrade skipped by parameter."
        return
    }

    if ($nodeMajorVersion) {
        Write-Host "[REMOVE] Legacy Node.js $nodeMajorVersion" -ForegroundColor Yellow

        $legacyNodePackages = @(
            'OpenJS.NodeJS.16',
            'OpenJS.NodeJS.18',
            'OpenJS.NodeJS.20',
            'OpenJS.NodeJS.22',
            'OpenJS.NodeJS'
        )

        foreach ($legacyPackageId in $legacyNodePackages) {
            if (Test-WingetPackageInstalled -Id $legacyPackageId) {
                & winget.exe uninstall `
                    --source winget `
                    --accept-source-agreements `
                    --disable-interactivity `
                    --silent `
                    --id $legacyPackageId `
                    --exact

                if ($LASTEXITCODE -ne 0) {
                    throw "Failed to remove legacy Node.js package $legacyPackageId."
                }
            }
        }

        Update-ProcessPath
    }

    Install-WingetPackage `
        -Id 'OpenJS.NodeJS.LTS' `
        -DisplayName 'Node.js 24 LTS'

    Update-ProcessPath

    $installedMajorVersion = Get-NodeMajorVersion
    if ($installedMajorVersion -ne 24) {
        throw "Node.js major version 24 was expected, but version $installedMajorVersion was detected."
    }
}

function Resolve-CodeCommand {
    $codeCommand = Get-Command 'code.cmd' -ErrorAction SilentlyContinue
    if ($codeCommand) {
        return $codeCommand.Source
    }

    $candidatePaths = @(
        "$env:ProgramFiles\Microsoft VS Code\bin\code.cmd",
        "$env:LOCALAPPDATA\Programs\Microsoft VS Code\bin\code.cmd"
    )

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path -LiteralPath $candidatePath) {
            return $candidatePath
        }
    }

    throw 'VS Code was installed, but code.cmd could not be located.'
}

function Install-CodeExtension {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CodeCommand,

        [Parameter(Mandatory = $true)]
        [string]$ExtensionId
    )

    Write-Host "[EXTENSION] $ExtensionId" -ForegroundColor Cyan
    & $CodeCommand --install-extension $ExtensionId --force

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install VS Code extension $ExtensionId."
    }
}

Write-Host ''
Write-Host 'DryEnd.Next development environment installation' -ForegroundColor Cyan
Write-Host 'Instalacao do ambiente de desenvolvimento DryEnd.Next' -ForegroundColor Cyan
Write-Host ''

Assert-Administrator
Assert-WingetAvailable

Install-WingetPackage `
    -Id 'Microsoft.VisualStudioCode' `
    -DisplayName 'Microsoft Visual Studio Code'

Install-WingetPackage `
    -Id 'Microsoft.DotNet.SDK.10' `
    -DisplayName '.NET 10 SDK x64'

Install-Node24Lts

if (-not (Get-Command 'git.exe' -ErrorAction SilentlyContinue)) {
    Install-WingetPackage `
        -Id 'Git.Git' `
        -DisplayName 'Git for Windows'
}
else {
    Write-Host '[OK] Git for Windows is already installed.' -ForegroundColor Green
}

if ($IncludePowerShell7) {
    Install-WingetPackage `
        -Id 'Microsoft.PowerShell' `
        -DisplayName 'PowerShell 7'
}

Update-ProcessPath
$codeCommand = Resolve-CodeCommand

$requiredExtensions = @(
    'ms-dotnettools.csdevkit',
    'dbaeumer.vscode-eslint',
    'esbenp.prettier-vscode',
    'ms-vscode.powershell'
)

if ($IncludeSqlTools) {
    $requiredExtensions += 'ms-mssql.mssql'
}

foreach ($extensionId in $requiredExtensions) {
    Install-CodeExtension `
        -CodeCommand $codeCommand `
        -ExtensionId $extensionId
}

Write-Host ''
Write-Host 'Installation completed. Open a new PowerShell window before development.' -ForegroundColor Green
Write-Host 'Instalacao concluida. Abra uma nova janela do PowerShell antes de desenvolver.' -ForegroundColor Green
Write-Host ''

& (Join-Path $PSScriptRoot 'Get-EnvironmentStatus.ps1')
