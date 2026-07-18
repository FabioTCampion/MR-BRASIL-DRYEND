# Transferencia de contexto - DryEnd.Next

## Recuperacao de historico e divergencia do nextOrder (2026-07-18)

- Supervisor e Administrador podem recuperar um pedido finalizado pelo botao do
  historico. A operacao altera somente `ProductionState` para `0`: preserva o
  mesmo registro, ID, sequencia, horarios, quantidades e contadores.
- Um pedido recuperado permanece com `HistorySavedAt` preenchido e recebe a
  marca visual `Recuperado do historico` na fila.
- O card do proximo pedido consulta a comparacao SQL/PLC a cada cinco segundos.
  A comparacao inclui somente campos enviados por `NextOrderUpdate` e ignora
  metricas, flags, contadores produzidos e `GeneratedOrder`.
- Divergencias mostram os valores programado e atual. Somente Supervisor e
  Administrador podem confirmar `Reescrever no CLP`; a operacao e bloqueada
  durante troca/AOC, possui readback e registra usuario e campos no log.

## Recuperacao do banco durante o boot (2026-07-18)

- A migracao/verificacao do schema nao bloqueia mais a inicializacao do servidor.
- `DatabaseInitializationWorker` repete a operacao a cada cinco segundos enquanto
  o SQL estiver indisponivel e encerra depois da primeira inicializacao concluida.
- O instalador habilita `failureflag` no Windows Service, aplicando as acoes de
  recuperacao tambem quando o processo termina com erro.
- O objetivo e permitir que o PC de producao reinicie antes do SQL remoto sem
  exigir um `Restart-Service` manual para restaurar o login.

## Registro de paradas e justificativas (2026-07-17)

- `ProductionStopWorker` observa a velocidade do pedido atual a cada segundo e registra somente transicoes: abaixo de `10 m/min` abre uma parada; ao retornar a `10 m/min` ou mais, encerra a parada aberta.
- Ao iniciar, o worker tambem percorre todo o historico de `MachineSpeedRecords`, reconstrói as sequencias abaixo de `10 m/min` e substitui apenas paradas ainda nao justificadas. Paradas justificadas sao preservadas e lacunas de amostragem acima de cinco minutos separam os eventos.
- O SQL possui duas novas tabelas, criadas automaticamente pelo migrador: `ProductionStopReasons` (catalogo) e `ProductionStops` (eventos e justificativas).
- O catalogo oficial esta incorporado em `src/DryEnd.Infrastructure.Database/StopReasons.txt` e e semeado de forma idempotente. Atualmente contem 119 codigos em 7 categorias.
- Endpoints em `/api/production-data`: `stop-reasons`, `stops?date=AAAA-MM-DD`, `stops/pending-count` e `PUT stops/{id}/justification`.
- A tela Metricas inclui `Tabela de paradas`, selecao de motivo por categoria, observacao e usuario digitado manualmente. A autenticacao do usuario sera integrada posteriormente.
- O badge vermelho fica no botao `Tabela de paradas`, dentro da tela Metricas, e mostra somente as paradas encerradas ainda sem justificativa na data selecionada. A parada aberta nao entra no contador.
- A duracao individual de uma parada nao tem limite de 60 minutos. O limite de 60 minutos permanece apenas no calculo agregado de tempo parado dentro de cada hora.

## Graficos interativos de metricas (2026-07-17)

- Os graficos manuais em SVG/CSS foram substituidos por Apache ECharts 6.1, usando importacoes modulares e renderizador Canvas.
- Velocidade, medias horarias, parada por hora e duracao das paradas possuem tooltip, zoom pelo mouse/toque, barra inferior de navegacao e redimensionamento automatico.
- O grafico de velocidade destaca abaixo de `10 m/min`, media, maxima e regioes de parada.
- `MetricsScreen` e carregada sob demanda por `React.lazy`, mantendo o bundle inicial da HMI sem o peso do motor de graficos.

## Login e controle de usuarios (2026-07-17)

- Autenticacao simples por cookie `HttpOnly`, sessao de 12 horas e senhas processadas com `PasswordHasher` do ASP.NET Core.
- A tabela multiplataforma `ApplicationUsers` e criada automaticamente para SQL Server, PostgreSQL ou SQLite.
- Perfis: `Observer`, `Operator`, `Supervisor` e `Administrator`. O observador possui somente leitura; comandos PLC exigem operador; alteracao de pedidos exige supervisor; usuarios exigem administrador.
- Quando nao ha usuarios, `/api/auth/status` informa `requiresSetup` e a interface apresenta a criacao do primeiro administrador. Nao existe senha padrao no codigo.
- A tela `Usuarios` permite criar, editar, desativar e redefinir senha. Usuarios sao desativados, nao apagados fisicamente, para preservar historico.
- Justificativas de parada usam automaticamente o usuario autenticado; o campo manual foi removido.
- Chaves dos cookies ficam em `Authentication:KeyPath` ou, por padrao, em `data/keys` ao lado da aplicacao. Em Docker/Linux esse caminho deve ser um volume/diretorio persistente e gravavel.

## Objetivo deste documento

Este documento registra as decisoes tomadas durante o planejamento da nova
aplicacao do PC e permite continuar o trabalho em outro computador ou com outro
agente sem depender do historico da conversa.

O codigo existente continua sendo a referencia definitiva. Antes de modificar
arquivos, leia tambem `../../PROJECT_CONTEXT.md` e `../../CONVENTIONS.md`.

## Atualizacao de implementacao em 2026-07-17

- A leitura de `currentOrder` e `nextOrder` esta configurada para 2000 ms.
- A edicao explicita de `currentOrder` foi autorizada e implementada com
  readback ADS.
- A fila SQL e consultada a cada 5000 ms e o primeiro pedido valido e escrito
  automaticamente em `nextOrder`.
- O pedido atual e excluido da selecao, valores nulos em campos ativos bloqueiam
  a transferencia e um `nextOrder` ja ocupado nao e sobrescrito.
- `tableID` e gravado por ultimo e a configuracao completa e confirmada por
  readback.
- A troca de pedido e o handshake SQL ainda nao sao executados pela nova
  aplicacao.
- Nenhuma rotina usa `WriteControl` ou a porta ADS 10000.

## Limite do trabalho

O escopo de alteracao foi limitado a `PC/`.

- `PC/Hmi` contem o WinForms atual e pode ser consultado durante a migracao.
- `PLC/` deve ser usado somente para conferir DUTs, simbolos e o comportamento
  do handshake.
- `HMI/` deve ser usado somente como referencia da interface dedicada.
- Nenhuma mudanca no PLC ou na HMI dedicada foi autorizada nesta etapa.
- O WinForms existente deve continuar disponivel durante a migracao.

## Situacao atual

A aplicacao existente e C# WinForms em .NET Framework 4.7.2 e concentra grande
parte da integracao em `PC/Hmi/MainForm/frmMain.cs`.

Ela possui:

- comunicacao TwinCAT ADS;
- gerenciamento da fila de pedidos;
- historico de producao;
- sincronizacao com SQL Server;
- graficos e metricas;
- comandos de troca de pedido e AOC.

Os principais problemas relatados sao perda de conexao e falhas de sincronismo
entre PC e PLC. Foi definida a seguinte regra principal:

> O PLC sempre tem prioridade. O pedido atual e qualquer pedido ja aceito pelo
> PLC nao podem ser sobrescritos automaticamente pelo PC.

Tambem foram solicitadas estas funcionalidades:

1. recuperar um pedido do historico e coloca-lo novamente na fila;
2. inverter o pedido superior com o pedido inferior;
3. organizar os estados dos pedidos;
4. tornar a comunicacao e o handshake resistentes a reconexao.

## Problemas tecnicos observados no WinForms

- O proximo pedido e escrito no PLC campo a campo.
- Uma queda durante a escrita pode deixar uma estrutura parcial.
- `_lastNextOrderSignature` considera o SQL, mas nao confirma o conteudo real do
  PLC.
- A assinatura pode permanecer valida apos reconexao, mesmo com dados diferentes
  no PLC.
- O envio do proximo pedido e disparado periodicamente por timer.
- O handshake de troca depende da borda de subida de `changeOrderRequest`.
- Uma borda pode ser perdida durante uma falha de comunicacao.
- O acesso SQL do handshake ocorre de forma sincrona no fluxo da interface.
- A reconexao atual usa janelas modais e depende de interacao do operador.
- `EnsurePlcIsRunning()` pode tentar colocar o runtime em RUN.
- Os significados de `ProductionState` divergem entre classes e consultas.
- O tratamento de `LevelSelector = 2` nao e consistente entre tela, modelo e
  escrita ADS.

## Decisao de plataforma

Foi aprovada a criacao de uma nova aplicacao web, em paralelo ao WinForms.

### Frontend

- React;
- TypeScript;
- Vite;
- SignalR Client;
- biblioteca visual a selecionar durante a implementacao;
- Vitest e Playwright para testes.

### Backend

- ASP.NET Core;
- .NET 10 LTS;
- Web API;
- `BackgroundService` para conexao e sincronizacao ADS;
- SignalR para atualizacoes em tempo real;
- pacote oficial `Beckhoff.TwinCAT.Ads`;
- Dapper e SQL Server na primeira versao;
- xUnit para testes.

### Hospedagem

O backend rodara inicialmente no mesmo PC de operacao/desenvolvimento.

- A rota ADS para o PLC ja existe nesse PC.
- O ASP.NET Core deve executar sem IIS.
- O backend e o frontend compilado devem formar um unico pacote.
- Inicialmente a interface deve escutar apenas em `localhost`.
- Posteriormente a aplicacao podera ser instalada como Windows Service.
- A arquitetura deve permanecer compativel com uma futura publicacao Linux.
- Fechar o navegador nao pode interromper sincronizacao, historico ou logs.

React nao acessara ADS diretamente. Todo acesso ao PLC passara pelo backend.
Somente uma instancia do backend podera atuar como escritor ADS.

## Arquitetura logica

```text
Navegador React
    |
    | HTTPS / REST / SignalR
    v
DryEnd Edge - ASP.NET Core
    |-- servico de conexao ADS
    |-- maquina de estados de sincronizacao
    |-- regras de pedidos
    |-- repositorio SQL
    |-- logs e auditoria
    |
    +-- ADS --> PLC TwinCAT
    +-- SQL --> banco atual
```

Nao devem ser introduzidos inicialmente:

- microservicos;
- broker de mensagens;
- Kubernetes;
- migracao simultanea do banco;
- acesso ADS pelo navegador;
- dependencias globais de React ou TypeScript.

## Regra de autoridade e sincronizacao

### Pedido atual

- `currentOrder` do PLC e a fonte de verdade.
- O SQL espelha o pedido e seus contadores.
- Divergencias devem ser registradas, nao corrigidas sobrescrevendo o PLC.
- Um `tableID` inexistente no SQL deve gerar diagnostico e bloquear a operacao
  correspondente, sem modificar o PLC.

### Proximo pedido

- O SQL organiza a fila.
- O PC so pode oferecer um pedido quando o PLC estiver em condicao segura para
  recebe-lo.
- Apos a escrita, o backend deve ler novamente e confirmar o conteudo.
- Depois de aceito, o pedido presente no PLC tem prioridade.
- Apos reconexao, primeiro ler, estabilizar e reconciliar; somente depois
  considerar qualquer escrita.

### Reconexao

Maquina de estados proposta:

```text
Offline
  -> Connecting
  -> WaitingForStableData
  -> Reconciling
  -> Online
  -> Degraded
  -> Reconnecting
```

- Nao usar MessageBox ou qualquer interacao modal.
- Aplicar tentativas automaticas com intervalo progressivo.
- Invalidar snapshots e confirmacoes antigas ao desconectar.
- Aguardar watchdog e snapshots validos antes de habilitar comandos.
- Nao colocar automaticamente o PLC em RUN.

### Handshake da troca

O processamento deve ser idempotente, identificado pelos IDs do pedido atual e
do proximo pedido, e nao apenas por uma borda em `changeOrderRequest`.

O ACK `saveSQLFinished` so pode ser escrito depois do commit SQL. Se houver queda
de conexao depois do commit e antes do ACK, a reconexao deve reconhecer que a
transacao ja foi aplicada e repetir somente a confirmacao necessaria.

## Estados dos pedidos

Os valores definitivos ainda precisam ser validados com dados reais do banco.
O comportamento mais consistente encontrado no codigo atual foi:

- `0`: aguardando na fila;
- `1`: em producao;
- `4`: finalizado/historico.

Foi proposta a criacao de um enum central, eliminando comparacoes dispersas como
`ProductionState < 1` e `ProductionState >= 4`.

Modelo preliminar, ainda sujeito a validacao:

| Valor | Nome | Uso proposto |
| ---: | --- | --- |
| 0 | `Queued` | Aguardando na fila |
| 1 | `InProduction` | Pedido atual confirmado pelo PLC |
| 2 | `Suspended` | Interrompido e recuperavel |
| 3 | `Cancelled` | Cancelado pelo operador |
| 4 | `Completed` | Concluido |
| 5 | `CompletedPartial` | Encerrado parcialmente |
| 6 | `SynchronizationError` | Requer analise |

Nao migrar dados ou adotar essa numeracao sem validar o banco de producao.

## Recuperacao do historico

Comportamento acordado:

- selecionar um pedido no historico;
- solicitar confirmacao;
- criar um novo registro na fila;
- preservar o registro historico original;
- atribuir novo `Id`;
- colocar na ultima sequencia valida;
- limpar horarios, quantidades produzidas e contadores de execucao;
- registrar usuario, horario e origem quando o esquema permitir;
- impedir duplo clique e duplicacao acidental.

Uma coluna futura como `RecoveredFromId` e recomendada, mas a primeira versao
pode funcionar sem alterar o esquema.

## Inversao superior e inferior

Para pedidos com os dois niveis ativos:

- trocar todos os campos `Order1*` pelos campos `Order2*` correspondentes;
- manter os campos comuns de papel e composicao;
- manter `LevelSelector = 3`;
- persistir somente apos confirmacao do operador;
- bloquear alteracao se o pedido ja estiver aceito pelo PLC ou em producao.

Campos que devem ser trocados em conjunto:

- ID/OF;
- cliente;
- produto;
- quantidade de chapas;
- tipo de chapa;
- M1 a M5;
- comprimento;
- numero de cortes;
- quantidade por pilha.

Para pedidos de nivel unico, ainda deve ser confirmada a regra de negocio. A
recomendacao preliminar e permitir a mudanca entre `LevelSelector = 1` e
`LevelSelector = 2`, mas primeiro deve ser corrigida e testada a interpretacao
do valor `2`.

## Ambiente de desenvolvimento acordado

O novo projeto pode ser desenvolvido integralmente no VS Code.

Instalacoes previstas no PC de desenvolvimento:

- VS Code estavel;
- .NET 10 SDK x64;
- Node.js 24 LTS x64;
- Git para Windows;
- navegador Edge ou Chrome;
- rota ADS ja existente;
- SSMS opcional.

Extensoes sugeridas:

- `ms-dotnettools.csdevkit`;
- `dbaeumer.vscode-eslint`;
- `esbenp.prettier-vscode`;
- `ms-vscode.powershell`;
- `ms-mssql.mssql`, opcional.

O C# Dev Kit suporta o projeto moderno, mas nao substitui o Visual Studio para o
Designer do WinForms legado.

Foi planejada a criacao posterior destes scripts PowerShell:

```text
scripts/
|-- Install-DevelopmentEnvironment.ps1
|-- Initialize-DevelopmentEnvironment.ps1
`-- Install-DryEndWindowsService.ps1
```

Esses scripts ainda nao foram criados. Eles devem:

- usar `winget` para VS Code, .NET 10, Node 24 LTS e Git;
- instalar extensoes do VS Code;
- validar as versoes instaladas;
- nao alterar a rota ADS;
- nao conter credenciais SQL;
- nao instalar SDKs Preview;
- manter a instalacao do Windows Service separada.

## Estrutura proposta

```text
PC/DryEnd.Next/
|-- src/
|   |-- DryEnd.Domain/
|   |-- DryEnd.Application/
|   |-- DryEnd.Infrastructure.Ads/
|   |-- DryEnd.Infrastructure.Database/
|   |-- DryEnd.Web/
|   `-- dryend-web-client/
|-- tests/
|   |-- DryEnd.Domain.Tests/
|   |-- DryEnd.Application.Tests/
|   `-- DryEnd.Infrastructure.Tests/
|-- scripts/
|-- README.md
`-- PROJECT_HANDOFF.md
```

A separacao e logica; a primeira implantacao continuara sendo um unico servico,
nao varios processos independentes.

## Plano de execucao original (historico)

As fases abaixo registram o plano inicial. A prova somente leitura foi
concluida e as escritas de `currentOrder` e `nextOrder` foram autorizadas em
2026-07-17 conforme a atualizacao no inicio deste documento.

### Fase 1 - prova de conceito somente leitura

1. Criar a solucao .NET e o frontend React.
2. Configurar o pacote ADS oficial.
3. Usar a rota existente sem modifica-la.
4. Ler `currentOrder`, `nextOrder` e watchdog.
5. Exibir conexao e snapshots no navegador.
6. Implementar reconexao automatica.
7. Registrar eventos em arquivo.
8. Confirmar que nao existe nenhuma chamada de escrita ADS.

### Fase 2 - dominio e persistencia

1. Criar modelos e estados centralizados.
2. Criar repositorio SQL.
3. Adicionar testes das transicoes.
4. Implementar recuperacao e inversao sem ADS.

### Fase 3 - modo sombra

1. Ler PLC e SQL.
2. Calcular a reconciliacao.
3. Registrar o que seria alterado.
4. Nao escrever no PLC nem mudar estados de producao.

### Fase 4 - escritas graduais

Habilitar separadamente e com configuracao explicita:

1. atualizacao de contadores no banco;
2. envio do proximo pedido;
3. confirmacao da troca;
4. comandos manuais.

### Fase 5 - interface completa e transicao

1. fila e edicao;
2. historico e recuperacao;
3. inversao superior/inferior;
4. graficos;
5. diagnostico e usuarios;
6. piloto controlado;
7. retirada gradual do WinForms.

## Criterios de aceitacao da primeira prova (concluida)

- Executa no PC atual pelo VS Code e `dotnet run`.
- Conecta usando a rota ADS existente.
- Le continuamente os simbolos acordados.
- Na etapa original nao possuia escrita ADS; atualmente as escritas autorizadas
  possuem validacao e readback.
- Mostra os dados em uma pagina React.
- Recupera-se automaticamente de interrupcao de rede.
- Nao coloca o runtime PLC em RUN.
- Fechar o navegador nao encerra o backend.
- Registra conexao, desconexao, reconexao e erros.
- Compila e executa sem depender do WinForms.

## Regras de seguranca para o proximo agente

- Manter as escritas limitadas aos fluxos explicitamente autorizados.
- Nao executar o WinForms e o novo backend como escritores simultaneos.
- Nao alterar PLC, HMI dedicada, rota ADS ou banco de producao incidentalmente.
- Nao copiar credenciais do `App.config` para documentacao, codigo ou logs.
- Nao introduzir `EnsurePlcIsRunning()` na nova aplicacao.
- Nao aceitar um simples `IsConnected` como prova de dados validos.
- Nao confirmar uma escrita antes de ler e validar o resultado.
- Manter nomes de codigo em ingles.
- Escrever comentarios de codigo em ingles e portugues, conforme
  `CONVENTIONS.md`.

## Primeira tarefa sugerida para o proximo agente

1. Ler os tres documentos de contexto.
2. Verificar o estado do Git e preservar alteracoes existentes.
3. Inventariar `dotnet`, `node`, `npm`, `git`, VS Code e ADS Router.
4. Criar os scripts de instalacao idempotentes.
5. Inicializar a solucao e os testes sem adicionar escrita ADS.
6. Implementar um cliente ADS somente leitura com configuracao externa.
7. Entregar uma pagina de diagnostico mostrando conexao, watchdog,
   `currentOrder.tableID` e `nextOrder.tableID`.

A escrita de `currentOrder` e `nextOrder` foi autorizada em 2026-07-17.
Qualquer outro comando ou ampliacao de escrita continua exigindo autorizacao
explicita.

## Atualizacao operacional em 2026-07-17

- `AdsPlcConnection` passou a ler cada DUT raiz inteira usando
  `SymbolLoaderSettings.DefaultDynamic`: uma operacao para `.currentOrder` e uma
  para `.nextOrder`, em vez de centenas de leituras campo a campo.
- A leitura conectada foi validada neste PC com PLC `Online`, incluindo strings,
  canais, medidas, facas, vincos e arrays com indices PLC iniciando em `1`.
- A versao Release local `0.1.1` foi publicada com o frontend em `wwwroot` e
  validada em `http://127.0.0.1:5099`.
- Na validacao produtiva, PLC e SQL ficaram disponiveis e os workers de
  sincronizacao, handshake e velocidade foram habilitados com autorizacao do
  operador e maquina parada.
- O procedimento completo para iniciar em modo somente leitura ou Release local,
  validar os endpoints e encerrar esta documentado na secao
  `Colocar o servidor local online` do `README.md`.
- Uma execucao mantida por terminal/agente nao substitui o Windows Service. Para
  operacao persistente use `DEPLOYMENT.md` e configuracao externa em
  `C:\ProgramData\CPNTeck\DryEnd\appsettings.Production.json`.
- Nesta copia sem `.git`, `Publish-DryEnd.ps1` concluiu testes, backend e frontend,
  mas falhou depois na coleta do commit para o manifesto. O diretorio `app` ficou
  executavel; o ZIP/manifesto nao deve ser considerado concluido nesse caso.
