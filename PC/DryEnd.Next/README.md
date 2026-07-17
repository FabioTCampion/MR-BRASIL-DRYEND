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
Pedidos, Historico, Graficos e Diagnostico. Comandos de troca de pedido no PLC
permanecem visiveis, mas desabilitados nesta fase.

Antes de implementar qualquer funcionalidade, leia:

1. `../../PROJECT_CONTEXT.md`
2. `../../CONVENTIONS.md`
3. `PROJECT_HANDOFF.md`

## Escopo

- As alteracoes devem permanecer dentro de `PC/`.
- `PLC/` e `HMI/` devem ser usados apenas como referencia e contrato.
- O WinForms atual deve permanecer intacto durante a migracao inicial.
- A primeira prova de conceito realiza somente leitura no ADS.
- Futuras escritas de simbolos PLC sao permitidas com validacao e readback.
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

Nesta fase, a camada ADS nao possui operacoes de escrita. Os comandos legados
permanecem desabilitados na interface ate a etapa de validacao operacional.

## Banco de dados

A integracao SQL deve receber a connection string por configuracao externa,
preferencialmente pela variavel de ambiente
`ConnectionStrings__DryEnd`. Credenciais da HMI WinForms nao devem ser copiadas
para o repositorio, documentacao ou logs.

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

Em 2026-07-17, a instancia configurada na HMI antiga nao estava acessivel deste
PC (SQL Server error 26). A integracao permanece pendente ate que nome da
instancia, rede e servico SQL sejam confirmados.

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
