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

### Escrita diagnostica controlada

O teste de escrita de `.currentOrder.fluteType` possui validacao e readback e
fica desabilitado por padrao (`Ads:EnableDiagnosticWrites = false`). Para um
teste supervisionado, habilite-o somente no processo atual:

```powershell
$env:Ads__EnableDiagnosticWrites = 'true'
dotnet run --project .\src\DryEnd.Web --urls http://localhost:5074
```

Depois envie `POST /api/diagnostics/current-order/flute-type` com o JSON
`{ "value": "TESTE-ADS" }`. Reiniciar normalmente o backend volta a bloquear
esse endpoint. Essa escrita nao usa a porta 10000 nem altera o estado do
runtime TwinCAT.

## Banco de dados

A integracao SQL deve receber a connection string por configuracao externa,
preferencialmente pela variavel de ambiente
`ConnectionStrings__DryEnd`. Credenciais da HMI WinForms nao devem ser copiadas
para o repositorio, documentacao ou logs.

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
