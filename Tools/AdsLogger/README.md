# Complex Stacker ADS Logger

Logger ADS somente leitura para diagnosticar a sequência de descarte do stacker superior e o controle do elevador.

Read-only ADS logger for diagnosing the upper stacker discharge sequence and elevator control.

## Execução contínua

Abra um PowerShell na pasta principal do projeto e execute:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\AdsLogger\Start-ComplexStackerAdsLog.ps1
```

O logger continua executando até pressionar `Ctrl+C`.

Também é possível executar `Start-ComplexStackerAdsLog.cmd` com duplo clique.

## Captura com tempo definido

Exemplo para registrar cinco minutos com uma amostra a cada 50 ms:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\AdsLogger\Start-ComplexStackerAdsLog.ps1 -DurationSeconds 300 -SampleIntervalMs 50
```

## Comunicação padrão

```text
AMS Net ID : 192.168.30.79.1.1
ADS Port   : 851
ADS DLL    : C:\TwinCAT\AdsApi\.NET\v4.0.30319\TwinCAT.Ads.dll
PLC symbol : MAIN.instFBUpperStackerControl
```

Esses valores podem ser substituídos por parâmetros:

```powershell
.\Tools\AdsLogger\Start-ComplexStackerAdsLog.ps1 `
    -AmsNetId '192.168.30.79.1.1' `
    -AdsPort 851 `
    -SampleIntervalMs 50
```

## Arquivos gerados

Os arquivos são criados em `Tools\AdsLogger\Logs`:

```text
complex-stacker-YYYYMMDD-HHMMSS.csv
complex-stacker-YYYYMMDD-HHMMSS.events.txt
```

- O CSV contém um snapshot sincronizado de 137 símbolos por amostra.
- O arquivo `events.txt` registra apenas conexão, erros e mudanças importantes.
- Os arquivos de log são ignorados pelo Git para evitar commits muito grandes.

## Diagnósticos calculados

O CSV adiciona cinco colunas derivadas:

- `Derived.PlatformClear`: reproduz a condição de plataforma livre.
- `Derived.ElevatorState30Reason`: explica por que o elevador permanece no estado 30.
- `Derived.ElevatorHomeWaitReason`: explica cada espera do homing entre o limite inferior e a subida final.
- `Derived.DischargeWaitReason`: informa o que cada passo do descarte está aguardando.
- `Derived.Anomaly`: destaca combinações incompatíveis, como override ativo com elevador fora do estado 40.

## Segurança

O programa utiliza somente `ReadState` e `SumSymbolRead`. Ele não possui chamadas de escrita ADS e não altera variáveis do PLC.

The program only uses `ReadState` and `SumSymbolRead`. It contains no ADS write calls and does not modify PLC variables.

## Diagnóstico one-shot das posições do slitter

Para validar uma vez as posições das facas e dos vincos do pedido atual, execute:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\AdsLogger\Get-SlitterToolPositionDiagnostic.ps1
```

Ou execute `Get-SlitterToolPositionDiagnostic.cmd` com duplo clique.

O diagnóstico recalcula os limites e vincos das chapas a partir das medidas do
`currentOrder`, identifica as ferramentas utilizadas por `knifeEnabledArr` e
`scorerEnabledArr`, compara posições geradas, offsets, alvos enviados, posições
atuais e sensores, grava um CSV e um resumo TXT em `Tools\AdsLogger\Logs`, e
também gera um `.summary.txt` com a composição dos níveis, larguras, aparas e
uma tabela compacta das ferramentas. Depois encerra automaticamente.

### Monitor automático por mudança de pedido

Para manter o monitor em execução e gerar os três relatórios somente quando as
medidas, quantidades, tipos ou níveis do pedido mudarem, execute:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\AdsLogger\Start-SlitterOrderDiagnosticMonitor.ps1
```

Ou abra `Start-SlitterOrderDiagnosticMonitor.cmd`. Na inicialização, o pedido
atual é registrado como referência sem gerar um log. Depois de uma mudança, o
monitor aguarda o pedido ficar estável e o posicionamento terminar. Para também
capturar o pedido presente na inicialização, use `-CaptureInitialOrder`.

### Análise histórica de variação

Para consolidar todos os diagnósticos e ranquear as ferramentas pela variação:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\AdsLogger\Get-SlitterToolVariationReport.ps1
```

Por padrão, erros absolutos acima de `10 mm` são descartados e registrados em
um CSV separado. O limite pode ser alterado com `-MaximumAcceptedErrorMm`. O
monitor automático atualiza esse relatório após cada novo pedido diagnosticado.
