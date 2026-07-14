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

Exemplo para registrar cinco minutos com uma amostra a cada 100 ms:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\AdsLogger\Start-ComplexStackerAdsLog.ps1 -DurationSeconds 300 -SampleIntervalMs 100
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
    -SampleIntervalMs 100
```

## Arquivos gerados

Os arquivos são criados em `Tools\AdsLogger\Logs`:

```text
complex-stacker-YYYYMMDD-HHMMSS.csv
complex-stacker-YYYYMMDD-HHMMSS.events.txt
```

- O CSV contém um snapshot sincronizado de 105 símbolos por amostra.
- O arquivo `events.txt` registra apenas conexão, erros e mudanças importantes.
- Os arquivos de log são ignorados pelo Git para evitar commits muito grandes.

## Diagnósticos calculados

O CSV adiciona quatro colunas derivadas:

- `Derived.PlatformClear`: reproduz a condição de plataforma livre.
- `Derived.ElevatorState30Reason`: explica por que o elevador permanece no estado 30.
- `Derived.DischargeWaitReason`: informa o que cada passo do descarte está aguardando.
- `Derived.Anomaly`: destaca combinações incompatíveis, como override ativo com elevador fora do estado 40.

## Segurança

O programa utiliza somente `ReadState` e `SumSymbolRead`. Ele não possui chamadas de escrita ADS e não altera variáveis do PLC.

The program only uses `ReadState` and `SumSymbolRead`. It contains no ADS write calls and does not modify PLC variables.
