# Preparação para a auditoria cruzada de segurança e processo

Data de preparação: 15/11/2026.

## Referência e limites

Este roteiro considera a atividade “Auditoria de Processo e de Qualidade de Desenvolvimento de Software”, fornecida pelo professor, e o parecer de pendências do GradusFlow. O PDF é uma referência de critérios, não uma evidência de conformidade do projeto.

O trabalho possui dois papéis: preparar o GradusFlow para receber a auditoria e, em momento distinto, auditar o projeto de outra equipe. Este documento não é o relatório independente dos auditores e não atribui nota ao GradusFlow.

O plano de testes e a revisão da Wiki/README permanecem com a integrante responsável. A implantação na OCI ainda depende da disponibilidade da conta. Não apresentar o ambiente temporário do CI como uma implantação na nuvem.

## Acessos e alinhamento

| Recurso | Referência ou situação |
| --- | --- |
| Repositório GradusFlow | https://github.com/ifpebj-ti/gestao-tccs |
| Quadro de trabalho | https://github.com/orgs/ifpebj-ti/projects/38 |
| Releases | https://github.com/ifpebj-ti/gestao-tccs/releases |
| Política de segurança | [SECURITY.md](../../../SECURITY.md) |
| Fluxo de contribuição | [CONTRIBUTING.md](../../../CONTRIBUTING.md) |
| ADRs existentes | [Gates e deploy separado](../../adr/0001-gates-de-seguranca-e-deploy-separado.md) e [recuperação e autorização de arquivos](../../adr/0002-recuperacao-e-autorizacao-de-arquivos.md) |
| Acesso do outro time | A equipe informou que ainda não recebeu acesso ao projeto que irá auditar. |
| Acesso dos auditores ao nosso Projects | Confirmar permissão de leitura com os auditores; ter um link não comprova acesso. |
| Ambiente de testes para os auditores | Combinar ambiente isolado, acesso e dados fictícios. Ainda não foi fornecido um endereço compartilhado. |
| Entrevista | Agendar com a equipe. O PDF menciona 15 e 20 minutos em trechos distintos; confirmar o tempo adotado. |

## Matriz de preparação do GradusFlow

| Critério | Evidência existente | O que ainda conferir |
| --- | --- | --- |
| SAST | Semgrep e Opengrep aprovados no CI da versão selecionada; captura de Code scanning da main com zero abertos e sete encerrados | Preservar os relatórios; conferir os motivos de encerramento se necessário. |
| SCA | npm, NuGet transitivo e Trivy aprovados; captura de Dependabot com zero abertos e 118 encerrados | Não interpretar os 118 encerrados como 118 correções verificadas individualmente. |
| DAST | Workflow com frontend/API em ambiente temporário | Preservar relatórios e delimitar endpoints, autenticação e fluxos efetivamente cobertos. Scan automatizado não comprova auditoria completa da aplicação. |
| Segredos | Gitleaks aprovado; Default sem alertas na captura; #794 encerrada após verificação local e confirmação da outra responsável | Guardar os registros sanitizados e confirmar o estado final privado do alerta genérico #4; não publicar a chave. |
| Testes e cobertura | Release 2.14.0; 573 testes backend e 107 frontend na validação registrada; gate backend de 95% | Guardar TRX/OpenCover e CI do mesmo SHA. A contagem e o percentual não substituem avaliação de qualidade dos cenários. |
| Proteção de branches | API pública confirma main e develop protegidas e rulesets ativos, com uma aprovação, Quality gate e ausência de bypass | Evidência pontual de 15/11/2026; repetir se as configurações forem alteradas. |
| Revisão por pares | Aprovações registradas nas PRs | Tornar explícito o que o revisor realmente verificou e as limitações da revisão. |
| Rastreabilidade | Issues, Projects, PRs e releases públicos | Vincular a tarefa correta a cada mudança. Registrar retrospectivas como retrospectivas, sem inventar vínculo prévio. |
| Arquitetura e documentação | ADRs, Wiki, SECURITY e CONTRIBUTING | A responsável revisa README/Wiki: arquitetura textual, equipe/papéis, Projects, Garage e páginas de ameaças duplicadas. |
| Changelog | Histórico reconstruído e geração de branch para abertura manual | Conferir a entrada da última release e integrar a atualização documental quando preparada. |
| Releases e imagens | Publicação multiarch validada nas entregas recentes | Conciliar lacunas antigas e a imagem órfã citada no parecer; não reconstruir história sem evidência. |
| Infraestrutura operacional | Deploy separado e manual | OCI, HTTPS, segredos exclusivos, acessos, observabilidade e recuperação precisam de validação no ambiente implantado. |

## Amostra de processo consultada

Consulta pública em 15/11/2026. Amostra deliberada de três PRs recentes; não representa todo o histórico do projeto.

| PR | Integração registrada | Aprovações registradas | Comentários inline de review | Reviews com texto |
| --- | --- | --- | --- | --- |
| [#821](https://github.com/ifpebj-ti/gestao-tccs/pull/821) | Sim | 1 | 0 | 0 |
| [#823](https://github.com/ifpebj-ti/gestao-tccs/pull/823) | Sim | 1 | 0 | 0 |
| [#824](https://github.com/ifpebj-ti/gestao-tccs/pull/824) | Sim | 1 | 0 | 0 |

As aprovações demonstram a participação de outro integrante. A ausência de texto não prova que a revisão não ocorreu, mas limita a evidência sobre sua profundidade. As descrições de #823 e #824 também apresentavam “Issue vinculada: não informada”; a rastreabilidade dessas entregas deve ser esclarecida com a tarefa correta, sem alterar ou reescrever commits publicados.

Para as próximas PRs, o revisor deve registrar de forma curta: componentes examinados, resultados de testes realmente consultados ou executados, riscos considerados e conclusão. Não inserir comentários fictícios, reproduzir uma revisão não realizada ou criar sugestões apenas para aumentar a métrica. Uma revisão sem pedidos de alteração pode ser válida se sua análise estiver fundamentada.

## Pacote de evidências

Selecionar um commit ou tag e manter essa referência em todos os registros. Para cada evidência, anotar:

| Campo | Conteúdo esperado |
| --- | --- |
| Identificação | Controle, achado ou critério demonstrado. |
| Origem | URL de PR, issue, execução ou relatório. |
| Versão | SHA completo, tag e ambiente avaliado. |
| Ferramenta | Nome, versão e configuração relevante. |
| Resultado | Quantidade e severidade dos achados, testes aprovados/falhos/ignorados e cobertura com seu escopo. |
| Limitações | Partes não acessadas, testes não executados ou serviços simulados. |
| Responsável e data | Pessoa que conferiu e data da coleta. |

Baixar os artifacts selecionados antes de expirarem; a retenção configurada é limitada. Guardar relatórios completos em local de acesso apropriado e publicar somente a versão revisada, sem dados pessoais, tokens, cookies ou conteúdo de documentos acadêmicos. Não copiar o conteúdo de segredos para comprovar uma ocorrência.

## Conferência dos alertas com acesso da equipe

1. Em Security and quality, abrir Dependabot Vulnerabilities e registrar contagens por severidade, pacote, arquivo, versão afetada, correção disponível e link do alerta.
2. Abrir Code scanning e registrar regra, arquivo/linha, referência analisada, severidade e estado. Separar resultado antigo de ocorrência reproduzida na versão atual.
3. Abrir Secret scanning e registrar tipo, localização e estado, sem o valor detectado. Confirmar revogação/rotação com os responsáveis; remoção do arquivo não equivale a revogação.
4. Para cada achado, registrar confirmação ou falso positivo com justificativa específica, ação, responsável e evidência de reteste. Não descartar alertas apenas para limpar o painel.
5. Identificar alertas cuja consulta exige permissões ainda indisponíveis como “não verificado”, sem declarar contagem zero.

## Próximas ações

- Preservar os resumos dos painéis já conferidos e confirmar o estado final do alerta genérico #4 após seu encerramento.
- Manter a #794 como referência da rotação, diferenciando a verificação direta local da confirmação da outra responsável.
- Vincular entregas recentes às tarefas corretas e registrar revisão técnica nas próximas PRs.
- Preservar os artifacts e links da versão escolhida para auditoria.
- Combinar acessos, ambiente de teste e entrevista com os auditores.
- Preencher o [plano e matriz de alinhamento](plano-e-matriz-de-alinhamento.md) antes de iniciar a análise do outro projeto.

Next.js 16 e TypeScript 7 permanecem acompanhados em #825 e #826. Essas issues abertas não são, por si só, achados de vulnerabilidade; a prioridade depende de suporte, alertas e compatibilidade verificados.

O inventário detalhado, os digests e os limites das evidências estão no [registro de evidências](registro-de-evidencias.md) e na [coleta pública sanitizada](evidencias-publicas-2026-11-15.json).
