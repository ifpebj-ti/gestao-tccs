# Modelo de Relatório de Auditoria Integrada de Software

Status: modelo não preenchido. Nenhum resultado sobre outro time foi presumido.

Time auditor e integrantes: preencher.

Time auditado e integrantes: preencher.

Período, repositório, Projects, ambiente e SHA/tag: preencher.

## 1. Sumário executivo

Descrever em até dois parágrafos os pontos fortes observados, riscos prioritários e limites de acesso. Separar fatos confirmados, informações relatadas e verificações não realizadas.

## 2. Processo de desenvolvimento

### 2.1 Scrum, backlog e rastreabilidade

Registrar metas de sprint, critérios de aceite, Definition of Done, tarefas paradas e caminho entre tarefa, commit, PR e release. Informar período, amostra, método e resultado do índice de rastreabilidade.

### 2.2 Branching, revisão e documentação

Comparar política e prática. Registrar revisores e conteúdo técnico das análises, regras de merge, ADRs e compatibilidade dos diagramas com o código. A ausência de comentário escrito limita a evidência da revisão, mas não prova, isoladamente, que ela não ocorreu.

### 2.3 Testes e bloqueios

Registrar cenários examinados, resultados aprovados/falhos/ignorados, cobertura por camada, exclusões e evidência de bloqueio. Justificar qualquer classificação 0–5 com as condições efetivamente demonstradas.

## 3. Segurança técnica

### 3.1 SAST e SCA

Informar ferramentas, versões, regras, comando/configuração, SHA, relatórios e triagem. Uma dependência desatualizada não deve ser classificada como vulnerabilidade sem evidência do risco aplicável.

### 3.2 DAST e infraestrutura

Informar alvo autorizado, ambiente, contas fictícias, escopo autenticado ou não autenticado, ações realizadas, resultados e partes não cobertas. Não confundir ambiente temporário com produção.

### 3.3 Segredos

Registrar localização, tipo, impacto e situação de rotação/revogação, sem publicar os valores. Distinguir segredo real, dado sintético e falso positivo confirmado.

## 4. Matriz de descobertas

| ID | Tipo | Descrição e condição de reprodução | Severidade e justificativa | Impacto | Evidência e SHA | Estado |
| --- | --- | --- | --- | --- | --- | --- |

Preencher somente após análise. Estados sugeridos: confirmado, em investigação, falso positivo justificado, corrigido com reteste. Acesso indisponível deve ser registrado como limitação, não como ausência de vulnerabilidade.

## 5. Plano de Ação Corretiva

| Achado | Ação recomendada | Responsável acordado | Prioridade | Critério de aceite | Issue/canal privado | Evidência do reteste |
| --- | --- | --- | --- | --- | --- | --- |

Não atribuir responsáveis ou prazos sem acordo. Achados sensíveis devem usar canal privado. Correções de código e melhorias de processo precisam de critérios verificáveis.

## 6. Limitações e riscos residuais

Relacionar acessos faltantes, ambientes indisponíveis, testes não executados e riscos que permanecem após as ações. Não declarar certificação ou segurança total.

## 7. Encerramento e apresentação

Registrar data de compartilhamento, participantes, formato combinado com o professor, concordâncias e divergências fundamentadas. Preparar a apresentação com evidências e propostas viáveis; não realizar entrega ou contato em nome da equipe sem autorização.
