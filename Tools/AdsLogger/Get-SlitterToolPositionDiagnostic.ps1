[CmdletBinding()]
param(
    [string]$AmsNetId = '192.168.30.79.1.1',

    [ValidateRange(1, 65535)]
    [int]$AdsPort = 851,

    [ValidateRange(0.0, 100.0)]
    [double]$PositionToleranceMm = 0.5,

    [ValidateRange(0.0, 10.0)]
    [double]$GenerationToleranceMm = 0.01,

    [ValidateRange(250, 60000)]
    [int]$AdsTimeoutMs = 3000,

    [string]$OutputDirectory = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot 'Logs'
}

$baseSymbolPath = 'MAIN.instFBSlitter1Control'
$invariantCulture = [Globalization.CultureInfo]::InvariantCulture


#==============================================================
# EN: Load the TwinCAT ADS .NET library already installed locally.
# PT: Carrega a biblioteca .NET ADS já instalada localmente.
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


#==============================================================
# EN: Diagnostic symbols relative to the slitter FB instance.
# PT: Símbolos de diagnóstico relativos à instância do FB do slitter.
#==============================================================
$requestedSymbols = [ordered]@{
    'Order.ProductionListNumber' = '::currentOrder.productionListNumber'
    'Order.LevelSelector' = '::currentOrder.levelSelector'
    'Order.InvertOrderLevel' = '::currentOrder.invertOrderLevel'
    'Order.InvertOrderSide' = '::currentOrder.invertOrderSide'

    'Order1.SheetType' = '::currentOrder.order1.sheetType'
    'Order1.SheetQuantity' = '::currentOrder.order1.sheetQuantity'
    'Order1.M1' = '::currentOrder.order1.sheetM1'
    'Order1.M2' = '::currentOrder.order1.sheetM2'
    'Order1.M3' = '::currentOrder.order1.sheetM3'
    'Order1.M4' = '::currentOrder.order1.sheetM4'
    'Order1.M5' = '::currentOrder.order1.sheetM5'

    'Order2.SheetType' = '::currentOrder.order2.sheetType'
    'Order2.SheetQuantity' = '::currentOrder.order2.sheetQuantity'
    'Order2.M1' = '::currentOrder.order2.sheetM1'
    'Order2.M2' = '::currentOrder.order2.sheetM2'
    'Order2.M3' = '::currentOrder.order2.sheetM3'
    'Order2.M4' = '::currentOrder.order2.sheetM4'
    'Order2.M5' = '::currentOrder.order2.sheetM5'

    'Generated.NumberOfKnives' = '::currentOrder.generatedOrder.numberOfKnifes'
    'Generated.NumberOfScorers' = '::currentOrder.generatedOrder.numberOfScorers'
    'Generated.NumberOfSheets' = '::currentOrder.generatedOrder.numberOfSheets'
    'Generated.Order1WidthMm' = '::currentOrder.generatedOrder.order1Width'
    'Generated.Order2WidthMm' = '::currentOrder.generatedOrder.order2Width'
    'Generated.TotalWidthMm' = '::currentOrder.generatedOrder.orderTotalWidth'
    'Generated.FirstKnifePositionMm' = '::currentOrder.generatedOrder.firstKnifePosition'
    'Generated.LastKnifePositionMm' = '::currentOrder.generatedOrder.lastKnifePosition'
    'Generated.OrderNotOk' = '::currentOrder.generatedOrder.statusWord.orderNotOk'

    'Config.MachineWidthMm' = 'machineConfig.machineWidth'

    'Control.EnableAxis' = 'enableAxis'
    'Control.HomeFinished' = 'homeFinished'
    'Control.PositioningStarted' = 'positioningStarted'
    'Control.PositioningRunning' = 'positioningRunning'
    'Control.InTargetPosition' = 'inTargetPosition'
    'Control.AllKnivesInPosition' = 'bAllKnivesInPos'
    'Control.HaltAxis' = 'haltAxis'

    'Sensor.LimitKnife3_4' = 'limitSensorKnife3_4'
    'Sensor.LimitScorer3_5' = 'limitSensorScorer3_5'
    'Sensor.LimitScorer4_6' = 'limitSensorScorer4_6'
}

for ($knifeIndex = 1; $knifeIndex -le 10; $knifeIndex++) {
    $requestedSymbols["Generated.KnifeEnabled.$knifeIndex"] =
        "::currentOrder.generatedOrder.knifeEnabledArr.knifeEnabledArr[$knifeIndex]"

    if ($knifeIndex -le 5) {
        $requestedSymbols["Generated.KnifePosition.$knifeIndex"] =
            "::currentOrder.generatedOrder.knifePositionReferenceArr.knifePositionReferenceArr[$knifeIndex]"
        $requestedSymbols["Config.KnifeOffset.$knifeIndex"] =
            "machineConfig.knifePositionCorrectionArr.knifePositionCorrectionArr[$knifeIndex]"
        $requestedSymbols["Config.KnifeHomePosition.$knifeIndex"] =
            "machineConfig.homePositionKnife.homePositionKnife[$knifeIndex]"
        $requestedSymbols["Config.KnifeMinPosition.$knifeIndex"] =
            "machineConfig.knifeMinPostionArr.knifeMinPostionArr[$knifeIndex]"
        $requestedSymbols["Config.KnifeMaxPosition.$knifeIndex"] =
            "machineConfig.knifeMaxPostionArr.knifeMaxPostionArr[$knifeIndex]"
    }
}

for ($knifeIndex = 1; $knifeIndex -le 5; $knifeIndex++) {
    $requestedSymbols["Knife$knifeIndex.CommandedPositionMm"] =
        "knife${knifeIndex}_AxisConfiguration.posAbsolute.position"
    $requestedSymbols["Knife$knifeIndex.ActualPositionMm"] =
        "::SLT1_P1_100_UG${knifeIndex}.NcToPlc.ActPos"
    $requestedSymbols["Knife$knifeIndex.Homed"] =
        "::SLT1_P1_100_UG${knifeIndex}.Status.Homed"
    $requestedSymbols["Knife$knifeIndex.NotMoving"] =
        "::SLT1_P1_100_UG${knifeIndex}.Status.NotMoving"
    $requestedSymbols["Knife$knifeIndex.InTargetPosition"] =
        "::SLT1_P1_100_UG${knifeIndex}.Status.InTargetPosition"
    $requestedSymbols["Knife$knifeIndex.HomeSensor"] =
        "homeSensorKnife$knifeIndex"
    $requestedSymbols["Knife$knifeIndex.CommandBusy"] =
        "instFB_AxisControlKnife${knifeIndex}_Axis.commandBusy"
    $requestedSymbols["Knife$knifeIndex.CommandDone"] =
        "instFB_AxisControlKnife${knifeIndex}_Axis.commandDone"
    $requestedSymbols["Knife$knifeIndex.CommandAborted"] =
        "instFB_AxisControlKnife${knifeIndex}_Axis.commandAborted"
    $requestedSymbols["Knife$knifeIndex.AxisError"] =
        "instFB_AxisControlKnife${knifeIndex}_Axis.error"
    $requestedSymbols["Knife$knifeIndex.AxisStatus"] =
        "instFB_AxisControlKnife${knifeIndex}_Axis.status"
    $requestedSymbols["Knife$knifeIndex.RelativeMoveSelected"] =
        "instFB_AxisControlKnife${knifeIndex}_Axis.posRelativeSelected"
    $requestedSymbols["Knife$knifeIndex.JogSelected"] =
        "instFB_AxisControlKnife${knifeIndex}_Axis.jogSelected"
}

for ($scorerIndex = 1; $scorerIndex -le 8; $scorerIndex++) {
    $requestedSymbols["Generated.ScorerEnabled.$scorerIndex"] =
        "::currentOrder.generatedOrder.scorerEnabledArr.scorerEnabledArr[$scorerIndex]"
    $requestedSymbols["Generated.ScorerPosition.$scorerIndex"] =
        "::currentOrder.generatedOrder.scorerPositionReferenceArr.scorerPositionReferenceArr[$scorerIndex]"
    $requestedSymbols["Config.ScorerOffset.$scorerIndex"] =
        "machineConfig.scorerPositionCorrectionArr.scorerPositionCorrectionArr[$scorerIndex]"
    $requestedSymbols["Config.ScorerHomePosition.$scorerIndex"] =
        "machineConfig.homePositionScorer.homePositionScorer[$scorerIndex]"
    $requestedSymbols["Config.ScorerMinPosition.$scorerIndex"] =
        "machineConfig.scorerMinPostionArr.scorerMinPostionArr[$scorerIndex]"
    $requestedSymbols["Config.ScorerMaxPosition.$scorerIndex"] =
        "machineConfig.scorerMaxPostionArr.scorerMaxPostionArr[$scorerIndex]"
    $requestedSymbols["Scorer$scorerIndex.CommandedPositionMm"] =
        "scorer${scorerIndex}_AxisConfiguration.posAbsolute.position"
    $requestedSymbols["Scorer$scorerIndex.ActualPositionMm"] =
        "::SLT1_P1_200_UG${scorerIndex}.NcToPlc.ActPos"
    $requestedSymbols["Scorer$scorerIndex.Homed"] =
        "::SLT1_P1_200_UG${scorerIndex}.Status.Homed"
    $requestedSymbols["Scorer$scorerIndex.NotMoving"] =
        "::SLT1_P1_200_UG${scorerIndex}.Status.NotMoving"
    $requestedSymbols["Scorer$scorerIndex.InTargetPosition"] =
        "::SLT1_P1_200_UG${scorerIndex}.Status.InTargetPosition"
    $requestedSymbols["Scorer$scorerIndex.CommandBusy"] =
        "instFB_AxisControlScorer${scorerIndex}_Axis.commandBusy"
    $requestedSymbols["Scorer$scorerIndex.CommandDone"] =
        "instFB_AxisControlScorer${scorerIndex}_Axis.commandDone"
    $requestedSymbols["Scorer$scorerIndex.CommandAborted"] =
        "instFB_AxisControlScorer${scorerIndex}_Axis.commandAborted"
    $requestedSymbols["Scorer$scorerIndex.AxisError"] =
        "instFB_AxisControlScorer${scorerIndex}_Axis.error"
    $requestedSymbols["Scorer$scorerIndex.AxisStatus"] =
        "instFB_AxisControlScorer${scorerIndex}_Axis.status"
    $requestedSymbols["Scorer$scorerIndex.RelativeMoveSelected"] =
        "instFB_AxisControlScorer${scorerIndex}_Axis.posRelativeSelected"
    $requestedSymbols["Scorer$scorerIndex.JogSelected"] =
        "instFB_AxisControlScorer${scorerIndex}_Axis.jogSelected"
}

for ($scorerIndex = 1; $scorerIndex -le 8; $scorerIndex++) {
    $requestedSymbols["Sensor.Scorer${scorerIndex}Home"] =
        "homeSensorScorer$scorerIndex"
}

$requiredSymbols = @(
    'Order.LevelSelector',
    'Order1.SheetType',
    'Order1.SheetQuantity',
    'Order1.M1',
    'Order1.M2',
    'Order1.M3',
    'Order1.M4',
    'Order1.M5',
    'Order2.SheetType',
    'Order2.SheetQuantity',
    'Order2.M1',
    'Order2.M2',
    'Order2.M3',
    'Order2.M4',
    'Order2.M5',
    'Generated.NumberOfKnives',
    'Generated.NumberOfScorers',
    'Config.MachineWidthMm'
)

for ($knifeIndex = 1; $knifeIndex -le 5; $knifeIndex++) {
    $requiredSymbols += "Generated.KnifeEnabled.$knifeIndex"
    $requiredSymbols += "Generated.KnifePosition.$knifeIndex"
    $requiredSymbols += "Config.KnifeOffset.$knifeIndex"
    $requiredSymbols += "Config.KnifeHomePosition.$knifeIndex"
    $requiredSymbols += "Config.KnifeMinPosition.$knifeIndex"
    $requiredSymbols += "Config.KnifeMaxPosition.$knifeIndex"
    $requiredSymbols += "Knife$knifeIndex.CommandedPositionMm"
    $requiredSymbols += "Knife$knifeIndex.ActualPositionMm"
}

for ($scorerIndex = 1; $scorerIndex -le 8; $scorerIndex++) {
    $requiredSymbols += "Generated.ScorerEnabled.$scorerIndex"
    $requiredSymbols += "Generated.ScorerPosition.$scorerIndex"
    $requiredSymbols += "Config.ScorerOffset.$scorerIndex"
    $requiredSymbols += "Config.ScorerHomePosition.$scorerIndex"
    $requiredSymbols += "Config.ScorerMinPosition.$scorerIndex"
    $requiredSymbols += "Config.ScorerMaxPosition.$scorerIndex"
    $requiredSymbols += "Scorer$scorerIndex.CommandedPositionMm"
    $requiredSymbols += "Scorer$scorerIndex.ActualPositionMm"
}


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


function Resolve-AbsoluteSymbol {
    param(
        $RootSymbols,
        [string]$AbsolutePath
    )

    $segments = $AbsolutePath.Split('.')
    $currentSymbol = $RootSymbols |
        Where-Object { $_.InstanceName -eq $segments[0] } |
        Select-Object -First 1

    if ($null -eq $currentSymbol) {
        return $null
    }

    for ($index = 1; $index -lt $segments.Count; $index++) {
        $nextSymbol = $currentSymbol.SubSymbols |
            Where-Object { $_.InstanceName -eq $segments[$index] } |
            Select-Object -First 1

        if ($null -eq $nextSymbol) {
            return $null
        }

        $currentSymbol = $nextSymbol
    }

    return $currentSymbol
}


function Get-SampleValue {
    param(
        [Collections.IDictionary]$Sample,
        [string]$Name,
        $DefaultValue = $null
    )

    if ($Sample.Contains($Name) -and ($null -ne $Sample[$Name])) {
        return $Sample[$Name]
    }

    return $DefaultValue
}


function Get-ArrayValue {
    param(
        $ArrayValue,
        [int]$PlcIndex,
        $DefaultValue = $null
    )

    if ($null -eq $ArrayValue) {
        return $DefaultValue
    }

    if ($ArrayValue -is [Array]) {
        $zeroBasedIndex = $PlcIndex - 1

        if (($zeroBasedIndex -ge 0) -and ($zeroBasedIndex -lt $ArrayValue.Length)) {
            return $ArrayValue.GetValue($zeroBasedIndex)
        }
    }

    if ($ArrayValue -is [Collections.IList]) {
        $zeroBasedIndex = $PlcIndex - 1

        if (($zeroBasedIndex -ge 0) -and ($zeroBasedIndex -lt $ArrayValue.Count)) {
            return $ArrayValue[$zeroBasedIndex]
        }
    }

    return $DefaultValue
}


function ConvertTo-DoubleValue {
    param($Value)

    if ($null -eq $Value) {
        return [double]::NaN
    }

    return [Convert]::ToDouble($Value, $invariantCulture)
}


function Get-SheetWidthMm {
    param(
        [int]$SheetType,
        [double[]]$Measures
    )

    switch ($SheetType) {
        0 { return $Measures[0] }
        1 { return $Measures[0] + $Measures[1] + $Measures[2] }
        2 { return $Measures[0] + $Measures[1] + $Measures[2] + $Measures[3] + $Measures[4] }
        default { return $Measures[0] }
    }
}


function Format-Mm {
    param($Value)

    if (($null -eq $Value) -or [double]::IsNaN([double]$Value)) {
        return ''
    }

    return ([double]$Value).ToString('0.000', $invariantCulture)
}


function Format-CompactMm {
    param($Value)

    if (($null -eq $Value) -or [double]::IsNaN([double]$Value)) {
        return '-'
    }

    return ([double]$Value).ToString('0.###', $invariantCulture)
}


function Format-OrderComposition {
    param(
        [bool]$Enabled,
        [int]$Quantity,
        [int]$SheetType,
        [double[]]$Measures
    )

    if ((-not $Enabled) -or ($Quantity -le 0)) {
        return 'INACTIVE / INATIVO'
    }

    $measureCount = switch ($SheetType) {
        0 { 1 }
        1 { 3 }
        2 { 5 }
        default { 1 }
    }

    $measureText = @(
        for ($index = 0; $index -lt $measureCount; $index++) {
            Format-CompactMm $Measures[$index]
        }
    ) -join ' * '

    return "$Quantity x $measureText"
}


function Format-LogValue {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }

    if ($Value -is [bool]) {
        if ($Value) { return 'TRUE' }
        return 'FALSE'
    }

    if ($Value -is [IFormattable]) {
        return $Value.ToString($null, $invariantCulture)
    }

    return $Value.ToString()
}


#==============================================================
# EN: Connect and resolve the symbols once.
# PT: Conecta e resolve os símbolos uma única vez.
#==============================================================
$client = $null
$loader = $null
$sumReader = $null

try {
    $client = New-Object TwinCAT.Ads.TcAdsClient
    $client.Timeout = $AdsTimeoutMs
    $client.Connect($AmsNetId, $AdsPort)
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

    $rootRelativePath = $baseSymbolPath.Substring('MAIN.'.Length)
    $rootSymbol = Resolve-ChildSymbol -RootSymbol $mainSymbol -RelativePath $rootRelativePath

    if ($null -eq $rootSymbol) {
        throw "Slitter FB instance was not found: $baseSymbolPath"
    }

    $resolvedSymbols = New-Object 'System.Collections.Generic.List[TwinCAT.TypeSystem.ISymbol]'
    $resolvedNames = New-Object 'System.Collections.Generic.List[string]'
    $missingNames = New-Object 'System.Collections.Generic.List[string]'

    foreach ($entry in $requestedSymbols.GetEnumerator()) {
        $requestedPath = [string]$entry.Value

        if ($requestedPath.StartsWith('::')) {
            $symbol = Resolve-AbsoluteSymbol `
                -RootSymbols $loader.Symbols `
                -AbsolutePath $requestedPath.Substring(2)
        }
        else {
            $symbol = Resolve-ChildSymbol -RootSymbol $rootSymbol -RelativePath $requestedPath
        }

        if ($null -eq $symbol) {
            [void]$missingNames.Add([string]$entry.Key)
            continue
        }

        [void]$resolvedSymbols.Add($symbol)
        [void]$resolvedNames.Add([string]$entry.Key)
    }

    $missingRequired = @($requiredSymbols | Where-Object { $_ -in $missingNames })

    if ($missingRequired.Count -gt 0) {
        foreach ($arrayPath in @(
            'currentOrder.generatedOrder.knifeEnabledArr',
            'currentOrder.generatedOrder.knifePositionReferenceArr',
            'machineConfig.knifePositionCorrectionArr',
            'machineConfig.knifeMinPostionArr',
            'machineConfig.knifeMaxPostionArr'
        )) {
            $arraySymbol = Resolve-ChildSymbol -RootSymbol $rootSymbol -RelativePath $arrayPath

            if ($null -ne $arraySymbol) {
                $childNames = @($arraySymbol.SubSymbols | ForEach-Object { $_.InstanceName })
                Write-Host ("Array symbol {0}; children: {1}" -f $arrayPath, ($childNames -join ', '))
            }
        }

        throw ('Required ADS symbols were not found: ' + ($missingRequired -join ', '))
    }

    $readStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $sample = [ordered]@{}

    foreach ($name in $requestedSymbols.Keys) {
        $sample[$name] = $null
    }

    $readFailureNames = New-Object 'System.Collections.Generic.List[string]'

    for ($index = 0; $index -lt $resolvedNames.Count; $index++) {
        $symbolName = $resolvedNames[$index]
        $singleSymbol = New-Object 'System.Collections.Generic.List[TwinCAT.TypeSystem.ISymbol]'
        [void]$singleSymbol.Add($resolvedSymbols[$index])

        try {
            $singleReader = New-Object TwinCAT.Ads.SumCommand.SumSymbolRead(
                $client,
                $singleSymbol
            )
            $singleValue = $singleReader.Read()
            $sample[$symbolName] = $singleValue[0]
        }
        catch {
            [void]$readFailureNames.Add($symbolName)
            Write-Warning "ADS read skipped for '$symbolName' ($($resolvedSymbols[$index].InstancePath)): $($_.Exception.Message)"
        }
    }

    $readStopwatch.Stop()

    $failedRequired = @($requiredSymbols | Where-Object { $_ -in $readFailureNames })

    if ($failedRequired.Count -gt 0) {
        throw ('Required ADS values could not be read: ' + ($failedRequired -join ', '))
    }

    $timestamp = Get-Date


    #==============================================================
    # EN: Independently calculate nominal knife boundaries from order measures.
    # PT: Calcula independentemente os limites nominais pelas medidas do pedido.
    #==============================================================
    $order1Measures = [double[]]@(
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order1.M1' 0)),
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order1.M2' 0)),
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order1.M3' 0)),
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order1.M4' 0)),
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order1.M5' 0))
    )

    $order2Measures = [double[]]@(
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order2.M1' 0)),
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order2.M2' 0)),
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order2.M3' 0)),
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order2.M4' 0)),
        (ConvertTo-DoubleValue (Get-SampleValue $sample 'Order2.M5' 0))
    )

    $levelSelector = [int](Get-SampleValue $sample 'Order.LevelSelector' 1)
    $useOrder1 = $true
    $useOrder2 = ($levelSelector -eq 3)

    $order1SheetType = [int](Get-SampleValue $sample 'Order1.SheetType' 0)
    $order2SheetType = [int](Get-SampleValue $sample 'Order2.SheetType' 0)
    $order1Quantity = if ($useOrder1) { [Math]::Max(0, [int](Get-SampleValue $sample 'Order1.SheetQuantity' 0)) } else { 0 }
    $order2Quantity = if ($useOrder2) { [Math]::Max(0, [int](Get-SampleValue $sample 'Order2.SheetQuantity' 0)) } else { 0 }

    $order1SheetWidthMm = Get-SheetWidthMm $order1SheetType $order1Measures
    $order2SheetWidthMm = Get-SheetWidthMm $order2SheetType $order2Measures
    $calculatedOrder1WidthMm = $order1SheetWidthMm * $order1Quantity
    $calculatedOrder2WidthMm = $order2SheetWidthMm * $order2Quantity
    $calculatedTotalWidthMm = $calculatedOrder1WidthMm + $calculatedOrder2WidthMm
    $machineWidthMm = ConvertTo-DoubleValue (Get-SampleValue $sample 'Config.MachineWidthMm')
    $calculatedLeftTrimMm = ($machineWidthMm - $calculatedTotalWidthMm) / 2.0

    $calculatedBoundaries = New-Object 'System.Collections.Generic.List[double]'
    $currentBoundaryMm = $calculatedLeftTrimMm
    [void]$calculatedBoundaries.Add($currentBoundaryMm)

    for ($index = 1; $index -le $order1Quantity; $index++) {
        $currentBoundaryMm += $order1SheetWidthMm
        [void]$calculatedBoundaries.Add($currentBoundaryMm)
    }

    for ($index = 1; $index -le $order2Quantity; $index++) {
        $currentBoundaryMm += $order2SheetWidthMm
        [void]$calculatedBoundaries.Add($currentBoundaryMm)
    }


    #==============================================================
    # EN: Build active knife ranking from generated references.
    # PT: Ordena as facas ativas pelas referências geradas.
    #==============================================================
    $activeKnives = @()

    for ($knifeIndex = 1; $knifeIndex -le 5; $knifeIndex++) {
        $used = [bool](Get-SampleValue $sample "Generated.KnifeEnabled.$knifeIndex" $false)
        $generatedPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Generated.KnifePosition.$knifeIndex")

        if ($used) {
            $activeKnives += [pscustomobject]@{
                KnifeIndex = $knifeIndex
                GeneratedPositionMm = $generatedPositionMm
            }
        }
    }

    $activeKnives = @($activeKnives | Sort-Object GeneratedPositionMm)
    $boundaryByKnife = @{}

    for ($rank = 0; $rank -lt $activeKnives.Count; $rank++) {
        if ($rank -lt $calculatedBoundaries.Count) {
            $boundaryByKnife[$activeKnives[$rank].KnifeIndex] = $calculatedBoundaries[$rank]
        }
    }

    #==============================================================
    # EN: Independently calculate crease positions from sheet measures.
    # PT: Calcula independentemente as posições dos vincos pelas medidas.
    #==============================================================
    $calculatedScorerPositions = New-Object 'System.Collections.Generic.List[double]'
    $currentSheetStartMm = $calculatedLeftTrimMm

    foreach ($orderDefinition in @(
        [pscustomobject]@{ Enabled = $useOrder1; Quantity = $order1Quantity; SheetType = $order1SheetType; Measures = $order1Measures },
        [pscustomobject]@{ Enabled = $useOrder2; Quantity = $order2Quantity; SheetType = $order2SheetType; Measures = $order2Measures }
    )) {
        if (-not $orderDefinition.Enabled) { continue }

        for ($sheetIndex = 1; $sheetIndex -le $orderDefinition.Quantity; $sheetIndex++) {
            $positionMm = $currentSheetStartMm

            for ($measureIndex = 0; $measureIndex -lt 5; $measureIndex++) {
                $positionMm += [double]$orderDefinition.Measures[$measureIndex]

                if ((($orderDefinition.SheetType -eq 1) -and ($measureIndex -lt 2)) -or
                    (($orderDefinition.SheetType -eq 2) -and ($measureIndex -lt 4))) {
                    [void]$calculatedScorerPositions.Add($positionMm)
                }
            }

            $currentSheetStartMm += (Get-SheetWidthMm $orderDefinition.SheetType $orderDefinition.Measures)
        }
    }

    $activeScorers = @()

    for ($scorerIndex = 1; $scorerIndex -le 8; $scorerIndex++) {
        $used = [bool](Get-SampleValue $sample "Generated.ScorerEnabled.$scorerIndex" $false)
        $generatedPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Generated.ScorerPosition.$scorerIndex")

        if ($used) {
            $activeScorers += [pscustomobject]@{
                ScorerIndex = $scorerIndex
                GeneratedPositionMm = $generatedPositionMm
            }
        }
    }

    $activeScorers = @($activeScorers | Sort-Object GeneratedPositionMm)
    $positionByScorer = @{}

    for ($rank = 0; $rank -lt $activeScorers.Count; $rank++) {
        if ($rank -lt $calculatedScorerPositions.Count) {
            $positionByScorer[$activeScorers[$rank].ScorerIndex] = $calculatedScorerPositions[$rank]
        }
    }


    #==============================================================
    # EN: Calculate final diagnostics for each physical knife.
    # PT: Calcula o diagnóstico final para cada faca física.
    #==============================================================
    $rows = New-Object 'System.Collections.Generic.List[object]'

    for ($knifeIndex = 1; $knifeIndex -le 5; $knifeIndex++) {
        $used = [bool](Get-SampleValue $sample "Generated.KnifeEnabled.$knifeIndex" $false)
        $generatedPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Generated.KnifePosition.$knifeIndex")
        $offsetMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Config.KnifeOffset.$knifeIndex" 0.0)
        $homePositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Config.KnifeHomePosition.$knifeIndex")
        $expectedAxisPositionMm = $generatedPositionMm + $offsetMm
        $commandedPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Knife$knifeIndex.CommandedPositionMm")
        $actualPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Knife$knifeIndex.ActualPositionMm")
        $targetAssignmentErrorMm = $commandedPositionMm - $expectedAxisPositionMm
        $positionErrorMm = $actualPositionMm - $expectedAxisPositionMm
        $minPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Config.KnifeMinPosition.$knifeIndex")
        $maxPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Config.KnifeMaxPosition.$knifeIndex")
        $calculatedFromOrderMm = [double]::NaN

        if ($boundaryByKnife.ContainsKey($knifeIndex)) {
            $calculatedFromOrderMm = [double]$boundaryByKnife[$knifeIndex]
        }

        $generationErrorMm = $generatedPositionMm - $calculatedFromOrderMm
        $issues = New-Object 'System.Collections.Generic.List[string]'
        $homeSensor = [bool](Get-SampleValue $sample "Knife$knifeIndex.HomeSensor" $false)
        $sharedLimitSensor = $false

        if ($knifeIndex -in @(3, 4)) {
            $sharedLimitSensor = [bool](Get-SampleValue $sample 'Sensor.LimitKnife3_4' $false)
        }

        if ($used) {
            if ([double]::IsNaN($calculatedFromOrderMm)) {
                [void]$issues.Add('ORDER_BOUNDARY_MISSING')
            }
            elseif ([Math]::Abs($generationErrorMm) -gt ($GenerationToleranceMm + 0.000001)) {
                [void]$issues.Add('ORDER_GENERATION_MISMATCH')
            }

            if (($expectedAxisPositionMm -lt $minPositionMm) -or
                ($expectedAxisPositionMm -gt $maxPositionMm)) {
                [void]$issues.Add('CORRECTED_TARGET_OUTSIDE_LIMIT')
            }

            if ([Math]::Abs($targetAssignmentErrorMm) -gt ($GenerationToleranceMm + 0.000001)) {
                [void]$issues.Add('TARGET_ASSIGNMENT_MISMATCH')
            }

            if ([bool](Get-SampleValue $sample "Knife$knifeIndex.AxisError" $false)) {
                [void]$issues.Add('AXIS_ERROR')
            }

            if (-not [bool](Get-SampleValue $sample "Knife$knifeIndex.Homed" $false)) {
                [void]$issues.Add('NOT_HOMED')
            }

            if ([bool](Get-SampleValue $sample "Knife$knifeIndex.CommandBusy" $false) -or
                (-not [bool](Get-SampleValue $sample "Knife$knifeIndex.NotMoving" $false))) {
                [void]$issues.Add('MOVING_OR_COMMAND_BUSY')
            }
            elseif ([Math]::Abs($positionErrorMm) -gt ($PositionToleranceMm + 0.000001)) {
                if ($homeSensor -or $sharedLimitSensor -or
                    [bool](Get-SampleValue $sample 'Control.HaltAxis' $false)) {
                    [void]$issues.Add('STOPPED_OUTSIDE_TARGET_SENSOR_OR_HALT')
                }
                else {
                    [void]$issues.Add('STOPPED_OUTSIDE_TARGET')
                }
            }

            if (-not [bool](Get-SampleValue $sample "Knife$knifeIndex.InTargetPosition" $false)) {
                [void]$issues.Add('NC_NOT_IN_TARGET_POSITION')
            }

            if ([bool](Get-SampleValue $sample "Knife$knifeIndex.RelativeMoveSelected" $false)) {
                [void]$issues.Add('RELATIVE_CORRECTION_ACTIVE')
            }

            if ([bool](Get-SampleValue $sample "Knife$knifeIndex.JogSelected" $false)) {
                [void]$issues.Add('JOG_ACTIVE')
            }
        }

        $result = if (-not $used) {
            'NOT_USED'
        }
        elseif ($issues.Count -eq 0) {
            'OK'
        }
        else {
            $issues -join '|'
        }

        $rows.Add([pscustomobject][ordered]@{
            Timestamp = $timestamp.ToString('yyyy-MM-ddTHH:mm:ss.fffK')
            ProductionListNumber = Get-SampleValue $sample 'Order.ProductionListNumber' ''
            Tool = "Knife$knifeIndex"
            Used = $used
            CalculatedFromOrderMm = Format-Mm $calculatedFromOrderMm
            GeneratedPositionMm = Format-Mm $generatedPositionMm
            GenerationErrorMm = if ($used) { Format-Mm $generationErrorMm } else { '' }
            ConfiguredOffsetMm = Format-Mm $offsetMm
            CurrentHomePositionMm = Format-Mm $homePositionMm
            ProposedZeroOffsetHomeMm = Format-Mm ($homePositionMm - $offsetMm)
            ExpectedAxisPositionMm = Format-Mm $expectedAxisPositionMm
            CommandedPositionMm = Format-Mm $commandedPositionMm
            TargetAssignmentErrorMm = Format-Mm $targetAssignmentErrorMm
            ActualPositionMm = Format-Mm $actualPositionMm
            PositionErrorMm = Format-Mm $positionErrorMm
            AbsPositionErrorMm = Format-Mm ([Math]::Abs($positionErrorMm))
            MinPositionMm = Format-Mm $minPositionMm
            MaxPositionMm = Format-Mm $maxPositionMm
            Homed = Get-SampleValue $sample "Knife$knifeIndex.Homed" ''
            NotMoving = Get-SampleValue $sample "Knife$knifeIndex.NotMoving" ''
            InTargetPosition = Get-SampleValue $sample "Knife$knifeIndex.InTargetPosition" ''
            CommandBusy = Get-SampleValue $sample "Knife$knifeIndex.CommandBusy" ''
            CommandDone = Get-SampleValue $sample "Knife$knifeIndex.CommandDone" ''
            CommandAborted = Get-SampleValue $sample "Knife$knifeIndex.CommandAborted" ''
            AxisError = Get-SampleValue $sample "Knife$knifeIndex.AxisError" ''
            AxisStatus = Get-SampleValue $sample "Knife$knifeIndex.AxisStatus" ''
            HomeSensor = $homeSensor
            SharedLimitSensor = $sharedLimitSensor
            RelativeMoveSelected = Get-SampleValue $sample "Knife$knifeIndex.RelativeMoveSelected" ''
            JogSelected = Get-SampleValue $sample "Knife$knifeIndex.JogSelected" ''
            Result = $result
        })
    }

    #==============================================================
    # EN: Calculate final diagnostics for each physical scorer.
    # PT: Calcula o diagnóstico final para cada vincador físico.
    #==============================================================
    for ($scorerIndex = 1; $scorerIndex -le 8; $scorerIndex++) {
        $prefix = "Scorer$scorerIndex"
        $used = [bool](Get-SampleValue $sample "Generated.ScorerEnabled.$scorerIndex" $false)
        $generatedPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Generated.ScorerPosition.$scorerIndex")
        $offsetMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Config.ScorerOffset.$scorerIndex" 0.0)
        $homePositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Config.ScorerHomePosition.$scorerIndex")
        $expectedAxisPositionMm = $generatedPositionMm + $offsetMm
        $commandedPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "$prefix.CommandedPositionMm")
        $actualPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "$prefix.ActualPositionMm")
        $targetAssignmentErrorMm = $commandedPositionMm - $expectedAxisPositionMm
        $positionErrorMm = $actualPositionMm - $expectedAxisPositionMm
        $minPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Config.ScorerMinPosition.$scorerIndex")
        $maxPositionMm = ConvertTo-DoubleValue (Get-SampleValue $sample "Config.ScorerMaxPosition.$scorerIndex")
        $calculatedFromOrderMm = [double]::NaN

        if ($positionByScorer.ContainsKey($scorerIndex)) {
            $calculatedFromOrderMm = [double]$positionByScorer[$scorerIndex]
        }

        $generationErrorMm = $generatedPositionMm - $calculatedFromOrderMm
        $issues = New-Object 'System.Collections.Generic.List[string]'
        $homeSensor = [bool](Get-SampleValue $sample "Sensor.Scorer${scorerIndex}Home" $false)
        $sharedLimitSensor = $false

        if ($scorerIndex -in @(3, 5)) {
            $sharedLimitSensor = [bool](Get-SampleValue $sample 'Sensor.LimitScorer3_5' $false)
        }
        elseif ($scorerIndex -in @(4, 6)) {
            $sharedLimitSensor = [bool](Get-SampleValue $sample 'Sensor.LimitScorer4_6' $false)
        }

        if ($used) {
            if ([double]::IsNaN($calculatedFromOrderMm)) {
                [void]$issues.Add('ORDER_CREASE_POSITION_MISSING')
            }
            elseif ([Math]::Abs($generationErrorMm) -gt ($GenerationToleranceMm + 0.000001)) {
                [void]$issues.Add('ORDER_GENERATION_MISMATCH')
            }

            if (($expectedAxisPositionMm -lt $minPositionMm) -or
                ($expectedAxisPositionMm -gt $maxPositionMm)) {
                [void]$issues.Add('CORRECTED_TARGET_OUTSIDE_LIMIT')
            }

            if ([Math]::Abs($targetAssignmentErrorMm) -gt ($GenerationToleranceMm + 0.000001)) {
                [void]$issues.Add('TARGET_ASSIGNMENT_MISMATCH')
            }

            if ([bool](Get-SampleValue $sample "$prefix.AxisError" $false)) {
                [void]$issues.Add('AXIS_ERROR')
            }

            if (-not [bool](Get-SampleValue $sample "$prefix.Homed" $false)) {
                [void]$issues.Add('NOT_HOMED')
            }

            if ([bool](Get-SampleValue $sample "$prefix.CommandBusy" $false) -or
                (-not [bool](Get-SampleValue $sample "$prefix.NotMoving" $false))) {
                [void]$issues.Add('MOVING_OR_COMMAND_BUSY')
            }
            elseif ([Math]::Abs($positionErrorMm) -gt ($PositionToleranceMm + 0.000001)) {
                if ($homeSensor -or $sharedLimitSensor -or
                    [bool](Get-SampleValue $sample 'Control.HaltAxis' $false)) {
                    [void]$issues.Add('STOPPED_OUTSIDE_TARGET_SENSOR_OR_HALT')
                }
                else {
                    [void]$issues.Add('STOPPED_OUTSIDE_TARGET')
                }
            }

            if (-not [bool](Get-SampleValue $sample "$prefix.InTargetPosition" $false)) {
                [void]$issues.Add('NC_NOT_IN_TARGET_POSITION')
            }

            if ([bool](Get-SampleValue $sample "$prefix.RelativeMoveSelected" $false)) {
                [void]$issues.Add('RELATIVE_CORRECTION_ACTIVE')
            }

            if ([bool](Get-SampleValue $sample "$prefix.JogSelected" $false)) {
                [void]$issues.Add('JOG_ACTIVE')
            }
        }

        $result = if (-not $used) { 'NOT_USED' } elseif ($issues.Count -eq 0) { 'OK' } else { $issues -join '|' }

        $rows.Add([pscustomobject][ordered]@{
            Timestamp = $timestamp.ToString('yyyy-MM-ddTHH:mm:ss.fffK')
            ProductionListNumber = Get-SampleValue $sample 'Order.ProductionListNumber' ''
            Tool = $prefix
            Used = $used
            CalculatedFromOrderMm = Format-Mm $calculatedFromOrderMm
            GeneratedPositionMm = Format-Mm $generatedPositionMm
            GenerationErrorMm = if ($used) { Format-Mm $generationErrorMm } else { '' }
            ConfiguredOffsetMm = Format-Mm $offsetMm
            CurrentHomePositionMm = Format-Mm $homePositionMm
            ProposedZeroOffsetHomeMm = Format-Mm ($homePositionMm - $offsetMm)
            ExpectedAxisPositionMm = Format-Mm $expectedAxisPositionMm
            CommandedPositionMm = Format-Mm $commandedPositionMm
            TargetAssignmentErrorMm = Format-Mm $targetAssignmentErrorMm
            ActualPositionMm = Format-Mm $actualPositionMm
            PositionErrorMm = Format-Mm $positionErrorMm
            AbsPositionErrorMm = Format-Mm ([Math]::Abs($positionErrorMm))
            MinPositionMm = Format-Mm $minPositionMm
            MaxPositionMm = Format-Mm $maxPositionMm
            Homed = Get-SampleValue $sample "$prefix.Homed" ''
            NotMoving = Get-SampleValue $sample "$prefix.NotMoving" ''
            InTargetPosition = Get-SampleValue $sample "$prefix.InTargetPosition" ''
            CommandBusy = Get-SampleValue $sample "$prefix.CommandBusy" ''
            CommandDone = Get-SampleValue $sample "$prefix.CommandDone" ''
            CommandAborted = Get-SampleValue $sample "$prefix.CommandAborted" ''
            AxisError = Get-SampleValue $sample "$prefix.AxisError" ''
            AxisStatus = Get-SampleValue $sample "$prefix.AxisStatus" ''
            HomeSensor = $homeSensor
            SharedLimitSensor = $sharedLimitSensor
            RelativeMoveSelected = Get-SampleValue $sample "$prefix.RelativeMoveSelected" ''
            JogSelected = Get-SampleValue $sample "$prefix.JogSelected" ''
            Result = $result
        })
    }


    #==============================================================
    # EN: Global consistency checks and final result.
    # PT: Verificações globais de consistência e resultado final.
    #==============================================================
    $globalIssues = New-Object 'System.Collections.Generic.List[string]'
    $generatedNumberOfKnives = [int](Get-SampleValue $sample 'Generated.NumberOfKnives' 0)
    $generatedNumberOfScorers = [int](Get-SampleValue $sample 'Generated.NumberOfScorers' 0)
    $enabledKnifeCount = 0
    $enabledScorerCount = 0

    for ($knifeIndex = 1; $knifeIndex -le 10; $knifeIndex++) {
        if ([bool](Get-SampleValue $sample "Generated.KnifeEnabled.$knifeIndex" $false)) {
            $enabledKnifeCount++
        }
    }

    if ($enabledKnifeCount -ne $generatedNumberOfKnives) {
        [void]$globalIssues.Add('TOOL_ENABLE_COUNT_MISMATCH')
    }

    if ($activeKnives.Count -ne $calculatedBoundaries.Count) {
        [void]$globalIssues.Add('ORDER_KNIFE_COUNT_MISMATCH')
    }

    for ($scorerIndex = 1; $scorerIndex -le 8; $scorerIndex++) {
        if ([bool](Get-SampleValue $sample "Generated.ScorerEnabled.$scorerIndex" $false)) {
            $enabledScorerCount++
        }
    }

    if ($enabledScorerCount -ne $generatedNumberOfScorers) {
        [void]$globalIssues.Add('SCORER_ENABLE_COUNT_MISMATCH')
    }

    if ($activeScorers.Count -ne $calculatedScorerPositions.Count) {
        [void]$globalIssues.Add('ORDER_SCORER_COUNT_MISMATCH')
    }

    $generatedOrder1WidthMm = ConvertTo-DoubleValue (Get-SampleValue $sample 'Generated.Order1WidthMm')
    $generatedOrder2WidthMm = ConvertTo-DoubleValue (Get-SampleValue $sample 'Generated.Order2WidthMm')
    $generatedTotalWidthMm = ConvertTo-DoubleValue (Get-SampleValue $sample 'Generated.TotalWidthMm')

    if ([Math]::Abs($generatedOrder1WidthMm - $calculatedOrder1WidthMm) -gt ($GenerationToleranceMm + 0.000001)) {
        [void]$globalIssues.Add('ORDER1_WIDTH_MISMATCH')
    }

    if ([Math]::Abs($generatedOrder2WidthMm - $calculatedOrder2WidthMm) -gt ($GenerationToleranceMm + 0.000001)) {
        [void]$globalIssues.Add('ORDER2_WIDTH_MISMATCH')
    }

    if ([Math]::Abs($generatedTotalWidthMm - $calculatedTotalWidthMm) -gt ($GenerationToleranceMm + 0.000001)) {
        [void]$globalIssues.Add('TOTAL_WIDTH_MISMATCH')
    }

    if ([bool](Get-SampleValue $sample 'Generated.OrderNotOk' $false)) {
        [void]$globalIssues.Add('PLC_ORDER_NOT_OK')
    }

    $usedToolFailures = @($rows | Where-Object { $_.Used -and ($_.Result -ne 'OK') })
    $overallResult = if (($globalIssues.Count -eq 0) -and ($usedToolFailures.Count -eq 0)) {
        'VALID'
    }
    else {
        'INVALID'
    }

    $activeSensors = New-Object 'System.Collections.Generic.List[string]'

    foreach ($name in $sample.Keys) {
        if (($name -like 'Sensor.*') -and [bool](Get-SampleValue $sample $name $false)) {
            [void]$activeSensors.Add($name)
        }
    }


    #==============================================================
    # EN: Save one CSV row per knife and a human-readable summary.
    # PT: Salva uma linha CSV por faca e um resumo legível.
    #==============================================================
    [void](New-Item -ItemType Directory -Path $OutputDirectory -Force)
    $fileTimestamp = $timestamp.ToString('yyyyMMdd-HHmmss')
    $csvPath = Join-Path $OutputDirectory "slitter-tool-position-$fileTimestamp.csv"
    $summaryPath = Join-Path $OutputDirectory "slitter-tool-position-$fileTimestamp.txt"
    $compactSummaryPath = Join-Path $OutputDirectory "slitter-tool-position-$fileTimestamp.summary.txt"

    $rows | Export-Csv -LiteralPath $csvPath -Delimiter ';' -NoTypeInformation -Encoding UTF8

    $summaryLines = New-Object 'System.Collections.Generic.List[string]'
    [void]$summaryLines.Add("Slitter tool position diagnostic / Diagnostico de posicao das ferramentas")
    [void]$summaryLines.Add("Timestamp: $($timestamp.ToString('yyyy-MM-ddTHH:mm:ss.fffK'))")
    [void]$summaryLines.Add("ADS: ${AmsNetId}:${AdsPort} State=$($adsStateInfo.AdsState)")
    [void]$summaryLines.Add("Read duration ms: $($readStopwatch.Elapsed.TotalMilliseconds.ToString('0.###', $invariantCulture))")
    [void]$summaryLines.Add("Production list: $(Format-LogValue (Get-SampleValue $sample 'Order.ProductionListNumber' ''))")
    [void]$summaryLines.Add("Level selector: $levelSelector")
    [void]$summaryLines.Add("Machine width mm: $(Format-Mm $machineWidthMm)")
    [void]$summaryLines.Add("Calculated order 1 width mm: $(Format-Mm $calculatedOrder1WidthMm)")
    [void]$summaryLines.Add("Generated order 1 width mm: $(Format-Mm $generatedOrder1WidthMm)")
    [void]$summaryLines.Add("Calculated order 2 width mm: $(Format-Mm $calculatedOrder2WidthMm)")
    [void]$summaryLines.Add("Generated order 2 width mm: $(Format-Mm $generatedOrder2WidthMm)")
    [void]$summaryLines.Add("Calculated total width mm: $(Format-Mm $calculatedTotalWidthMm)")
    [void]$summaryLines.Add("Generated total width mm: $(Format-Mm $generatedTotalWidthMm)")
    [void]$summaryLines.Add("Calculated left trim mm: $(Format-Mm $calculatedLeftTrimMm)")
    [void]$summaryLines.Add("Calculated boundaries mm: $(($calculatedBoundaries | ForEach-Object { Format-Mm $_ }) -join ', ')")
    [void]$summaryLines.Add("Calculated crease positions mm: $(($calculatedScorerPositions | ForEach-Object { Format-Mm $_ }) -join ', ')")
    [void]$summaryLines.Add("Generated knife count: $generatedNumberOfKnives")
    [void]$summaryLines.Add("Enabled knife count: $enabledKnifeCount")
    [void]$summaryLines.Add("Generated scorer count: $generatedNumberOfScorers")
    [void]$summaryLines.Add("Enabled scorer count: $enabledScorerCount")
    [void]$summaryLines.Add("Position tolerance mm: $(Format-Mm $PositionToleranceMm)")
    [void]$summaryLines.Add("Generation tolerance mm: $(Format-Mm $GenerationToleranceMm)")
    [void]$summaryLines.Add("Halt axis: $(Format-LogValue (Get-SampleValue $sample 'Control.HaltAxis' ''))")
    [void]$summaryLines.Add("Active sensors: $(if($activeSensors.Count -eq 0){'<none>'}else{$activeSensors -join ', '})")
    [void]$summaryLines.Add("Missing optional symbols: $(if($missingNames.Count -eq 0){'<none>'}else{$missingNames -join ', '})")
    [void]$summaryLines.Add("Global issues: $(if($globalIssues.Count -eq 0){'<none>'}else{$globalIssues -join ', '})")
    [void]$summaryLines.Add("Overall result: $overallResult")
    [void]$summaryLines.Add('')

    foreach ($row in $rows) {
        [void]$summaryLines.Add(
            ('{0}: Used={1} FromOrder={2} Generated={3} Offset={4} Expected={5} Commanded={6} Actual={7} Error={8} Result={9}' -f
                $row.Tool,
                $row.Used,
                $row.CalculatedFromOrderMm,
                $row.GeneratedPositionMm,
                $row.ConfiguredOffsetMm,
                $row.ExpectedAxisPositionMm,
                $row.CommandedPositionMm,
                $row.ActualPositionMm,
                $row.PositionErrorMm,
                $row.Result)
        )
    }

    [IO.File]::WriteAllLines($summaryPath, $summaryLines, (New-Object Text.UTF8Encoding($false)))

    #==============================================================
    # EN: Save a compact, operator-friendly order and tool report.
    # PT: Salva um relatório compacto do pedido e das ferramentas.
    #==============================================================
    $order1Composition = Format-OrderComposition $useOrder1 $order1Quantity $order1SheetType $order1Measures
    $order2Composition = Format-OrderComposition $useOrder2 $order2Quantity $order2SheetType $order2Measures
    $usedRows = @($rows | Where-Object { $_.Used })
    $failedRows = @($usedRows | Where-Object { $_.Result -ne 'OK' })
    $maximumPositionErrorMm = 0.0

    foreach ($row in $usedRows) {
        $rowErrorMm = [Convert]::ToDouble($row.AbsPositionErrorMm, $invariantCulture)
        $maximumPositionErrorMm = [Math]::Max($maximumPositionErrorMm, $rowErrorMm)
    }

    $compactLines = New-Object 'System.Collections.Generic.List[string]'
    [void]$compactLines.Add('SLITTER POSITION SUMMARY / RESUMO DE POSICIONAMENTO')
    [void]$compactLines.Add("Timestamp: $($timestamp.ToString('yyyy-MM-ddTHH:mm:ss.fffK'))")
    [void]$compactLines.Add("Order / Pedido: $(Format-LogValue (Get-SampleValue $sample 'Order.ProductionListNumber' ''))")
    [void]$compactLines.Add("Level 1 / Nivel 1: $order1Composition")
    [void]$compactLines.Add("Level 2 / Nivel 2: $order2Composition")
    [void]$compactLines.Add('')
    [void]$compactLines.Add("Level 1 width / Largura nivel 1: $(Format-CompactMm $calculatedOrder1WidthMm) mm")
    [void]$compactLines.Add("Level 2 width / Largura nivel 2: $(Format-CompactMm $calculatedOrder2WidthMm) mm")
    [void]$compactLines.Add("Produced width / Largura produzida: $(Format-CompactMm $calculatedTotalWidthMm) mm")
    [void]$compactLines.Add("Machine width / Largura da maquina: $(Format-CompactMm $machineWidthMm) mm")
    [void]$compactLines.Add("Left trim / Apara esquerda: $(Format-CompactMm $calculatedLeftTrimMm) mm")
    [void]$compactLines.Add("Right trim / Apara direita: $(Format-CompactMm ($machineWidthMm - $calculatedTotalWidthMm - $calculatedLeftTrimMm)) mm")
    [void]$compactLines.Add("Overall result / Resultado geral: $overallResult")
    [void]$compactLines.Add('')
    [void]$compactLines.Add(('TOOL'.PadRight(10) + 'USE'.PadRight(6) + 'CALCULATED'.PadLeft(12) + 'GENERATED'.PadLeft(12) + 'OFFSET'.PadLeft(10) + 'REFERENCE'.PadLeft(12) + 'ACTUAL'.PadLeft(12) + 'ERROR'.PadLeft(10) + '  STATUS'))
    [void]$compactLines.Add(('-' * 96))

    foreach ($row in $rows) {
        $useText = if ($row.Used) { 'YES' } else { 'NO' }
        $calculatedText = if ([string]::IsNullOrWhiteSpace($row.CalculatedFromOrderMm)) { '-' } else { $row.CalculatedFromOrderMm }
        [void]$compactLines.Add(
            $row.Tool.PadRight(10) +
            $useText.PadRight(6) +
            $calculatedText.PadLeft(12) +
            $row.GeneratedPositionMm.PadLeft(12) +
            $row.ConfiguredOffsetMm.PadLeft(10) +
            $row.ExpectedAxisPositionMm.PadLeft(12) +
            $row.ActualPositionMm.PadLeft(12) +
            $row.PositionErrorMm.PadLeft(10) +
            '  ' + $row.Result
        )
    }

    [void]$compactLines.Add('')
    [void]$compactLines.Add("Used knives / Facas utilizadas: $enabledKnifeCount/5")
    [void]$compactLines.Add("Used scorers / Vincos utilizados: $enabledScorerCount/8")
    [void]$compactLines.Add("Failed tools / Ferramentas com falha: $($failedRows.Count)")
    [void]$compactLines.Add("Maximum position error / Maior erro: $(Format-CompactMm $maximumPositionErrorMm) mm")
    [void]$compactLines.Add("Order generation / Geracao do pedido: $(if($globalIssues.Count -eq 0){'OK'}else{$globalIssues -join '|'})")

    [IO.File]::WriteAllLines($compactSummaryPath, $compactLines, (New-Object Text.UTF8Encoding($false)))

    Write-Host ''
    Write-Host 'Slitter tool position diagnostic / Diagnostico das posicoes do slitter'
    Write-Host ("Order / Pedido: {0}" -f (Get-SampleValue $sample 'Order.ProductionListNumber' ''))
    Write-Host ("Overall / Resultado: {0}" -f $overallResult)
    Write-Host ("Global issues / Falhas globais: {0}" -f $(if($globalIssues.Count -eq 0){'<none>'}else{$globalIssues -join ', '}))
    Write-Host ''

    $rows |
        Select-Object Tool, Used, CalculatedFromOrderMm, GeneratedPositionMm,
            ConfiguredOffsetMm, ExpectedAxisPositionMm, CommandedPositionMm,
            ActualPositionMm, PositionErrorMm, Result |
        Format-Table -AutoSize

    Write-Host ("CSV: {0}" -f $csvPath)
    Write-Host ("Summary / Resumo: {0}" -f $summaryPath)
    Write-Host ("Compact summary / Resumo compacto: {0}" -f $compactSummaryPath)
}
finally {
    if ($null -ne $loader) {
        $loader.Dispose()
    }

    if ($null -ne $client) {
        $client.Dispose()
    }
}
