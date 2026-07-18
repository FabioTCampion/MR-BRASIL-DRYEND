# DryEnd.Next

Nova aplicacao web para substituir gradualmente a HMI WinForms localizada em
`PC/Hmi`.

## Estado atual

A primeira base executavel esta inicializada. Ela contem o monitor ADS,
API de diagnostico, atualizacao em tempo real por SignalR, painel React e testes
automatizados.

O diagnostico de pedidos le `currentOrder` e `nextOrder`, incluindo dados do
papel, medidas M1..M5, contadores, pilhas, larguras calculadas, ferramentas
habilitadas, referencias geradas e flags de fora de faixa.

A interface segue as funcoes da HMI WinForms e possui as telas Producao,
Pedidos, Historico, Graficos e Diagnostico. A aplicacao permite editar
explicitamente o pedido atual e sincroniza automaticamente o primeiro pedido
valido da fila SQL com `nextOrder`. A troca de pedido possui confirmacao do
operador e handshake controlado com o PLC.

Antes de implementar qualquer funcionalidade, leia:

1. `../../PROJECT_CONTEXT.md`
2. `../../CONVENTIONS.md`
3. `PROJECT_HANDOFF.md`

## Escopo

- As alteracoes devem permanecer dentro de `PC/`.
- `PLC/` e `HMI/` devem ser usados apenas como referencia e contrato.
- O WinForms atual deve permanecer intacto durante a migracao inicial.
- A leitura ADS de `currentOrder` e `nextOrder` ocorre a cada 500 ms.
- A edicao explicita de `currentOrder` e a sincronizacao validada de `nextOrder`
  possuem escrita ADS com readback.
- E proibido alterar o estado do runtime TwinCAT: nunca usar STOP, RUN, RESET,
  `WriteControl` ou a porta ADS de servico do sistema `10000`.

## Arquitetura acordada

- Frontend: React com TypeScript e Vite.
- Backend: ASP.NET Core em .NET 10 LTS.
- Comunicacao em tempo real: SignalR.
- Comunicacao industrial: pacote oficial `Beckhoff.TwinCAT.Ads`.
- Persistencia inicial: SQL Server atual com Dapper.
- Hospedagem inicial: backend local no PC atual, sem IIS.
- Implantacao: um unico servico Windows contendo backend e frontend compilado.
- Evolucao futura: publicacao compativel com Linux.

Consulte `PROJECT_HANDOFF.md` para as decisoes, riscos, plano e criterios de
aceitacao completos.

## Executar em desenvolvimento

Em dois terminais:

```powershell
dotnet run --project .\src\DryEnd.Web

Set-Location .\src\dryend-web-client
npm run dev
```

O backend tenta reconectar ao PLC automaticamente, sem alterar o estado do
runtime. A configuracao ADS fica em `src/DryEnd.Web/appsettings.json`.

Os simbolos globais deste PLC usam ponto inicial, por exemplo
`.currentOrder.tableID` e `.nextOrder.tableID`.

O sincronizador consulta a fila SQL a cada 1000 ms. Somente pedidos pendentes,
com sequencia positiva, campos ativos nao nulos e valores compativeis com os
tipos PLC podem ser enviados. O pedido atual nunca e selecionado e um
`nextOrder` ocupado por outro `tableID` nunca e sobrescrito automaticamente.
O `tableID` e gravado por ultimo e todo o pedido e confirmado por readback ADS.

A sincronizacao apenas prepara `nextOrder`; ela nao aciona
`changeOrderRequest`, nao confirma o handshake SQL e nao altera o estado do
runtime TwinCAT. Pode ser desabilitada por configuracao:

```json
"NextOrderSync": {
  "Enabled": false,
  "IntervalMilliseconds": 1000
}
```

O backend tambem registra `currentOrder.lineSpeed` em `MachineSpeedRecords`
sem depender do navegador. A velocidade e limitada a `0..300 m/min`, e os
horarios sao alinhados em intervalos estaveis de 30 segundos. A gravacao ocorre
somente com snapshot PLC online e valido. A deduplicacao consulta o proprio
banco em uma transacao serializavel, portanto continua protegida depois de uma
reinicializacao do servico. A configuracao pode ser alterada externamente:

```json
"MachineSpeedLogging": {
  "Enabled": true,
  "CheckIntervalMilliseconds": 1000,
  "SlotIntervalSeconds": 30,
  "MaximumSnapshotAgeSeconds": 10
}
```

Snapshots com mais de 10 segundos sao ignorados, mesmo se o ultimo estado
publicado ainda estiver `Online`. Isso evita registrar uma velocidade obsoleta
durante uma operacao ADS longa ou uma interrupcao entre leituras.

Esse servico somente le o snapshot ja publicado pelo monitor ADS e grava no
banco. Ele nao escreve no PLC e nao interfere na sincronizacao de `nextOrder`.

## Banco de dados

A integracao SQL deve receber a connection string por configuracao externa,
preferencialmente pela variavel de ambiente
`ConnectionStrings__DryEnd`. Credenciais da HMI WinForms nao devem ser copiadas
para o repositorio, documentacao ou logs.

No computador de desenvolvimento, a opcao recomendada e o Secret Manager do
.NET. O projeto `DryEnd.Web` possui um `UserSecretsId`, portanto a conexao pode
ser registrada fora do repositorio:

```powershell
dotnet user-secrets set "ConnectionStrings:DryEnd" `
    "<connection string externa>" `
    --project .\src\DryEnd.Web
```

A camada de dados usa Dapper sobre interfaces genericas de conexao e dialeto.
As telas e APIs nao dependem do provedor selecionado. Exemplo para SQL Server:

```powershell
$env:Database__Provider = 'SqlServer'
$env:ConnectionStrings__DryEnd = '<connection string externa>'
```

Para PostgreSQL, altere o provedor e os nomes das tabelas conforme o schema:

```powershell
$env:Database__Provider = 'PostgreSql'
$env:Database__OrdersTable = 'public.production_list_plc'
$env:Database__MachineSpeedTable = 'public.machine_speed_records'
$env:ConnectionStrings__DryEnd = '<connection string externa>'
```

O dialeto SQLite tambem esta implementado. O driver permanece como plugin
opcional porque a versao nativa disponivel durante esta implementacao possuia
um alerta de vulnerabilidade alto. Quando houver um pacote corrigido, basta
inclui-lo na implantacao e usar:

```powershell
$env:Database__Provider = 'Sqlite'
$env:Database__OrdersTable = 'ProductionList_Plc'
$env:Database__MachineSpeedTable = 'MachineSpeedRecords'
$env:ConnectionStrings__DryEnd = 'Data Source=C:\DryEnd\dryend.db'
```

Cada banco precisa possuir um schema compativel com as tabelas produtivas. As
diferencas de `TOP/LIMIT`, conversao de texto e retorno de identidade ficam
isoladas em `ProviderProductionQueries`.

Em 2026-07-17, a conexao com a instancia produtiva foi validada por
`192.168.30.10,1433`. Como o nome `FACAO-CCO` nao e resolvido por DNS neste PC,
o ambiente local usa o endereco IP. O SQL Server possui certificado interno;
o segredo local mantem criptografia habilitada e usa
`TrustServerCertificate=True`. Nenhuma credencial foi adicionada ao Git.

## Ferramentas e scripts

Os scripts de preparacao ficam em `scripts/`:

```powershell
# Inventario sem alteracoes no computador
.\scripts\Get-EnvironmentStatus.ps1

# Ambiente de desenvolvimento
# Executar em uma janela PowerShell aberta como Administrador
.\scripts\Install-DevelopmentEnvironment.ps1

# Ambiente de desenvolvimento com PowerShell 7 e extensao SQL
.\scripts\Install-DevelopmentEnvironment.ps1 `
    -IncludePowerShell7 `
    -IncludeSqlTools

# Gerar o pacote completo de producao
.\scripts\Publish-DryEnd.ps1 -Version 0.1.0
```

O perfil de desenvolvimento instala VS Code, .NET 10 SDK, Node.js 24 LTS e
extensoes do editor. Git e preservado quando ja estiver instalado.

O pacote padrao e autocontido e, portanto, nao exige a instalacao do runtime
.NET no servidor. Node.js, Visual Studio, Git e IIS tambem nao sao necessarios.
Consulte `DEPLOYMENT.md` para a primeira instalacao, atualizacao remota,
rollback e diagnostico. Nenhum script altera a rota ADS ou o estado do runtime
TwinCAT.

## Colocar o servidor local online

Este procedimento serve para teste conectado no PC da maquina. Ele nao substitui
a instalacao definitiva como Windows Service descrita em `DEPLOYMENT.md`.

### Verificacoes obrigatorias

1. Confirme que a HMI WinForms legada esta fechada. O backend novo e o WinForms
   nao podem atuar simultaneamente como escritores ADS.
2. Confirme que a maquina esta parada e liberada para teste quando os workers de
   escrita forem habilitados.
3. Nao altere a rota ADS, o AMS Net ID, a porta `851` ou o estado do runtime.
4. Confirme que a connection string esta no Secret Manager, nunca no repositorio:

```powershell
dotnet user-secrets list --project .\src\DryEnd.Web
```

Nao copie o valor mostrado por esse comando para logs, documentos ou mensagens.

### Modo conectado somente leitura

Use este modo para validar PLC, DUTs e interface sem escrever no PLC ou SQL:

```powershell
$env:NextOrderSync__Enabled = 'false'
$env:ChangeOrderHandshake__Enabled = 'false'
$env:MachineSpeedLogging__Enabled = 'false'
$env:Logging__EventLog__LogLevel__Default = 'None'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5099'

dotnet run --project .\src\DryEnd.Web --no-build --no-launch-profile
```

### Gerar e executar uma versao Release local

Gere primeiro o pacote completo, que inclui o frontend em `wwwroot`:

```powershell
.\scripts\Publish-DryEnd.ps1 -Version 0.1.1
```

Em uma copia sem metadados `.git`, o script pode terminar com erro durante a
criacao do manifesto, depois de o `dotnet publish` e o build React terem sido
concluidos. Nesse caso, confirme que estes arquivos existem antes de executar:

```powershell
Test-Path .\artifacts\CPNTeck-DryEnd-0.1.1-win-x64\app\DryEnd.Web.exe
Test-Path .\artifacts\CPNTeck-DryEnd-0.1.1-win-x64\app\wwwroot\index.html
```

Para um teste produtivo local usando o segredo SQL do projeto, execute a partir
do diretorio publicado:

```powershell
Set-Location .\artifacts\CPNTeck-DryEnd-0.1.1-win-x64\app
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5099'
$env:Logging__EventLog__LogLevel__Default = 'None'
.\DryEnd.Web.exe
```

O build continua sendo Release. O ambiente `Development` e usado nesse teste
local somente para carregar o Secret Manager. Os valores padrao deixam
`NextOrderSync`, `ChangeOrderHandshake` e `MachineSpeedLogging` habilitados.
Portanto, este comando autoriza os fluxos ADS/SQL ja implementados.

O `EventLog` e silenciado apenas no processo porque uma execucao sem elevacao
pode nao ter permissao para gravar no Windows Event Log. A instalacao real como
servico deve manter a configuracao produtiva externa em `ProgramData`.

### Validar depois da inicializacao

```powershell
Invoke-RestMethod http://127.0.0.1:5099/api/version
Invoke-RestMethod http://127.0.0.1:5099/api/diagnostics
Invoke-RestMethod http://127.0.0.1:5099/api/production-data/status
```

Os criterios minimos sao:

- `/` responde HTTP 200 e carrega um bundle em `/assets/`;
- diagnostico com estado `Online`, snapshot e `lastError` vazio;
- banco com `configured: true` e `available: true`;
- IDs de `currentOrder` e `nextOrder` coerentes com o PLC.

Abra a interface em [http://127.0.0.1:5099](http://127.0.0.1:5099). Se uma aba
antiga continuar mostrando `Offline`, use `Ctrl+F5` para descartar o bundle em
cache.

Para encerrar uma execucao interativa, pressione `Ctrl+C` no terminal que esta
executando o backend. Um processo iniciado e supervisionado por um agente nao e
uma instalacao persistente: ele pode terminar junto com a tarefa. Para operacao
continua, instale o Windows Service conforme `DEPLOYMENT.md`.
