# Contexto do projeto MR-BRASIL-DRYEND

## Objetivo deste documento

Este arquivo transfere o contexto técnico básico do projeto entre computadores e instâncias do Codex/ChatGPT.

Antes de alterar qualquer arquivo, leia este documento, confira `CONVENTIONS.md` e examine o estado atual do Git. O código-fonte é sempre a referência definitiva quando houver diferença entre este resumo e a implementação.

## Repositório

- Repositório: `MR-BRASIL-DRYEND`
- Branch principal: `main`
- Remote atual: `git@github.com:FabioTCampion/MR-BRASIL-DRYEND.git`
- Commit de referência ao criar este documento: `908b557`
- Mensagem do commit: `feat: atualiza seguranca alarmes e suporte EL1008`

## Estrutura principal

- `PLC/`: solução TwinCAT, programa PLC, configuração de hardware e projeto TwinSAFE.
- `HMI/`: projeto da interface de operação existente, pacotes, imagens e listas de alarmes. Não confundir esta pasta com a aplicação C# de `PC/`.
- `PC/`: aplicação HMI C# WinForms que será trabalhada no outro computador.
- `Tools/`: ferramentas auxiliares, incluindo diagnóstico e registro de variáveis via ADS.
- `CONVENTIONS.md`: convenções obrigatórias de desenvolvimento.

Existem duas famílias de HMI no repositório:

- `PC/HMI-DRYEND.sln`: aplicação Windows Forms responsável por pedidos, histórico, SQL e integração ADS.
- `HMI/DRY_END_MRBRASIL` e `HMI/DRY_END_MRBRASIL_10POL`: projetos da interface dedicada do operador, independentes da solução C#.

Não edite uma família supondo que ela atualiza automaticamente a outra. Ainda deve ser confirmado com a equipe qual variante dentro de `HMI/` é a fonte atualmente usada em produção.

## Projeto HMI para PC

O arquivo que deve ser aberto no Visual Studio é:

`PC/HMI-DRYEND.sln`

Não copie somente o arquivo `.sln`. Trabalhe com o repositório completo, pois a solução depende dos projetos, recursos, configurações e demais arquivos presentes na pasta `PC`.

O escopo atual dessa aplicação é principalmente gerenciamento de pedidos, histórico de produção, banco SQL e sincronização ADS. Ela ainda não deve ser considerada uma interface completa do novo stacker superior. As alterações recentes de stacker, Safety e novos alarmes feitas no PLC/HMI dedicado não aparecem automaticamente no projeto C#.

### Tecnologia e componentes

- Aplicação C# Windows Forms.
- .NET Framework 4.7.2.
- Solução gravada pelo Visual Studio 2019, versão 16.
- Configuração recomendada para a primeira compilação: `Any CPU`.
- Projeto principal: `PC/Hmi/HMI.csproj`.
- Biblioteca de teclado: `PC/Hmi/TextBoxWKeyBoard/TextBoxWKeyBoard.csproj`.
- Comunicação com o PLC por TwinCAT ADS.
- Integração com TwinCAT Event Logger por bibliotecas COM/interops.
- Uso de Dapper e LiveCharts.
- Dependências NuGet definidas em `PC/Hmi/packages.config`.
- Executável gerado pelo projeto principal: `SF1_Hmi`.

### Mapa da aplicação C#

- `PC/Hmi/Program.cs`: ponto de entrada; usa um `Mutex` para impedir duas instâncias simultâneas.
- `PC/Hmi/MainForm/frmMain.cs`: janela principal, navegação, conexão ADS, sincronização de pedidos e integração SQL.
- `PC/Hmi/SubForms/frmHome.cs`: tela inicial e acesso ao pedido atual/próximo.
- `PC/Hmi/SubForms/frmOrders.cs`: cadastro e manutenção da fila de pedidos.
- `PC/Hmi/SubForms/frmHist.cs`: histórico de produção.
- `PC/Hmi/SubForms/frmProductionGraph.cs`: gráficos de produção.
- `PC/Hmi/SubForms/Production Metrics/frmMachineSpeedGraph.cs`: gráfico de velocidade.
- `PC/Hmi/Data/Order/order_TypeStruct.cs`: modelo C# correspondente ao pedido do PLC.
- `PC/Hmi/Data/Order/PlcOrderMapper.cs`: conversão dos símbolos ADS para o modelo C#.
- `PC/Hmi/Data/Order/ProductionListPlc.cs`: modelo e acesso aos registros SQL de produção.
- `PC/Hmi/App.config`: configuração da conexão SQL; contém informação específica do ambiente e deve ser tratado como arquivo sensível.

Responsabilidades observadas nas telas atuais:

- `frmHome`: pedido atual/próximo, estimativas e comandos de troca de pedido/AOC.
- `frmOrders`: criação, edição e organização da fila no SQL.
- `frmHist`: histórico filtrado a partir dos estados considerados finalizados.
- `frmProductionGraph`: velocidade, médias e tempos de parada; a lógica atual considera velocidade abaixo de `10` como máquina parada.
- `frmMachineSpeedGraph`: permanece no projeto, mas não foi encontrado no fluxo de navegação principal e pode ser legado.

### Requisitos do computador de desenvolvimento

Antes de abrir e compilar, confirme a instalação de:

- Visual Studio 2019 ou versão compatível mais recente.
- Workload **Desenvolvimento para desktop com .NET**.
- .NET Framework 4.7.2 Developer/Targeting Pack.
- Cliente e bibliotecas TwinCAT ADS compatíveis com a versão 4.3 usada pelo projeto.
- Componentes do TwinCAT Event Logger necessários às referências COM.
- Acesso ao servidor SQL Server da aplicação, quando o teste exigir banco real.
- Git e credenciais para acessar o remote GitHub por SSH. Se o outro computador não possuir chave SSH autorizada, configure-a ou use uma URL HTTPS aprovada.

A interface principal foi desenhada para tela `1920 x 1080`, sem borda. No computador alvo, teste a mesma resolução e a escala/DPI do Windows usada na máquina; diferenças de DPI podem deslocar ou redimensionar controles WinForms.

O `PC/UpgradeLog.htm` registra que os projetos C# não foram carregados em uma tentativa anterior porque o tipo de projeto não estava instalado. Normalmente isso indica ausência do workload de desenvolvimento desktop/.NET, e não necessidade de converter a solução.

### Atenção ao abrir em outro computador

Algumas referências são dependentes da instalação ou do caminho local e podem aparecer como ausentes:

- `TwinCAT.Ads.dll` aponta para uma instalação local do TwinCAT.
- LiveCharts possui uma referência apontando para outro diretório de projeto, `DRY-END-COCELPA`, que não faz parte deste repositório. O pacote correspondente existe em `packages.config`, portanto confira o caminho depois da restauração NuGet.
- `TextBoxWKeyBoard.dll` aparece como referência a uma saída de build por um caminho relativo que não corresponde claramente à localização atual do projeto fonte. Compile primeiro `TextBoxWKeyBoard` e confira a referência. Se necessário, avalie substituir a referência binária por uma referência ao projeto, mas faça isso como uma alteração separada e revisável.
- As referências COM do TwinCAT Event Logger exigem os componentes correspondentes instalados e registrados no Windows.
- A solução oferece configurações com nomes de destinos TwinCAT, mas os projetos C# estão mapeados para `Any CPU`. Valide arquitetura e referências COM antes de escolher x86 ou x64.
- Os nomes `TwinCAT CE7`, `TwinCAT OS` e `TwinCAT RT` presentes na solução apenas selecionam configurações mapeadas para `Any CPU`; eles não comprovam que a aplicação e suas dependências executam no Windows CE/ARM.

Ao encontrar referências ausentes, primeiro identifique a versão originalmente usada e corrija somente os caminhos ou referências necessárias. Não atualize pacotes, framework ou APIs em massa apenas para eliminar avisos.

### Procedimento inicial de abertura e build

1. Abra `PC/HMI-DRYEND.sln` sem executar a aplicação.
2. Confirme que os dois projetos foram carregados.
3. Execute a restauração dos pacotes NuGet definidos em `packages.config`.
4. Confira as referências `TwinCAT.Ads`, LiveCharts, `TextBoxWKeyBoard` e as referências COM.
5. Compile primeiro `TextBoxWKeyBoard` e depois a solução em `Debug | Any CPU`.
6. Revise todos os erros de build antes de alterar código funcional.
7. Não inicie a HMI conectada à rede da máquina até revisar a seção ADS abaixo.

Não existe atualmente um modo offline/simulação nem seleção de ambiente centralizada. O AMS Net ID está no código e a conexão SQL está em `App.config`; a inicialização tenta acessar ambos. Criar uma configuração segura de desenvolvimento deve ser uma tarefa deliberada e separada.

## Contrato de integração entre HMI C# e PLC

### Comunicação ADS

A comunicação principal está em `PC/Hmi/MainForm/frmMain.cs`:

- AMS Net ID atualmente definido no código: `192.168.30.79.1.1`.
- Porta do runtime PLC: `851`.
- Porta do System Service: `10000`.
- Símbolos globais principais: `currentOrder` e `nextOrder`.
- Declaração correspondente no PLC: `PLC/.../Plc_Program/GVLs/GVL_Hmi.TcGVL`.
- Os nomes aparecem na raiz do ADS devido ao atributo TwinCAT `{attribute 'Tc2GvlVarNames'}`. Remover esse atributo ou renomear os símbolos quebra a busca dinâmica atual.
- O carregamento usa símbolos dinâmicos e eventos `ValueChanged`.
- O modelo local é atualizado por `PlcOrderMapper.FromPlc`.
- A comunicação é monitorada por `currentOrder.plcWatchDog` e pela atualização dos eventos; o limite atual é de aproximadamente três segundos.

O computador de desenvolvimento precisa de rota ADS válida até o target, e o target precisa reconhecer a rota de retorno. Não altere o AMS Net ID fixo sem confirmar qual ambiente será usado.

### Advertência operacional crítica

Ao iniciar, a aplicação executa `TryReconnectAds()` e `AdsConnect()`. Durante essa sequência, `EnsurePlcIsRunning()` acessa a porta `10000` e, se o runtime não estiver em `RUN`, pode enviar um comando de controle para levá-lo a `RUN`.

Portanto:

- Não execute a HMI de desenvolvimento ligada à rede da máquina sem autorização e preparação do teste.
- Para teste offline, bloqueie a rota para o PLC de produção ou use um target de laboratório/simulação.
- Trate qualquer proposta para remover ou modificar `EnsurePlcIsRunning()` como mudança funcional separada; não altere esse comportamento incidentalmente durante a integração visual.

### Compatibilidade dos dados

Os layouts do PLC e do C# precisam permanecer compatíveis:

- PLC: `Order_TypeStruct` e variáveis persistentes `currentOrder`/`nextOrder`.
- C#: `order_TypeStruct.cs` e `PlcOrderMapper.cs`.
- Banco: `ProductionListPlc.cs` e colunas de `dbo.ProductionList_Plc`.

Os principais DUTs de referência ficam em `PLC/.../Library/Machine/DryEnd/Order control/DataTypes/`, incluindo `order_TypeStruct.TcDUT`, `orderVariables_TypeStruct.TcDUT` e `generatedOrder_TypeStruct.TcDUT`.

Quando um campo do pedido for incluído, removido ou tiver seu tipo alterado, revise os três lados: PLC, modelo/mapeador C# e SQL. Confirme também os pontos de escrita em `frmMain.WriteNextOrderToPlc()` e os bindings dos formulários.

A HMI escreve dados no PLC; ela não é somente leitura. Entre as operações existentes estão a carga completa de `nextOrder`, comandos de troca/AOC e confirmações da sincronização SQL.

O mapeamento de arrays também exige atenção: índices TwinCAT normalmente começam em `1`, enquanto arrays/listas C# começam em `0`. Confirme explicitamente qualquer tradução de índice em `PlcOrderMapper` antes de alterar ferramentas, medidas ou status.

Nem todos os campos presentes no PLC estão necessariamente representados no modelo C#. Por exemplo, estruturas geradas e palavras de status podem existir apenas no lado TwinCAT, enquanto alguns comandos são acessados dinamicamente pela HMI. Não suponha que o modelo C# seja uma cópia completa do DUT.

Há um possível conflito de tipo que precisa ser validado antes de refatorar o pedido: `ProductionListPlc.ProductionListNumber` é tratado como `string`/VARCHAR no C#, enquanto `Order_TypeStruct.productionListNumber` é `DINT` no PLC. A rotina atual escreve o valor diretamente no símbolo ADS.

### Fluxo de pedidos e handshake

- Com a comunicação saudável, a HMI consulta e atualiza `nextOrder` periodicamente.
- A borda de `currentOrder.changeOrderRequest` inicia a transação SQL que encerra o pedido anterior e promove o próximo.
- Ao concluir essa operação, a HMI escreve `currentOrder.saveSQLFinished := TRUE` como confirmação.
- O PLC possui timeout de aproximadamente 5 segundos para essa confirmação em `FB_MetricsAndCalculation`.
- A tela inicial também pode comandar `aocRequest`.
- No comportamento atual, `order1` é o pedido principal; `order2` é preenchido quando `levelSelector = 3`, correspondente ao trabalho com os dois níveis.

Preserve esse handshake ponta a ponta. Um teste apenas visual não valida troca de pedido, e um teste conectado pode escrever no PLC e alterar registros SQL.

### Integração SQL

- A connection string é obtida pelo nome `cn` em `PC/Hmi/App.config`.
- O banco esperado é SQL Server/SQL Express e usa a base `DryEnd` no ambiente atual.
- Tabelas usadas diretamente incluem `dbo.ProductionList_Plc`, `dbo.MachineSpeedRecords` e `MachineStopedTimePerHour`.
- Não foi encontrado script versionado para criação completa do banco. Para desenvolvimento isolado, será necessário obter esquema e dados de teste por um canal autorizado.
- `App.config` contém credenciais em texto simples. Não copie esses valores para este documento, mensagens ou logs. Considere posteriormente externalizar/rotacionar as credenciais como uma tarefa específica, sem interromper a integração atual.

Há suposições divergentes sobre `ProductionState` no código: a lógica principal usa `1` para produção e `4` para finalizado; uma atualização periódica procura o estado `2`; e um método possivelmente legado em `ProductionListPlc.cs` consulta o estado `3` como atual. O histórico usa estados a partir de `4`. Antes de alterar consultas ou fluxo de pedidos, confirme a tabela de estados válida na máquina e unifique a documentação.

### Temporizações relevantes da HMI

- Atualização do próximo pedido: 5 segundos.
- Registro de velocidade: 60 segundos.
- Atualização da interface: 1 segundo.
- Sincronização do pedido atual com SQL: 30 segundos.
- Verificação da conexão: 1 segundo.

Alterações nesses timers podem afetar carga do banco, tráfego ADS e handshakes; devem ser testadas como mudança funcional.

## Ferramentas de diagnóstico

`Tools/AdsLogger` é separado da HMI C# e não é uma dependência do build. Consulte `Tools/AdsLogger/README.md` antes de usar.

As ferramentas existentes incluem:

- Logger contínuo do Complex Stacker.
- Diagnóstico único das posições de facas e vincos.
- Monitor automático disparado por mudança de pedido.
- Relatório histórico de variação das ferramentas.

O logger do stacker foi desenvolvido para leitura ADS e grava os resultados em `Tools/AdsLogger/Logs`, pasta ignorada pelo Git. Mesmo assim, valide AMS Net ID, porta e lista de símbolos antes de executar em outro ambiente.

## Contexto do PLC e da máquina

O projeto controla a linha Dry End e possui, entre outros equipamentos:

- Slitter e posicionamento das ferramentas de corte e vinco.
- Rotary Knife.
- Automatic Order Change.
- Simple Sheet Stacker, usado no stacker inferior.
- Complex Sheet Stacker, usado no stacker superior.
- Backstop com servo Inovance IS620P-CO controlado por referência de velocidade e feedback de posição, sem NC do PLC.
- Elevador do stacker superior controlado por velocidade e feedback de posição.
- Esteiras de descarga e esteiras de buffer para as pilhas prontas.
- Configuração de hardware Beckhoff e projeto TwinSAFE.

Alterações recentes incluem suporte ao terminal Beckhoff EL1008, estruturas do EL6910/TwinSAFE e alarmes genéricos de feedback das emergências. Os sinais de feedback de emergência são usados para diagnóstico e alarmes; a função de segurança efetiva vem do circuito/projeto Safety.

## Regras de desenvolvimento

- Nomes de variáveis, métodos, funções, tipos e demais elementos de programação devem estar em inglês.
- Comentários devem ser escritos em inglês e português.
- Preserve o padrão já usado nos function blocks, estruturas, data types e métodos existentes.
- Não crie variáveis ou abstrações sem necessidade comprovada.
- Antes de alterar uma lógica de máquina, leia o bloco de referência em produção e entenda a sequência completa.
- Não altere simultaneamente lógica PLC, mapeamentos HMI e hardware sem separar e validar claramente o impacto.
- Preserve alterações existentes do usuário e evite formatação ou regravação desnecessária de arquivos TwinCAT e WinForms Designer.

## Orientação para uma nova instância do Codex/ChatGPT

Ao iniciar o trabalho no outro computador, use uma instrução semelhante a esta:

> Leia completamente `PROJECT_CONTEXT.md` e `CONVENTIONS.md`. Depois confira o estado do Git e analise `PC/HMI-DRYEND.sln`, os projetos associados e as referências atuais antes de modificar qualquer arquivo. Preserve o padrão existente e não faça atualizações de dependências sem explicar a necessidade.

Para uma tarefa específica, informe também:

- Qual tela, formulário ou funcionalidade deve ser alterada.
- Quais variáveis do PLC precisam ser lidas ou escritas.
- Se a tarefa é somente diagnóstico ou se autoriza alterações.
- Como o comportamento será validado na máquina ou em ambiente de teste.

## Fluxo Git recomendado

O Git permite trabalhar em outro computador com segurança, desde que as duas máquinas estejam sincronizadas e não editem a mesma branch sem coordenação.

### Antes de começar no outro computador

1. Neste computador, finalize ou registre as alterações pendentes em um commit.
2. Faça `push` para o repositório remoto.
3. No outro computador, clone o repositório completo ou execute `git pull` se ele já existir.
4. Confirme o estado com `git status` e o commit atual com `git log -1 --oneline`.
5. Não comece se existirem alterações locais desconhecidas; primeiro identifique sua origem.
6. Confirme que `PROJECT_CONTEXT.md` aparece em `git ls-files`; um arquivo não rastreado ou sem `push` não chegará ao outro computador.

### Criar uma branch para a HMI

Use uma branch separada para proteger a versão em produção:

```powershell
git switch main
git pull --ff-only
git switch -c codex/hmi-dryend
```

Se a branch já existir no remoto:

```powershell
git fetch origin
git switch codex/hmi-dryend
git pull --ff-only
```

### Durante o trabalho

- Faça alterações pequenas e relacionadas ao mesmo objetivo.
- Antes do commit, revise `git status` e `git diff`.
- Compile a solução quando o ambiente permitir.
- Não inclua senhas, credenciais, logs de produção ou dados sensíveis.
- Não force a inclusão de arquivos ignorados.
- Não use `git reset --hard`, force push ou comandos destrutivos sem necessidade e autorização explícita.
- Não copie nem versione chaves SSH privadas. Cada computador deve possuir sua própria credencial de acesso ao GitHub.
- Para tarefas somente no C#, evite abrir ou salvar os projetos dentro de `HMI/`, pois os editores dessa interface podem atualizar muitos arquivos gerados já rastreados.

Exemplo de registro:

```powershell
git status
git diff
git add PC PROJECT_CONTEXT.md
git commit -m "feat: atualiza interface HMI Dry End"
git push -u origin codex/hmi-dryend
```

### Arquivos locais e gerados

O `.gitignore` da raiz já exclui, entre outros:

- `.vs/`
- `bin/` e `obj/`
- `Debug/` e `Release/`
- `*.user` e `*.suo`
- `packages/`, restaurável por `packages.config`
- arquivos gerados pelo TwinCAT
- logs locais do ADS Logger

Não remova essas regras apenas porque algum arquivo aparece ausente no outro computador. Restaure dependências e gere os artefatos localmente.

O `.gitignore` não impede mudanças em arquivos que já estavam versionados. Por isso, depois de abrir qualquer editor, confira `git status` antes de selecionar arquivos para commit. Não faça limpeza automática de arquivos aparentemente estranhos; existe, por exemplo, um `frmOrders .resx` com espaço no nome que está rastreado, embora não esteja claramente incluído no projeto.

### Integração das alterações

Depois de testar:

1. Faça push da branch.
2. Compare a branch com `main`.
3. Revise principalmente arquivos `.csproj`, `.sln`, `App.config`, arquivos Designer e mapeamentos ADS.
4. Integre somente após confirmar que não existem alterações concorrentes incompatíveis.
5. No computador da máquina, atualize o repositório de forma controlada e mantenha um commit conhecido para rollback.

## Checklist de validação da HMI C#

Não há atualmente testes automatizados ou pipeline de CI no repositório. A integração deve ser validada em etapas:

1. Solução abre com os dois projetos carregados.
2. NuGet restaura sem depender de diretórios externos inesperados.
3. `TextBoxWKeyBoard` compila e é resolvido pelo projeto principal.
4. Build `Debug | Any CPU` e depois `Release | Any CPU` conclui sem erros.
5. Executável `SF1_Hmi.exe` inicia em ambiente isolado sem acesso ao PLC de produção.
6. Conexão SQL é testada com base de homologação ou cópia autorizada do esquema.
7. Conexão ADS é testada primeiro contra runtime de laboratório/simulação.
8. Leitura de `currentOrder`, escrita de `nextOrder`, watchdog e reconexão são conferidos.
9. Troca de pedido e ACK `saveSQLFinished` são validados ponta a ponta, incluindo timeout.
10. Telas Home, Orders, History e gráficos são verificadas com dados conhecidos.
11. Layout é verificado em `1920 x 1080` e na escala/DPI real do computador alvo.
12. Antes do deploy, guarde o executável/configuração atualmente em produção e registre o commit correspondente para rollback.

O processo definitivo de publicação/cópia do executável ainda não está documentado no repositório e deve ser confirmado no computador onde a HMI opera antes da primeira implantação.

## Pontos conhecidos que não devem ser corrigidos incidentalmente

Os itens abaixo merecem tarefas próprias, com build e teste separados:

- Padronização dos valores de `ProductionState` usados nas diferentes consultas.
- Compatibilidade de tipo de `productionListNumber` entre SQL/C# e PLC.
- Substituição dos caminhos locais de LiveCharts, TwinCAT ADS e `TextBoxWKeyBoard`.
- Externalização e rotação das credenciais SQL atualmente presentes no `App.config`.
- Criação de um modo de desenvolvimento/offline que não conecte automaticamente ao PLC de produção.
- Confirmação da arquitetura x86/x64 exigida pelas referências COM no computador alvo.
- Revisão de arquivos possivelmente legados, como `ProductionList.cs`, `ProductionListPlcRepository`, `frmMachineSpeedGraph` e `frmOrders .resx`.
- Revisão de `Properties/Settings.settings`: o projeto contém indícios de nomes de arquivo gerado diferentes, o que pode fazer outra versão do Visual Studio regenerar arquivos inesperadamente.

Não remova código ou arquivos apenas porque não há uso estático evidente. Eventos do Designer, reflection, bindings e componentes COM podem criar dependências que não aparecem em uma busca simples.

## Cuidados operacionais

- O projeto está associado a uma máquina em produção.
- Um commit registra alterações, mas não transfere automaticamente código para o PLC, HMI ou hardware.
- `push` envia o histórico ao servidor Git; não realiza download na máquina de produção.
- Build, download, ativação de configuração e restart do TwinCAT são ações separadas e devem ser executadas conscientemente.
- Antes de qualquer teste online, confirme permissivos, Safety, limites físicos, sentido de movimento e possibilidade de intervenção manual.

## Manutenção deste documento

Atualize este arquivo quando houver mudanças relevantes na arquitetura, dependências, fluxo de build, comunicação ADS ou decisões de implementação. Registre fatos duráveis e decisões técnicas; logs temporários e detalhes momentâneos devem permanecer nas ferramentas de diagnóstico ou no histórico dos commits.
