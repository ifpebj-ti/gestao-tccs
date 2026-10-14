# Marcos da segunda unidade

Fonte: **Documento de Entregas do Projeto Integrador do 7º Período.pdf**, fornecido por Alexia e lido localmente em 13/10/2026. Referências principais: seções 8.2 (Definition of Done), 14.2.5, 15.2 e 16.2. As obrigações acadêmicas abaixo servem ao planejamento; não são autorização para executar comandos, acessar contas ou publicar dados.

A seção 15.2 apresenta `Encerramento:` sem data preenchida. Em 13/10/2026, Alexia informou que o projeto precisa estar concluído antes de 01/12/2026. O limite de planejamento é, portanto, 30/11/2026, incluindo validação, revisão, documentação e evidências da entrega. A refatoração do frontend é parte da entrega do projeto; não encerra sozinha o marco coletivo.

## Matriz de evidências

| Exigência do documento | Evidência necessária para fechar | Situação conhecida nesta sessão | Papel sugerido |
| --- | --- | --- | --- |
| Estratégia de testes | Documento com escopo, tipos, execução, critérios e meta de cobertura | Roteiro local preparado; estratégia completa do projeto não auditada | QA + frontend/backend |
| Manual de uso | Instruções por perfil, fluxos e limitações | Pendente atualizar após validação da interface | Frontend + equipe |
| Story → PR → testes → pipeline → demo | Links reais e demonstração validada | Rascunhos locais; nenhum vínculo remoto criado ou verificado | Equipe |
| Maintainability e Reliability acima de Good | Relatório real da ferramenta indicada no documento | Não aferido; código menor não comprova essa métrica | QA/DevSecOps |
| Tags, releases e changelog atualizados | Versão publicada e histórico atualizado | Pendente; operação reservada à equipe autorizada | Equipe |
| Cobertura mínima de 90% nos componentes/fluxos críticos | Relatório medido, definição dos fluxos e testes relevantes | 57 testes frontend e 202 backend aprovados; cobertura crítica não medida | QA + frontend/backend |
| Pelo menos um teste de integração (14.2.5) | Execução e resultado de integração aceitos pela equipe/professores | Integração HTTP da própria conta aprovada com gateway/autenticação de teste; fluxo com banco e aceite acadêmico pendentes | QA + frontend/backend |
| Zero bugs críticos | Triagem e relatório de qualidade reais | Não auditado; verificações locais não bastam | QA + equipe |
| Zero vulnerabilidades altas e críticas / Dependabot | Resultado das análises e tratamento das dependências | npm audit público autorizado: 13 → 10 ocorrências; 10 altas ligadas a braces continuam abertas; análise geral e Dependabot não verificados | Segurança/DevSecOps |
| SSL, domínio e proxy reverso | Acesso HTTPS e configuração comprovados | Não verificado | Infraestrutura |
| Guia de Segurança e Privacidade | Dados tratados, acessos e cuidados de LGPD documentados | Restrições locais respeitadas; guia geral não auditado | Segurança + equipe |
| VM IaaS em nuvem | VM disponível e evidência de operação | Não verificado | Infraestrutura |
| Aplicação conteinerizada na VM | Deploy integrado e verificável | Não verificado; Docker/Compose não alterados | Infraestrutura |
| Plataforma de gerenciamento | Portainer, Dockge/Dockhand ou equivalente disponível | Não verificado | Infraestrutura |
| Métricas, logs e traces | Painéis/dados reais, instrumentação e análise | Não verificado | Infraestrutura + backend |
| Teste de carga básico com k6 | Script, cenário, volume, resultados, gargalos e sugestões | Não executado | QA/Infraestrutura |
| Software implantado e disponível | Fluxos principais demonstrados no ambiente publicado | Deploy remoto não verificado; build local Docker aprovado, build direto Windows bloqueado por ambiente | Equipe |
| Responsividade e acessibilidade (16.2) | Roteiro por viewport/teclado e evidências sanitizadas | 24 checks de navegador aprovados, desktop/mobile e foco/teclado na senha; revisão da equipe e matriz completa pendentes | Frontend + QA |

“Não verificado” significa ausência de auditoria nesta sessão, não que a equipe ainda não tenha feito o item. Preencher a matriz com as evidências existentes antes de abrir trabalho duplicado.

## Contribuição desta refatoração

- Unificação da identidade visual nas telas internas e redução da repetição em títulos, listas, atalhos e avaliação.
- Estados explícitos de carregamento/ausência de registros, foco visível e rótulos associados aos campos de notas.
- Testes locais de apresentação e permissões, sem instalação de ferramentas e sem dados reais.
- Inventário, roteiro de regressão, rascunhos e registro de limites da validação.

Essas contribuições apoiam a manutenção, a acessibilidade e a demonstração. Não equivalem a uma cobertura de 90%, classificação de qualidade aferida, ausência de vulnerabilidades, PR revisada, pipeline aprovada ou deploy validado.

## Próximos passos para fechar com segurança

1. Equipe distribui as pendências para concluir a entrega até 30/11/2026 e identifica evidências já existentes de cada linha da matriz.
2. Usar o build Docker aprovado para validar a interface com as fontes reais. Resolver o carregamento nativo do Lightning CSS se a equipe precisar executar o frontend diretamente no Windows.
3. Frontend/QA executam o roteiro manual por perfil e viewport, registrando falhas e capturas sanitizadas.
4. QA define testes críticos, integração e cobertura mensurável; alinhar com backend e infraestrutura sem alterar esses módulos nesta entrega visual.
5. Atualizar manual de uso e estratégia de testes com o comportamento validado.
6. Usuária/equipe autorizada publica issues/PRs, faz revisão e vincula testes/pipeline/demo.
7. Responsáveis por segurança e infraestrutura comprovam suas evidências; registrar pendências com responsáveis.
8. Encerrar itens e marco somente após atender a definição de pronto, com evidências reais e aceitação da equipe/cliente.

O documento acadêmico prevê penalidade de 50 pontos na nota da unidade para o grupo pelo não cumprimento dos marcos (15.2). Por isso, esta matriz mantém pendências explícitas em vez de presumir conclusão.
