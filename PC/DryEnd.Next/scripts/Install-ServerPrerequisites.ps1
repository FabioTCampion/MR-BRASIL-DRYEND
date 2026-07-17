[CmdletBinding()]
param(
    [switch]$SkipDotnetRuntime,

    [string]$SqlServerHost,

    [ValidateRange(1, 65535)]
    [int]$SqlServerPort = 1433
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

Write-Host ''
Write-Host 'DryEnd.Next server prerequisites' -ForegroundColor Cyan
Write-Host 'Pre-requisitos do servidor DryEnd.Next' -ForegroundColor Cyan
Write-Host ''

if (-not $SkipDotnetRuntime) {
    Assert-WingetAvailable
    Install-WingetPackage `
        -Id 'Microsoft.DotNet.AspNetCore.10' `
        -DisplayName 'ASP.NET Core Runtime 10 x64'
}
else {
    Write-Warning 'ASP.NET Core Runtime installation was skipped. Use only with a self-contained deployment.'
}

$twinCatService = Get-Service -Name 'TcSysSrv' -ErrorAction SilentlyContinue
if ($null -eq $twinCatService) {
    Write-Warning 'TwinCAT System Service was not found. Install the approved Beckhoff ADS Router/runtime manually.'
}
else {
    Write-Host "[OK] TwinCAT System Service: $($twinCatService.Status)" -ForegroundColor Green
}

$adsDllPath = 'C:\TwinCAT\AdsApi\.NET\v4.0.30319\TwinCAT.Ads.dll'
if (Test-Path -LiteralPath $adsDllPath) {
    Write-Host "[OK] TwinCAT ADS installation detected: $adsDllPath" -ForegroundColor Green
}
else {
    Write-Warning 'TwinCAT ADS installation was not detected at the legacy path. Validate the ADS Router and route manually.'
}

if ($SqlServerHost) {
    Write-Host "[TEST] SQL Server connectivity: ${SqlServerHost}:$SqlServerPort" -ForegroundColor Cyan
    $sqlConnectionTest = Test-NetConnection `
        -ComputerName $SqlServerHost `
        -Port $SqlServerPort `
        -InformationLevel Detailed

    if (-not $sqlConnectionTest.TcpTestSucceeded) {
        throw "Unable to reach SQL Server at ${SqlServerHost}:$SqlServerPort."
    }

    Write-Host '[OK] SQL Server TCP port is reachable.' -ForegroundColor Green
}

Write-Host ''
Write-Host 'Server preparation completed.' -ForegroundColor Green
Write-Host 'Preparacao do servidor concluida.' -ForegroundColor Green
Write-Host ''
Write-Host 'The script did not modify ADS routes, SQL credentials, firewall rules, or Windows services.'
Write-Host 'O script nao alterou rotas ADS, credenciais SQL, firewall ou servicos do Windows.'

