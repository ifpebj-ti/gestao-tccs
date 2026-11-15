# Auditoria de segurança e processo — GradusFlow

Pacote de preparação elaborado em 15/11/2026 para a atividade de auditoria cruzada. O material organiza evidências do GradusFlow e modelos para auditar outra equipe. Não constitui certificação, conclusão independente ou declaração de ausência de vulnerabilidades.

## Documentos

| Documento | Uso |
| --- | --- |
| [Preparação e evidências](preparacao-e-evidencias.md) | Matriz dos critérios, situação do projeto e limitações. |
| [Registro de evidências](registro-de-evidencias.md) | Links, versão avaliada, imagens, artifacts, painéis e rotação. |
| [Coleta pública sanitizada](evidencias-publicas-2026-11-15.json) | Registro estruturado de proteções, jobs, artifacts e imagens consultados por leitura. |
| [Plano e matriz de alinhamento](plano-e-matriz-de-alinhamento.md) | Entregável de alinhamento; completar acessos, responsáveis, ambiente e entrevista. |
| [Modelo do relatório integrado](modelo-relatorio-integrado.md) | Estrutura do relatório sobre o outro projeto, a preencher após análise e entrevista reais. |

## Referência da avaliação

Documento fornecido pela equipe em 15/11/2026: “Auditoria de Processo e de Qualidade de Desenvolvimento de Software”, identificado como Atividade-auditoria-2026.pdf. O PDF não foi incorporado ao repositório; este pacote referencia seu conteúdo sem republicar o documento pessoal.

| Critério ou entregável do PDF | Localização no pacote | Situação |
| --- | --- | --- |
| Plano, matriz e acessos, páginas 2 e 7 | Plano e matriz de alinhamento | Estrutura preparada; dados do outro time e acessos pendentes. |
| SAST/SCA, DAST e segredos, páginas 1 e 7 | Preparação e registro de evidências | Controles e resultados do GradusFlow identificados; limites registrados. |
| SDLC, revisão, rastreabilidade e testes, páginas 2, 4 e 8 | Matriz e amostra de processo | Amostra pública consultada; sem extrapolação para todo o histórico. |
| ADRs e documentação, página 6 | Links para ADRs e documentos existentes | Wiki/README e plano de testes seguem em revisão pela responsável. |
| Relatório integrado e PAC, páginas 3, 9 e 10 | Modelo de relatório | Não preenchido sobre outro time sem acesso e análise. |
| Apresentação e encerramento, páginas 11 e 12 | Seção de encerramento do modelo | Formato e agenda devem ser acordados com o professor. |

As ferramentas citadas no PDF são recomendações. A existência de ferramentas equivalentes no projeto deve ser demonstrada por configuração, resultado e cobertura do controle; não é necessário instalar todas as alternativas apenas para aumentar a lista.

## O que está demonstrado e o que falta

As fontes públicas confirmam CI aprovado na main da release 2.14.0, publicação aprovada, imagens de frontend e backend nas duas arquiteturas e regras ativas nas branches principais. As capturas fornecidas pela equipe registram ausência de alertas abertos de Dependabot e de Code scanning na main, além da triagem de segredos descrita no registro.

Permanecem pendentes o arquivamento autenticado dos relatórios completos, a confirmação visual final do alerta privado de segredos, a revisão dos documentos a cargo da outra integrante, a conciliação de imagens históricas e a combinação de ambiente e acessos com os auditores. A OCI não foi implantada. O relatório independente sobre outra equipe depende de acesso e da execução da auditoria.

Não atribuir score máximo de qualidade com base apenas nos checks verdes. Registrar o período, a amostra e o alcance de cada evidência.
