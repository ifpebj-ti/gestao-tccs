# Plano e matriz de alinhamento da auditoria cruzada

Status: preparação; acesso ao projeto auditado ainda não concedido na informação recebida em 15/11/2026.

Este documento corresponde ao entregável de alinhamento descrito no PDF da atividade. Os campos pendentes devem ser acordados entre os times; não constituem resultados de auditoria.

## Identificação e acessos

| Campo | Situação |
| --- | --- |
| Time auditor e integrantes | Preencher com os participantes confirmados. |
| Time auditado e integrantes | A equipe informa que conhece o time; identificação ainda não registrada neste documento. |
| Repositório auditado | Aguardar link e acesso de leitura. |
| GitHub Projects auditado | Aguardar link e acesso de leitura. |
| Ambiente de testes | Acordar endereço ou execução local isolada, responsável e dados fictícios. |
| Commit/tag inicial | Selecionar após obter acesso. |
| Entrevista | Combinar data, duração e participantes. |
| Janela da análise e entrega | Confirmar com o professor e o time auditado. |
| Canal de comunicação e achados sensíveis | Combinar canal privado e responsáveis antes de compartilhar detalhes de exploração. |

## Escopo acordado

| Frente | Procedimento | Evidência | Dependência |
| --- | --- | --- | --- |
| Processo e rastreabilidade | Selecionar amostra explícita de PRs e mapear tarefas, commits, revisões e releases | Links, critérios de amostragem, datas e contagens | Leitura do repositório e Projects |
| Branching e review | Comparar política documentada com histórico e regras demonstradas | PRs, comentários técnicos e rulesets | Acesso de leitura e evidência das configurações pelo responsável |
| SAST/SCA | Revisar configuração, resultados e falsos positivos; repetir scans no commit selecionado | Relatórios, versões das ferramentas e comandos | Fonte e acesso aos relatórios |
| Testes e cobertura | Verificar execução, cenários, falhas, skips e escopo da cobertura | Logs, relatórios e configuração | Ambiente isolado e dados de teste |
| DAST | Executar somente os testes acordados no ambiente de teste | Requisições sanitizadas, respostas e relatório | Autorização do alvo, contas fictícias e limites de execução |
| Segredos | Revisar histórico e triagem sem reproduzir credenciais | Localização e estado, com conteúdo redigido | Canal privado e responsáveis pelos ambientes |
| Arquitetura e operação | Conferir documentação com código e ambiente demonstrado | ADRs, diagramas e limitações | Documentação e entrevista |

Não executar testes ativos em produção ou serviços externos por inferência. Limites, alvos e ações permitidas devem ser combinados com o time auditado. Evitar testes destrutivos e dados reais.

## Roteiro da entrevista

1. Como uma necessidade entra no backlog e ganha critérios de aceite?
2. Qual é a meta da sprint e onde ela está registrada?
3. Como a equipe define pronto e prioriza bugs e vulnerabilidades?
4. Como uma tarefa chega à branch, PR, revisão e publicação?
5. O que os revisores conferem e como registram sua análise?
6. O que ocorre quando um teste ou scan falha?
7. Como são tratados falsos positivos e credenciais expostas?
8. Quem atualiza os ambientes e como a equipe recupera uma publicação com problema?
9. Quais etapas não estão implantadas ou dependem de outra equipe?

Registrar respostas com suas evidências. Diferenciar prática relatada de prática observada; não completar respostas em nome dos colegas.

## Métricas e limites

- Rastreabilidade: PRs da amostra com vínculo verificável a tarefa ou requisito / total de PRs avaliadas × 100. Registrar amostra e período. Links retrospectivos devem ser identificados.
- Review: registrar quantidade de revisores, comentários e análises relevantes por PR. A contagem não substitui a avaliação do conteúdo; não criar comentários artificiais.
- Automação e bloqueio: aplicar a escala 0–5 do PDF somente após conferir testes, scans, revisão obrigatória e DAST. Workflow configurado ou badge verde, isoladamente, não comprova score 5.
- Cobertura: registrar métrica, camada, coletor, exclusões e SHA. Não apresentar cobertura do backend como cobertura de todo o produto.

## Saída esperada

Após acesso, análise e entrevista, preencher o [modelo do relatório integrado](modelo-relatorio-integrado.md), incluindo achados reproduzidos, limitações, métricas e plano de ação. A entrevista e a revisão devem ser reais; um modelo preenchido não substitui sua realização.
