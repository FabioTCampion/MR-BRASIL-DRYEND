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
valido da fila SQL com `nextOrder`. Os comandos que executam a troca de pedido
permanecem desabilitados nesta fase.

Antes de implementar qualquer funcionalidade, leia:

1. `../../PROJECT_CONTEXT.md`
2. `../../CONVENTIONS.md`
3. `PROJECT_HANDOFF.md`

## Escopo

- As alteracoes devem permanecer dentro de `PC/`.
- `PLC/` e `HMI/` devem ser usados apenas como referencia e contrato.
- O WinForms atual deve permanecer intacto durante a migracao inicial.
- A leitura ADS de `currentOrder` e `nextOrder` ocorre a cada 2000 ms.
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

O sincronizador consulta a fila SQL a cada 5000 ms. Somente pedidos pendentes,
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
  "IntervalMilliseconds": 5000
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

# Pre-requisitos minimos de um servidor framework-dependent
.\scripts\Install-ServerPrerequisites.ps1
```

O perfil de desenvolvimento instala VS Code, .NET 10 SDK, Node.js 24 LTS e
extensoes do editor. Git e preservado quando ja estiver instalado.

O perfil de servidor instala somente o ASP.NET Core Runtime 10 e valida TwinCAT
ADS. Node.js, VS Code, Git e IIS nao sao necessarios no servidor. Nenhum script
altera a rota ADS ou grava credenciais SQL.
