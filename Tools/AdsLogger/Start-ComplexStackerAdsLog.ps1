[CmdletBinding()]
param(
    [string]$AmsNetId = '192.168.30.79.1.1',

    [ValidateRange(1, 65535)]
    [int]$AdsPort = 851,

    [ValidateRange(50, 60000)]
    [int]$SampleIntervalMs = 100,

    [ValidateRange(0, 86400)]
    [int]$DurationSeconds = 0,

    [ValidateRange(250, 60000)]
    [int]$AdsTimeoutMs = 2000,

    [ValidateRange(250, 60000)]
    [int]$ReconnectIntervalMs = 2000,

    [string]$OutputDirectory = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot 'Logs'
}


#==============================================================
# EN: Read-only ADS logger for the upper complex stacker.
# PT: Logger ADS somente leitura para o stacker superior complexo.
#==============================================================
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

$baseSymbolPath = 'MAIN.instFBUpperStackerControl'
$invariantCulture = [Globalization.CultureInfo]::InvariantCulture


#==============================================================
# EN: Symbols are relative to the complex stacker FB instance.
#     SumSymbolRead reads all resolved symbols in one ADS request.
# PT: Os simbolos sao relativos a instancia do FB do stacker complexo.
#     SumSymbolRead le todos os simbolos resolvidos em uma requisicao ADS.
#==============================================================
$symbolSuffixes = @(
    # EN: System and trigger conditions.
    # PT: Condicoes do sistema e gatilhos.
    'systemRunEnable',
    'safetyCircuitOk',
    'startCmd',
    'stopCmd',
    'packageCompleteCmd',
    'rPackageCompleteDischarge',
    'synchronizedConveyorsRunning',
    'sheetStackSeparationFinished',
    'sheetConveyor4SeparationFinished',

    # EN: Discharge sequence.
    # PT: Sequencia de descarte.
    'sheetStackDischargeControlState',
    'sheetStackDischargeState',
    'sheetStackDischargeStateCode',
    'sheetStackDischargePending',
    'sheetStackDischargeActive',
    'sheetStackDischargeStarted',
    'sheetStackDischargeFinished',
    'sheetStackDischargeFault',
    'sheetStackDischargeFaultCode',
    'sheetStackDischargeFaultCondition',
    'sheetStackDischargeLastCycleOk',
    'sheetStackDischargeLastResultCode',
    'sheetStackDischargeLastFaultStepCode',
    'sheetStackDischargeLastFaultCode',
    'sheetStackDischargeCycleHasManualSkip',
    'sheetStackDischargeCycleHasTimeout',
    'sheetStackDischargeConveyorRunRequest',
    'sheetStackDischargeConveyorRun',
    'sheetStackDischargeConveyorSpeedRefMPM',
    'sheetStackDischargeConveyorDriveOk',
    'sheetStackDischargeConveyorFault',
    'sheetStackDischargeConveyorFaultCode',
    'elevatorStackPresentAtDischargeStart',
    'elevatorPlatformClearDetected',
    'tonSheetStackDischargeStepTimeout.Q',
    'tonSheetStackDischargeStepTimeout.ET',
    'tonSheetStackDischarge.Q',
    'tonSheetStackDischarge.ET',
    'tonSheetStackDischargePlatformClearTimeout.Q',
    'tonSheetStackDischargePlatformClearTimeout.ET',

    # EN: Elevator state, position controller and final outputs.
    # PT: Estado do elevador, controlador de posicao e saidas finais.
    'elevatorControlState',
    'elevatorState',
    'elevatorAutoCmd',
    'elevatorHomeCmd',
    'elevatorHomed',
    'elevatorReadyForAutomaticCycle',
    'elevatorTargetOverrideActive',
    'elevatorTargetOverridePositionMm',
    'elevatorPositionMm',
    'elevatorTargetPositionMm',
    'elevatorPositionErrorMm',
    'elevatorAbsPositionErrorMm',
    'elevatorInPosition',
    'elevatorAtMinPosition',
    'elevatorAtMaxPosition',
    'elevatorRawSpeedRefMmps',
    'elevatorSignedSpeedRefMmps',
    'elevatorSpeedRefMmps',
    'elevatorRunUp',
    'elevatorRunDown',
    'elevatorMoveRequest',
    'elevatorDriveEnable',
    'elevatorBrakeReleaseRequest',
    'elevatorBrakeRelease',
    'tonElevatorBrakeReleaseDelay.Q',
    'tonElevatorBrakeReleaseDelay.ET',

    # EN: Elevator permissions, commands and protections.
    # PT: Permissoes, comandos e protecoes do elevador.
    'elevatorUpAllowed',
    'elevatorDownAllowed',
    'elevatorManualCommandArmed',
    'elevatorManualCommandRequest',
    'elevatorUpCmd',
    'elevatorDownCmd',
    'elevatorDriveOk',
    'elevatorFault',
    'elevatorFaultCode',
    'elevatorUpperLimit',
    'elevatorLowerLimit',
    'elevatorUpperSafetyLimit',
    'elevatorLowerSafetyLimit',
    'backstopMaterialInterlockSensor',
    'backstopStackLevelSensor',
    'elevatorStackPresentSensor',
    'elevatorStackExitClearSensor',

    # EN: Backstop participation and downstream buffer conditions.
    # PT: Participacao do backstop e condicoes do buffer de saida.
    'backstopAutoCmd',
    'backstopRequiredForAutomaticOperation',
    'backstopDriveOk',
    'backstopHomed',
    'backstopInPosition',
    'backstopFault',
    'backstopTargetOverrideActive',
    'stackBufferFull',
    'stackBufferConveyor1StackPresentSensor',
    'stackBufferConveyor2StackPresentSensor',
    'stackBufferConveyor3StackPresentSensor',
    'stackBufferConveyor1Run',
    'stackBufferConveyor2Run',
    'stackBufferConveyor3Run',
    'lineSpeedReductionRequest',

    # EN: Relevant configuration values captured with each sample.
    # PT: Valores de configuracao relevantes capturados em cada amostra.
    'machineConfig.elevatorMinPositionMm',
    'machineConfig.elevatorMaxPositionMm',
    'machineConfig.elevatorPositionToleranceMm',
    'machineConfig.elevatorPositionKp',
    'machineConfig.elevatorAutomaticUpSpeedMmps',
    'machineConfig.elevatorAutomaticDownSpeedMmps',
    'machineConfig.elevatorLevelingSpeed1Mmps',
    'machineConfig.elevatorLevelingSpeed2Mmps'
)

$derivedColumnNames = @(
    'Derived.PlatformClear',
    'Derived.ElevatorState30Reason',
    'Derived.DischargeWaitReason',
    'Derived.Anomaly'
)

$eventSuffixes = @(
    'packageCompleteCmd',
    'sheetStackDischargeControlState',
    'sheetStackDischargePending',
    'sheetStackDischargeActive',
    'sheetStackDischargeFault',
    'sheetStackDischargeFaultCode',
    'sheetStackDischargeConveyorRunRequest',
    'sheetStackDischargeConveyorRun',
    'elevatorControlState',
    'elevatorState',
    'elevatorAutoCmd',
    'elevatorHomed',
    'elevatorReadyForAutomaticCycle',
    'elevatorTargetOverrideActive',
    'elevatorTargetOverridePositionMm',
    'elevatorPositionMm',
    'elevatorTargetPositionMm',
    'elevatorInPosition',
    'elevatorSignedSpeedRefMmps',
    'elevatorSpeedRefMmps',
    'elevatorUpAllowed',
    'elevatorDownAllowed',
    'elevatorBrakeRelease',
    'elevatorFault',
    'elevatorFaultCode',
    'elevatorUpperLimit',
    'elevatorLowerLimit',
    'elevatorUpperSafetyLimit',
    'elevatorLowerSafetyLimit',
    'backstopMaterialInterlockSensor',
    'backstopStackLevelSensor',
    'elevatorStackPresentSensor',
    'elevatorStackExitClearSensor',
    'elevatorPlatformClearDetected',
    'stackBufferFull',
    'Derived.ElevatorState30Reason',
    'Derived.DischargeWaitReason',
    'Derived.Anomaly'
)


function Resolve-ChildSymbol {
    param(
        $RootSymbol,
        [string]$RelativePath
    )

    $currentSymbol = $RootSymbol

    foreach ($segment in $RelativePath.Split('.')) {
        $nextSymbol = $null

        foreach ($candidate in $currentSymbol.SubSymbols) {
            if ($candidate.InstanceName -eq $segment) {
                $nextSymbol = $candidate
                break
            }
        }

        if ($null -eq $nextSymbol) {
            return $null
        }

        $currentSymbol = $nextSymbol
    }

    return $currentSymbol
}


function New-AdsSession {
    param(
        [string]$TargetAmsNetId,
        [int]$TargetAdsPort,
        [int]$TimeoutMs,
        [string]$RootPath,
        [string[]]$RequestedSuffixes
    )

    $client = New-Object TwinCAT.Ads.TcAdsClient
    $loader = $null

    try {
        $client.Timeout = $TimeoutMs
        $client.Connect($TargetAmsNetId, $TargetAdsPort)
        $adsStateInfo = $client.ReadState()

        $loaderSettings = New-Object TwinCAT.Ads.SymbolLoaderSettings(
            [TwinCAT.SymbolsLoadMode]::VirtualTree
        )

        $loader = [TwinCAT.Ads.TypeSystem.SymbolLoaderFactory]::Create(
            $client,
            $loaderSettings
        )

        $mainSymbol = $loader.Symbols |
            Where-Object { $_.InstancePath -eq 'MAIN' } |
            Select-Object -First 1

        if ($null -eq $mainSymbol) {
            throw 'MAIN symbol was not found / Simbolo MAIN nao foi encontrado.'
        }

        $rootRelativePath = $RootPath.Substring('MAIN.'.Length)
        $rootSymbol = Resolve-ChildSymbol -RootSymbol $mainSymbol -RelativePath $rootRelativePath

        if ($null -eq $rootSymbol) {
            throw ("FB instance was not found / Instancia do FB nao foi encontrada: {0}" -f $RootPath)
        }

        $resolvedSymbols = New-Object 'System.Collections.Generic.List[TwinCAT.TypeSystem.ISymbol]'
        $resolvedSuffixes = New-Object 'System.Collections.Generic.List[string]'
        $missingSuffixes = New-Object 'System.Collections.Generic.List[string]'

        foreach ($suffix in $RequestedSuffixes) {
            $symbol = Resolve-ChildSymbol -RootSymbol $rootSymbol -RelativePath $suffix

            if ($null -eq $symbol) {
                [void]$missingSuffixes.Add($suffix)
                continue
            }

            [void]$resolvedSymbols.Add($symbol)
            [void]$resolvedSuffixes.Add($suffix)
        }

        if ($resolvedSymbols.Count -eq 0) {
            throw 'No diagnostic symbols were resolved / Nenhum simbolo de diagnostico foi resolvido.'
        }

        $sumReader = New-Object TwinCAT.Ads.SumCommand.SumSymbolRead(
            $client,
            $resolvedSymbols
        )

        return [pscustomobject]@{
            Client           = $client
            Loader           = $loader
            Reader           = $sumReader
            ResolvedSuffixes = [string[]]$resolvedSuffixes.ToArray()
            MissingSuffixes  = [string[]]$missingSuffixes.ToArray()
            AdsState         = $adsStateInfo.AdsState.ToString()
        }
    }
    catch {
        if ($null -ne $loader) {
            $loader.Dispose()
        }

        $client.Dispose()
        throw
    }
}


function Close-AdsSession {
    param($Session)

    if ($null -eq $Session) {
        return
    }

    if ($null -ne $Session.Loader) {
        $Session.Loader.Dispose()
    }

    if ($null -ne $Session.Client) {
        $Session.Client.Dispose()
    }
}


function Get-SampleValue {
    param(
        $Sample,
        [string]$Name
    )

    if ($Sample.Contains($Name)) {
        return $Sample[$Name]
    }

    return $null
}


function ConvertTo-LogValue {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }

    if ($Value -is [bool]) {
        if ($Value) {
            return 'TRUE'
        }

        return 'FALSE'
    }

    if ($Value -is [TimeSpan]) {
        return $Value.TotalMilliseconds.ToString('0.###', $invariantCulture)
    }

    if ($Value -is [IFormattable]) {
        return $Value.ToString($null, $invariantCulture)
    }

    return $Value.ToString()
}


function ConvertTo-CsvField {
    param($Value)

    $textValue = ConvertTo-LogValue -Value $Value

    if ($textValue.IndexOfAny([char[]]@(';', '"', "`r", "`n")) -ge 0) {
        return '"' + $textValue.Replace('"', '""') + '"'
    }

    return $textValue
}


function Get-PlatformClear {
    param($Sample)

    $stackPresentAtStart = Get-SampleValue $Sample 'elevatorStackPresentAtDischargeStart'
    $stackExitClear = Get-SampleValue $Sample 'elevatorStackExitClearSensor'
    $stackPresent = Get-SampleValue $Sample 'elevatorStackPresentSensor'

    return (
        (-not [bool]$stackPresentAtStart) -or
        ([bool]$stackExitClear -and (-not [bool]$stackPresent))
    )
}


function Get-ElevatorState30Reason {
    param($Sample)

    $controlState = Get-SampleValue $Sample 'elevatorControlState'

    if (($null -eq $controlState) -or ([int]$controlState -ne 30)) {
        return ''
    }

    if ([bool](Get-SampleValue $Sample 'elevatorFault')) {
        return 'FAULT_FLAG_BLOCKS_OUTPUT'
    }

    if (-not [bool](Get-SampleValue $Sample 'elevatorAutoCmd')) {
        return 'AUTO_DISABLED_EXPECT_STATE_0'
    }

    if (-not [bool](Get-SampleValue $Sample 'elevatorHomed')) {
        return 'AUTO_NO_HOME'
    }

    if (-not [bool](Get-SampleValue $Sample 'elevatorReadyForAutomaticCycle')) {
        return 'AUTO_NOT_READY'
    }

    if ([bool](Get-SampleValue $Sample 'elevatorTargetOverrideActive')) {
        return 'OVERRIDE_ACTIVE_EXPECT_STATE_40'
    }

    $manualCommandActive =
        [bool](Get-SampleValue $Sample 'elevatorUpCmd') -or
        [bool](Get-SampleValue $Sample 'elevatorDownCmd')

    if ($manualCommandActive -and
        (-not [bool](Get-SampleValue $Sample 'elevatorManualCommandArmed'))) {
        return 'MANUAL_COMMAND_WAIT_RELEASE'
    }

    if (-not [bool](Get-SampleValue $Sample 'backstopStackLevelSensor')) {
        return 'WAIT_LEVEL_SENSOR'
    }

    if (-not [bool](Get-SampleValue $Sample 'elevatorDownAllowed')) {
        if ([bool](Get-SampleValue $Sample 'elevatorLowerSafetyLimit')) {
            return 'DOWN_BLOCKED_LOWER_SAFETY_LIMIT'
        }

        if ([bool](Get-SampleValue $Sample 'elevatorLowerLimit')) {
            return 'DOWN_BLOCKED_LOWER_LIMIT'
        }

        return 'DOWN_NOT_ALLOWED'
    }

    return 'LEVELING_DOWN'
}


function Get-DischargeWaitReason {
    param($Sample)

    $stateValue = Get-SampleValue $Sample 'sheetStackDischargeControlState'

    if ($null -eq $stateValue) {
        return 'STATE_UNAVAILABLE'
    }

    switch ([int]$stateValue) {
        0 { return 'IDLE' }
        5 { return 'WAIT_SEPARATION' }
        10 { return 'START_POSITIONING' }
        20 {
            $waiting = New-Object 'System.Collections.Generic.List[string]'

            if ([bool](Get-SampleValue $Sample 'backstopRequiredForAutomaticOperation') -and
                (-not [bool](Get-SampleValue $Sample 'backstopInPosition'))) {
                [void]$waiting.Add('BACKSTOP_POSITION')
            }

            if (-not [bool](Get-SampleValue $Sample 'elevatorInPosition')) {
                [void]$waiting.Add('ELEVATOR_POSITION')
            }

            if ($waiting.Count -eq 0) {
                return 'POSITION_READY'
            }

            return 'WAIT_' + ($waiting -join '_AND_')
        }
        30 {
            if (-not [bool](Get-SampleValue $Sample 'sheetStackDischargeConveyorRun')) {
                return 'WAIT_DISCHARGE_CONVEYOR_RUN'
            }

            if (-not [bool](Get-SampleValue $Sample 'tonSheetStackDischarge.Q')) {
                return 'WAIT_MINIMUM_DISCHARGE_TIME'
            }

            if (-not [bool](Get-SampleValue $Sample 'elevatorPlatformClearDetected')) {
                return 'WAIT_PLATFORM_CLEAR'
            }

            return 'READY_TO_RETURN'
        }
        40 { return 'START_RETURN' }
        50 {
            $waiting = New-Object 'System.Collections.Generic.List[string]'

            if ([bool](Get-SampleValue $Sample 'backstopRequiredForAutomaticOperation') -and
                (-not [bool](Get-SampleValue $Sample 'backstopInPosition'))) {
                [void]$waiting.Add('BACKSTOP_RETURN')
            }

            if (-not [bool](Get-SampleValue $Sample 'elevatorInPosition')) {
                [void]$waiting.Add('ELEVATOR_RETURN')
            }

            if ($waiting.Count -eq 0) {
                return 'RETURN_READY'
            }

            return 'WAIT_' + ($waiting -join '_AND_')
        }
        60 { return 'FINISH' }
        90 { return 'FAULT_HOLD' }
        default { return 'UNKNOWN_STATE_' + [int]$stateValue }
    }
}


function Get-Anomaly {
    param($Sample)

    $dischargeState = Get-SampleValue $Sample 'sheetStackDischargeControlState'
    $elevatorState = Get-SampleValue $Sample 'elevatorControlState'
    $overrideActive = [bool](Get-SampleValue $Sample 'elevatorTargetOverrideActive')
    $elevatorHomed = [bool](Get-SampleValue $Sample 'elevatorHomed')
    $elevatorFault = [bool](Get-SampleValue $Sample 'elevatorFault')

    if (($null -ne $dischargeState) -and
        ([int]$dischargeState -in @(20, 30, 50)) -and
        $overrideActive -and
        $elevatorHomed -and
        (-not $elevatorFault) -and
        ($null -ne $elevatorState) -and
        ([int]$elevatorState -ne 40)) {
        return 'OVERRIDE_ACTIVE_BUT_ELEVATOR_NOT_STATE_40'
    }

    if (($null -ne $elevatorState) -and
        ([int]$elevatorState -eq 30) -and
        $overrideActive -and
        (-not $elevatorHomed)) {
        return 'OVERRIDE_BLOCKED_ELEVATOR_NOT_HOMED'
    }

    if (($null -ne $dischargeState) -and
        ([int]$dischargeState -eq 30) -and
        [bool](Get-SampleValue $Sample 'stackBufferFull')) {
        return 'DISCHARGE_WITH_FULL_STACK_BUFFER'
    }

    return ''
}


function Write-EventLine {
    param(
        [IO.StreamWriter]$Writer,
        [string]$Timestamp,
        [string]$EventType,
        [string]$Name,
        $OldValue,
        $NewValue
    )

    $Writer.WriteLine(
        "{0}`t{1}`t{2}`t{3}`t{4}",
        $Timestamp,
        $EventType,
        $Name,
        (ConvertTo-LogValue $OldValue),
        (ConvertTo-LogValue $NewValue)
    )
}


#==============================================================
# EN: Prepare output files before connecting so connection errors are logged.
# PT: Prepara os arquivos antes da conexao para registrar erros de comunicacao.
#==============================================================
[void](New-Item -ItemType Directory -Path $OutputDirectory -Force)

$fileTimestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$csvPath = Join-Path $OutputDirectory ("complex-stacker-{0}.csv" -f $fileTimestamp)
$eventPath = Join-Path $OutputDirectory ("complex-stacker-{0}.events.txt" -f $fileTimestamp)
$utf8NoBom = New-Object Text.UTF8Encoding($false)
$csvWriter = New-Object IO.StreamWriter($csvPath, $false, $utf8NoBom)
$eventWriter = New-Object IO.StreamWriter($eventPath, $false, $utf8NoBom)
$csvWriter.AutoFlush = $true
$eventWriter.AutoFlush = $true

$headerColumns = @(
    'TimestampLocal',
    'TimestampUtc',
    'ElapsedMs',
    'SampleId',
    'AdsState',
    'ReadDurationMs'
) + $symbolSuffixes + $derivedColumnNames

$csvWriter.WriteLine(
    (($headerColumns | ForEach-Object { ConvertTo-CsvField $_ }) -join ';')
)

$eventWriter.WriteLine("TimestampLocal`tEventType`tName`tOldValue`tNewValue")

$session = $null
$previousEventValues = @{}
$loggerStopwatch = [Diagnostics.Stopwatch]::StartNew()
$captureStarted = $false
$sampleId = 0L
$nextHeartbeatMs = 0L
$nextConsoleUpdateMs = 0L

Write-Host ("ADS logger / Logger ADS: {0}:{1}" -f $AmsNetId, $AdsPort)
Write-Host ("CSV: {0}" -f $csvPath)
Write-Host ("Events / Eventos: {0}" -f $eventPath)
Write-Host 'Press Ctrl+C to stop / Pressione Ctrl+C para parar.'

try {
    while (($DurationSeconds -eq 0) -or
           (-not $captureStarted) -or
           ($loggerStopwatch.Elapsed.TotalSeconds -lt $DurationSeconds)) {

        if ($null -eq $session) {
            try {
                $session = New-AdsSession `
                    -TargetAmsNetId $AmsNetId `
                    -TargetAdsPort $AdsPort `
                    -TimeoutMs $AdsTimeoutMs `
                    -RootPath $baseSymbolPath `
                    -RequestedSuffixes $symbolSuffixes

                $nowText = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffK')
                Write-EventLine $eventWriter $nowText 'ADS_CONNECTED' '<ADS>' '' $session.AdsState

                foreach ($missingSuffix in $session.MissingSuffixes) {
                    Write-EventLine $eventWriter $nowText 'SYMBOL_MISSING' $missingSuffix '' ''
                }

                Write-Host (
                    "Connected / Conectado: State={0}, Symbols={1}, Missing={2}" -f
                    $session.AdsState,
                    $session.ResolvedSuffixes.Count,
                    $session.MissingSuffixes.Count
                )

                if (-not $captureStarted) {
                    $loggerStopwatch.Restart()
                    $captureStarted = $true
                }

                $nextHeartbeatMs = $loggerStopwatch.ElapsedMilliseconds + 1000
            }
            catch {
                $nowText = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffK')
                Write-EventLine $eventWriter $nowText 'ADS_CONNECT_ERROR' '<ADS>' '' $_.Exception.Message
                Write-Warning ("ADS connection failed / Falha na conexao ADS: {0}" -f $_.Exception.Message)
                Start-Sleep -Milliseconds $ReconnectIntervalMs
                continue
            }
        }

        $cycleStartMs = $loggerStopwatch.ElapsedMilliseconds

        try {
            if ($loggerStopwatch.ElapsedMilliseconds -ge $nextHeartbeatMs) {
                $adsStateInfo = $session.Client.ReadState()
                $session.AdsState = $adsStateInfo.AdsState.ToString()
                $nextHeartbeatMs = $loggerStopwatch.ElapsedMilliseconds + 1000
            }

            $readStopwatch = [Diagnostics.Stopwatch]::StartNew()
            $readValues = $session.Reader.Read()
            $readStopwatch.Stop()

            $sample = [ordered]@{}

            foreach ($suffix in $symbolSuffixes) {
                $sample[$suffix] = $null
            }

            for ($index = 0; $index -lt $session.ResolvedSuffixes.Count; $index++) {
                $sample[$session.ResolvedSuffixes[$index]] = $readValues[$index]
            }

            $sample['Derived.PlatformClear'] = Get-PlatformClear $sample
            $sample['Derived.ElevatorState30Reason'] = Get-ElevatorState30Reason $sample
            $sample['Derived.DischargeWaitReason'] = Get-DischargeWaitReason $sample
            $sample['Derived.Anomaly'] = Get-Anomaly $sample

            $now = Get-Date
            $timestampLocal = $now.ToString('yyyy-MM-ddTHH:mm:ss.fffK')
            $timestampUtc = $now.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            $sampleId++

            $rowValues = @(
                $timestampLocal,
                $timestampUtc,
                $loggerStopwatch.ElapsedMilliseconds,
                $sampleId,
                $session.AdsState,
                $readStopwatch.Elapsed.TotalMilliseconds
            )

            foreach ($suffix in $symbolSuffixes) {
                $rowValues += $sample[$suffix]
            }

            foreach ($derivedName in $derivedColumnNames) {
                $rowValues += $sample[$derivedName]
            }

            $csvWriter.WriteLine(
                (($rowValues | ForEach-Object { ConvertTo-CsvField $_ }) -join ';')
            )

            foreach ($eventSuffix in $eventSuffixes) {
                $currentEventValue = $sample[$eventSuffix]

                if (-not $previousEventValues.ContainsKey($eventSuffix)) {
                    Write-EventLine $eventWriter $timestampLocal 'INITIAL' $eventSuffix '' $currentEventValue
                    $previousEventValues[$eventSuffix] = $currentEventValue
                    continue
                }

                $oldText = ConvertTo-LogValue $previousEventValues[$eventSuffix]
                $newText = ConvertTo-LogValue $currentEventValue

                if ($oldText -ne $newText) {
                    Write-EventLine `
                        $eventWriter `
                        $timestampLocal `
                        'CHANGE' `
                        $eventSuffix `
                        $previousEventValues[$eventSuffix] `
                        $currentEventValue

                    $previousEventValues[$eventSuffix] = $currentEventValue
                }
            }

            if ($loggerStopwatch.ElapsedMilliseconds -ge $nextConsoleUpdateMs) {
                Write-Host (
                    "{0} D={1} E={2} Pos={3} Target={4} InPos={5} Reason={6} Anomaly={7}" -f
                    $timestampLocal,
                    (ConvertTo-LogValue $sample['sheetStackDischargeControlState']),
                    (ConvertTo-LogValue $sample['elevatorControlState']),
                    (ConvertTo-LogValue $sample['elevatorPositionMm']),
                    (ConvertTo-LogValue $sample['elevatorTargetPositionMm']),
                    (ConvertTo-LogValue $sample['elevatorInPosition']),
                    (ConvertTo-LogValue $sample['Derived.DischargeWaitReason']),
                    (ConvertTo-LogValue $sample['Derived.Anomaly'])
                )

                $nextConsoleUpdateMs = $loggerStopwatch.ElapsedMilliseconds + 1000
            }
        }
        catch {
            $nowText = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffK')
            Write-EventLine $eventWriter $nowText 'ADS_READ_ERROR' '<ADS>' '' $_.Exception.Message
            Write-Warning ("ADS read failed; reconnecting / Leitura ADS falhou; reconectando: {0}" -f $_.Exception.Message)

            Close-AdsSession $session
            $session = $null
            Start-Sleep -Milliseconds $ReconnectIntervalMs
            continue
        }

        $cycleDurationMs = $loggerStopwatch.ElapsedMilliseconds - $cycleStartMs
        $remainingDelayMs = $SampleIntervalMs - $cycleDurationMs

        if ($remainingDelayMs -gt 0) {
            Start-Sleep -Milliseconds $remainingDelayMs
        }
    }
}
finally {
    Close-AdsSession $session
    $csvWriter.Dispose()
    $eventWriter.Dispose()

    Write-Host ("Stopped / Finalizado. Samples / Amostras: {0}" -f $sampleId)
    Write-Host ("CSV: {0}" -f $csvPath)
    Write-Host ("Events / Eventos: {0}" -f $eventPath)
}
