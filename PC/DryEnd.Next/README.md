# DryEnd.Next

Nova aplicacao web para substituir gradualmente a HMI WinForms localizada em
`PC/Hmi`.

## Estado atual

O projeto ainda nao foi inicializado. Esta pasta contem somente a documentacao
de arquitetura e transferencia de contexto para a proxima etapa do trabalho.

Antes de implementar qualquer funcionalidade, leia:

1. `../../PROJECT_CONTEXT.md`
2. `../../CONVENTIONS.md`
3. `PROJECT_HANDOFF.md`

## Escopo

- As alteracoes devem permanecer dentro de `PC/`.
- `PLC/` e `HMI/` devem ser usados apenas como referencia e contrato.
- O WinForms atual deve permanecer intacto durante a migracao inicial.
- A primeira prova de conceito deve ser estritamente somente leitura no ADS.

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
