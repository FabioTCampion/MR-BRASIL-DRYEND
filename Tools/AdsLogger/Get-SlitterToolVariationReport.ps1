[CmdletBinding()]
param(
    [string]$LogDirectory = '',
    [ValidateRange(0.1, 1000.0)] [double]$MaximumAcceptedErrorMm = 10.0,
    [ValidateRange(1, 1000)] [int]$MinimumSamples = 3,
    [string]$OutputDirectory = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$culture = [Globalization.CultureInfo]::InvariantCulture

if ([string]::IsNullOrWhiteSpace($LogDirectory)) {
    $LogDirectory = Join-Path $PSScriptRoot 'Logs'
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = $LogDirectory
}

function Format-Number {
    param([double]$Value)
    return $Value.ToString('0.000', $culture)
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $LogDirectory -Filter 'slitter-tool-position-*.csv' -File |
        Sort-Object LastWriteTime
)

if ($sourceFiles.Count -eq 0) {
    throw "No slitter diagnostic CSV files were found / Nenhum CSV encontrado: $LogDirectory"
}

$samples = New-Object 'System.Collections.Generic.List[object]'

foreach ($file in $sourceFiles) {
    foreach ($row in (Import-Csv -LiteralPath $file.FullName -Delimiter ';')) {
        if (($row.Used -ne 'True') -or [string]::IsNullOrWhiteSpace($row.PositionErrorMm)) {
            continue
        }

        $errorMm = [Convert]::ToDouble($row.PositionErrorMm, $culture)
        $absoluteErrorMm = [Math]::Abs($errorMm)

        [void]$samples.Add([pscustomobject][ordered]@{
            Timestamp = $row.Timestamp
            Tool = $row.Tool
            ErrorMm = $errorMm
            AbsoluteErrorMm = $absoluteErrorMm
            Included = $absoluteErrorMm -le $MaximumAcceptedErrorMm
            SourceFile = $file.Name
        })
    }
}

$acceptedSamples = @($samples | Where-Object Included)
$discardedSamples = @($samples | Where-Object { -not $_.Included })
$summary = New-Object 'System.Collections.Generic.List[object]'

foreach ($group in ($acceptedSamples | Group-Object Tool)) {
    $errors = @($group.Group | ForEach-Object { [double]$_.ErrorMm })
    $absoluteErrors = @($group.Group | ForEach-Object { [double]$_.AbsoluteErrorMm })
    $meanErrorMm = ($errors | Measure-Object -Average).Average
    $meanAbsoluteErrorMm = ($absoluteErrors | Measure-Object -Average).Average
    $minimumErrorMm = ($errors | Measure-Object -Minimum).Minimum
    $maximumErrorMm = ($errors | Measure-Object -Maximum).Maximum
    $maximumAbsoluteErrorMm = ($absoluteErrors | Measure-Object -Maximum).Maximum
    $sumSquaredDifference = 0.0

    foreach ($errorMm in $errors) {
        $sumSquaredDifference += [Math]::Pow($errorMm - $meanErrorMm, 2)
    }

    $standardDeviationMm = if ($errors.Count -gt 1) {
        [Math]::Sqrt($sumSquaredDifference / ($errors.Count - 1))
    }
    else { 0.0 }

    [void]$summary.Add([pscustomobject][ordered]@{
        Tool = $group.Name
        Samples = $errors.Count
        SufficientSamples = $errors.Count -ge $MinimumSamples
        MeanErrorMm = [Math]::Round($meanErrorMm, 3)
        MeanAbsoluteErrorMm = [Math]::Round($meanAbsoluteErrorMm, 3)
        StandardDeviationMm = [Math]::Round($standardDeviationMm, 3)
        MinimumErrorMm = [Math]::Round($minimumErrorMm, 3)
        MaximumErrorMm = [Math]::Round($maximumErrorMm, 3)
        RangeMm = [Math]::Round($maximumErrorMm - $minimumErrorMm, 3)
        MaximumAbsoluteErrorMm = [Math]::Round($maximumAbsoluteErrorMm, 3)
    })
}

$rankedSummary = @($summary | Sort-Object @{Expression='SufficientSamples';Descending=$true}, @{Expression='StandardDeviationMm';Descending=$true})
[void](New-Item -ItemType Directory -Path $OutputDirectory -Force)
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$summaryCsvPath = Join-Path $OutputDirectory "slitter-tool-variation-$timestamp.csv"
$discardedCsvPath = Join-Path $OutputDirectory "slitter-tool-variation-$timestamp.discarded.csv"
$reportPath = Join-Path $OutputDirectory "slitter-tool-variation-$timestamp.txt"
$latestReportPath = Join-Path $OutputDirectory 'slitter-tool-variation-latest.txt'

$rankedSummary | Export-Csv -LiteralPath $summaryCsvPath -Delimiter ';' -NoTypeInformation -Encoding UTF8
$discardedSamples | Export-Csv -LiteralPath $discardedCsvPath -Delimiter ';' -NoTypeInformation -Encoding UTF8

$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('SLITTER TOOL VARIATION / VARIACAO DAS FERRAMENTAS')
[void]$lines.Add("Generated / Gerado: $((Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK'))")
[void]$lines.Add("Source files / Arquivos: $($sourceFiles.Count)")
[void]$lines.Add("Accepted samples / Amostras aceitas: $($acceptedSamples.Count)")
[void]$lines.Add("Discarded samples / Amostras descartadas: $($discardedSamples.Count)")
[void]$lines.Add("Maximum accepted error / Erro maximo aceito: $(Format-Number $MaximumAcceptedErrorMm) mm")
[void]$lines.Add("Minimum samples / Minimo de amostras: $MinimumSamples")
[void]$lines.Add('')
[void]$lines.Add('RANK TOOL       SAMPLES  MEAN ABS   STD DEV       MIN       MAX     RANGE   MAX ABS  STATUS')
[void]$lines.Add('-' * 94)

$rank = 0
foreach ($item in $rankedSummary) {
    $rank++
    $status = if ($item.SufficientSamples) { 'EVALUATED' } else { 'INSUFFICIENT_DATA' }
    [void]$lines.Add(('{0,4} {1,-10} {2,7} {3,9} {4,9} {5,9} {6,9} {7,9} {8,9}  {9}' -f
        $rank, $item.Tool, $item.Samples,
        (Format-Number $item.MeanAbsoluteErrorMm),
        (Format-Number $item.StandardDeviationMm),
        (Format-Number $item.MinimumErrorMm),
        (Format-Number $item.MaximumErrorMm),
        (Format-Number $item.RangeMm),
        (Format-Number $item.MaximumAbsoluteErrorMm),
        $status))
}

[void]$lines.Add('')
[void]$lines.Add('DISCARDED OUTLIERS / OUTLIERS DESCARTADOS')

if ($discardedSamples.Count -eq 0) {
    [void]$lines.Add('<none / nenhum>')
}
else {
    foreach ($sample in $discardedSamples) {
        [void]$lines.Add("$($sample.Timestamp) $($sample.Tool) Error=$(Format-Number $sample.ErrorMm) mm File=$($sample.SourceFile)")
    }
}

$encoding = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllLines($reportPath, $lines, $encoding)
[IO.File]::WriteAllLines($latestReportPath, $lines, $encoding)

Write-Host "Variation report / Relatorio: $reportPath"
Write-Host "Latest report / Ultimo relatorio: $latestReportPath"
Write-Host "Summary CSV / CSV resumo: $summaryCsvPath"
Write-Host "Discarded CSV / CSV descartados: $discardedCsvPath"

$rankedSummary | Format-Table Tool, Samples, MeanAbsoluteErrorMm, StandardDeviationMm, MinimumErrorMm, MaximumErrorMm, RangeMm, SufficientSamples -AutoSize
