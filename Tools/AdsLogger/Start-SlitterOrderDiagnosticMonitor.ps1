[CmdletBinding()]
param(
    [string]$AmsNetId = '192.168.30.79.1.1',
    [ValidateRange(1, 65535)] [int]$AdsPort = 851,
    [ValidateRange(250, 60000)] [int]$PollIntervalMs = 1000,
    [ValidateRange(250, 60000)] [int]$StabilityTimeMs = 1000,
    [ValidateRange(1000, 3600000)] [int]$PositioningTimeoutMs = 120000,
    [ValidateRange(250, 60000)] [int]$ReconnectIntervalMs = 3000,
    [string]$MonitorLogPath = '',
    [switch]$CaptureInitialOrder
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$diagnosticScript = Join-Path $PSScriptRoot 'Get-SlitterToolPositionDiagnostic.ps1'
$variationScript = Join-Path $PSScriptRoot 'Get-SlitterToolVariationReport.ps1'
$baseSymbolPath = 'MAIN.instFBSlitter1Control'

if ([string]::IsNullOrWhiteSpace($MonitorLogPath)) {
    $MonitorLogPath = Join-Path $PSScriptRoot 'Logs\slitter-order-monitor.log'
}

[void](New-Item -ItemType Directory -Path (Split-Path -Parent $MonitorLogPath) -Force)
Start-Transcript -LiteralPath $MonitorLogPath -Append | Out-Null

if (-not (Test-Path -LiteralPath $diagnosticScript)) {
    throw "Diagnostic script was not found / Script de diagnostico nao encontrado: $diagnosticScript"
}

$adsDllCandidates = @(
    'C:\TwinCAT\AdsApi\.NET\v4.0.30319\TwinCAT.Ads.dll',
    'C:\TwinCAT\3.1\Components\Base\Addins\TcSmEL6821Extension\TwinCAT.Ads.dll'
)

$adsDllPath = $adsDllCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($adsDllPath)) {
    throw 'TwinCAT.Ads.dll was not found / TwinCAT.Ads.dll nao foi encontrada.'
}

[void][Reflection.Assembly]::LoadFrom($adsDllPath)

# EN: Only these values are polled continuously. The complete diagnostic is
#     executed only after a stable order change is detected.
# PT: Somente estes valores sao lidos continuamente. O diagnostico completo
#     roda apenas depois que uma alteracao estavel do pedido for detectada.
$requestedSymbols = [ordered]@{
    ProductionListNumber = '::currentOrder.productionListNumber'
    LevelSelector = '::currentOrder.levelSelector'
    InvertOrderLevel = '::currentOrder.invertOrderLevel'
    InvertOrderSide = '::currentOrder.invertOrderSide'
    Order1SheetType = '::currentOrder.order1.sheetType'
    Order1Quantity = '::currentOrder.order1.sheetQuantity'
    Order1M1 = '::currentOrder.order1.sheetM1'
    Order1M2 = '::currentOrder.order1.sheetM2'
    Order1M3 = '::currentOrder.order1.sheetM3'
    Order1M4 = '::currentOrder.order1.sheetM4'
    Order1M5 = '::currentOrder.order1.sheetM5'
    Order2SheetType = '::currentOrder.order2.sheetType'
    Order2Quantity = '::currentOrder.order2.sheetQuantity'
    Order2M1 = '::currentOrder.order2.sheetM1'
    Order2M2 = '::currentOrder.order2.sheetM2'
    Order2M3 = '::currentOrder.order2.sheetM3'
    Order2M4 = '::currentOrder.order2.sheetM4'
    Order2M5 = '::currentOrder.order2.sheetM5'
    PositioningStarted = 'positioningStarted'
    PositioningRunning = 'positioningRunning'
}

function Resolve-ChildSymbol {
    param($RootSymbol, [string]$RelativePath)

    $currentSymbol = $RootSymbol

    foreach ($segment in $RelativePath.Split('.')) {
        $currentSymbol = $currentSymbol.SubSymbols |
            Where-Object { $_.InstanceName -eq $segment } |
            Select-Object -First 1

        if ($null -eq $currentSymbol) { return $null }
    }

    return $currentSymbol
}

function Resolve-AbsoluteSymbol {
    param($RootSymbols, [string]$AbsolutePath)

    $segments = $AbsolutePath.Split('.')
    $currentSymbol = $RootSymbols |
        Where-Object { $_.InstanceName -eq $segments[0] } |
        Select-Object -First 1

    if ($null -eq $currentSymbol) { return $null }

    for ($index = 1; $index -lt $segments.Count; $index++) {
        $currentSymbol = $currentSymbol.SubSymbols |
            Where-Object { $_.InstanceName -eq $segments[$index] } |
            Select-Object -First 1

        if ($null -eq $currentSymbol) { return $null }
    }

    return $currentSymbol
}

function Open-AdsSession {
    $client = New-Object TwinCAT.Ads.TcAdsClient
    $loader = $null

    try {
        $client.Timeout = 3000
        $client.Connect($AmsNetId, $AdsPort)
        $loaderSettings = New-Object TwinCAT.Ads.SymbolLoaderSettings([TwinCAT.SymbolsLoadMode]::VirtualTree)
        $loader = [TwinCAT.Ads.TypeSystem.SymbolLoaderFactory]::Create($client, $loaderSettings)
        $mainSymbol = $loader.Symbols | Where-Object { $_.InstancePath -eq 'MAIN' } | Select-Object -First 1

        if ($null -eq $mainSymbol) { throw 'MAIN symbol was not found.' }

        $rootSymbol = Resolve-ChildSymbol $mainSymbol $baseSymbolPath.Substring('MAIN.'.Length)
        if ($null -eq $rootSymbol) { throw "Slitter FB was not found: $baseSymbolPath" }

        $symbols = [ordered]@{}

        foreach ($entry in $requestedSymbols.GetEnumerator()) {
            $path = [string]$entry.Value
            $symbol = if ($path.StartsWith('::')) {
                Resolve-AbsoluteSymbol $loader.Symbols $path.Substring(2)
            }
            else {
                Resolve-ChildSymbol $rootSymbol $path
            }

            if ($null -eq $symbol) { throw "Required ADS symbol was not found: $($entry.Key) ($path)" }
            $symbols[$entry.Key] = $symbol
        }

        return [pscustomobject]@{ Client = $client; Loader = $loader; Symbols = $symbols }
    }
    catch {
        if ($null -ne $loader) { $loader.Dispose() }
        $client.Dispose()
        throw
    }
}

function Close-AdsSession {
    param($Session)
    if ($null -eq $Session) { return }
    if ($null -ne $Session.Loader) { $Session.Loader.Dispose() }
    if ($null -ne $Session.Client) { $Session.Client.Dispose() }
}

function Read-OrderSample {
    param($Session)

    $sample = [ordered]@{}

    foreach ($entry in $Session.Symbols.GetEnumerator()) {
        $singleSymbol = New-Object 'System.Collections.Generic.List[TwinCAT.TypeSystem.ISymbol]'
        [void]$singleSymbol.Add($entry.Value)
        $reader = New-Object TwinCAT.Ads.SumCommand.SumSymbolRead($Session.Client, $singleSymbol)
        $values = $reader.Read()
        $sample[$entry.Key] = $values[0]
    }

    return $sample
}

function Get-OrderSignature {
    param([Collections.IDictionary]$Sample)

    $signatureNames = @(
        'LevelSelector', 'InvertOrderLevel', 'InvertOrderSide',
        'Order1SheetType', 'Order1Quantity', 'Order1M1', 'Order1M2', 'Order1M3', 'Order1M4', 'Order1M5',
        'Order2SheetType', 'Order2Quantity', 'Order2M1', 'Order2M2', 'Order2M3', 'Order2M4', 'Order2M5'
    )

    return ($signatureNames | ForEach-Object {
        [Convert]::ToString($Sample[$_], [Globalization.CultureInfo]::InvariantCulture)
    }) -join '|'
}

function Invoke-FullDiagnostic {
    param([string]$Reason)

    Write-Host "[$((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))] Running diagnostic / Executando diagnostico: $Reason"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $diagnosticScript -AmsNetId $AmsNetId -AdsPort $AdsPort

    if ($LASTEXITCODE -ne 0) {
        throw "Diagnostic returned exit code $LASTEXITCODE"
    }

    if (Test-Path -LiteralPath $variationScript) {
        Write-Host 'Updating variation history / Atualizando historico de variacao.'
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $variationScript

        if ($LASTEXITCODE -ne 0) {
            throw "Variation report returned exit code $LASTEXITCODE"
        }
    }
}

$session = $null
$lastProcessedSignature = $null
$candidateSignature = $null
$candidateSince = $null
$candidateDeadline = $null
$captureInitialPending = [bool]$CaptureInitialOrder

Write-Host 'Slitter order diagnostic monitor / Monitor de diagnostico do pedido'
Write-Host "ADS: ${AmsNetId}:${AdsPort} | Poll: ${PollIntervalMs}ms | Stability: ${StabilityTimeMs}ms"
Write-Host 'Press Ctrl+C to stop / Pressione Ctrl+C para parar.'

try {
    while ($true) {
        if ($null -eq $session) {
            try {
                $session = Open-AdsSession
                Write-Host "[$((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))] ADS connected / ADS conectado."
            }
            catch {
                Write-Warning "ADS connection failed / Falha ADS: $($_.Exception.Message)"
                Start-Sleep -Milliseconds $ReconnectIntervalMs
                continue
            }
        }

        try {
            $sample = Read-OrderSample $session
            $signature = Get-OrderSignature $sample
            $now = Get-Date

            if ($null -eq $lastProcessedSignature) {
                if ($captureInitialPending) {
                    $candidateSignature = $signature
                    $candidateSince = $now
                    $candidateDeadline = $now.AddMilliseconds($PositioningTimeoutMs)
                    $captureInitialPending = $false
                    Write-Host "Initial order queued / Pedido inicial agendado: $($sample.ProductionListNumber)"
                }
                else {
                    $lastProcessedSignature = $signature
                    Write-Host "Baseline registered / Pedido base registrado: $($sample.ProductionListNumber)"
                }
            }
            elseif (($signature -ne $lastProcessedSignature) -and ($signature -ne $candidateSignature)) {
                $candidateSignature = $signature
                $candidateSince = $now
                $candidateDeadline = $now.AddMilliseconds($PositioningTimeoutMs)
                Write-Host "Order change detected / Alteracao detectada: $($sample.ProductionListNumber)"
            }

            if ($null -ne $candidateSignature) {
                if ($signature -ne $candidateSignature) {
                    $candidateSignature = $signature
                    $candidateSince = $now
                    $candidateDeadline = $now.AddMilliseconds($PositioningTimeoutMs)
                    Write-Host 'Order changed again; stability timer restarted / Pedido mudou novamente.'
                }

                $stable = (($now - $candidateSince).TotalMilliseconds -ge $StabilityTimeMs)
                $positioningFinished = (-not [bool]$sample.PositioningStarted) -and (-not [bool]$sample.PositioningRunning)
                $timedOut = $now -ge $candidateDeadline

                if ($stable -and ($positioningFinished -or $timedOut)) {
                    $reason = if ($timedOut -and (-not $positioningFinished)) { 'POSITIONING_TIMEOUT' } else { 'ORDER_CHANGED_AND_STABLE' }
                    Invoke-FullDiagnostic $reason
                    $lastProcessedSignature = $candidateSignature
                    $candidateSignature = $null
                    $candidateSince = $null
                    $candidateDeadline = $null
                }
            }
        }
        catch {
            Write-Warning "ADS read/diagnostic failed; reconnecting / Falha; reconectando: $($_.Exception.Message)"
            Close-AdsSession $session
            $session = $null
            Start-Sleep -Milliseconds $ReconnectIntervalMs
            continue
        }

        Start-Sleep -Milliseconds $PollIntervalMs
    }
}
finally {
    Close-AdsSession $session
    Stop-Transcript | Out-Null
}
