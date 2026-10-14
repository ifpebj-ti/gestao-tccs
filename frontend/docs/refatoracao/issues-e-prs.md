# Rascunhos para issues, Projects e PRs

Os rascunhos atualizados de conta própria, navegação, validação, senha interna e dependências estão em [Issues para Projects](issues-para-projects.md). Abaixo permanece o planejamento da primeira etapa visual.

Preparados localmente em 13/10/2026. Não existem IDs ou URLs de issues/PRs desta sessão. Alexia e a equipe publicarão os itens depois de revisar os arquivos. Não retroagir datas nem apresentar os rascunhos como evidência já publicada.

## Sequência sugerida de issues

| Item | Título | Escopo | Dependência |
| --- | --- | --- | --- |
| FE-01 | Consolidar o padrão visual e componentes da área interna | Tokens, títulos, estados, campos, botões, abas e variantes opcionais | Aprovação do padrão já usado na autenticação |
| FE-02 | Organizar a home por perfil e prioridade | Lista única, grupos, contagens e apoio textual | FE-01 |
| FE-03 | Padronizar gestão e acompanhamento de TCCs | Usuários, instituições/cursos, semestres, propostas, listas e detalhes | FE-01 |
| FE-04 | Padronizar assinaturas e avaliação de banca | Pendências, documentos, etapas e campos de nota reutilizáveis | FE-01 |
| FE-05 | Validar regressão, acessibilidade e entrega do frontend | Ambiente de build, testes, cobertura, navegador e documentação | FE-02, FE-03, FE-04 |

Responsável frontend sugerida: Alexia, conforme seu papel declarado. Labels sugeridas: frontend, refatoração, acessibilidade, testes; usar os nomes reais adotados no repositório. Prioridade e prazo devem ser definidos no planejamento da equipe. Estado inicial: implementação local disponível para revisão; validação ainda em andamento. Não marcar Done.

## Modelo de issue — home

**Título:** Organizar a home por perfil e prioridade sem alterar os fluxos existentes.

**Descrição:** Como usuário do Gradus Flow, quero encontrar primeiro minhas pendências e depois as ferramentas de acompanhamento e gestão disponíveis para meu perfil, com a mesma identidade visual da autenticação.

**Objetivo:** Reduzir repetição no código e facilitar a identificação das ações disponíveis.

**Critérios de aceite:**

- [ ] Grupos atenção, acompanhamento e gestão aparecem apenas quando contêm atalhos visíveis.
- [ ] Cada atalho mantém as permissões, contagem e destino anteriores.
- [ ] O apoio textual corresponde ao perfil; usuários com múltiplos perfis têm permissões preservadas.
- [ ] O carregamento não é apresentado como ausência de pendências.
- [ ] Há uma única configuração de atalhos para mobile e desktop.
- [ ] Banners, modais, estados e handlers do discente permanecem funcionais.
- [ ] Lint, tipos, testes, build e roteiro manual aprovado têm evidências anexadas.

**Dependências:** Componentes visuais FE-01; ambiente local funcional; revisão da equipe.

**Evidências a preencher:** ID/URL da issue, PR, execução de pipeline, resultados de testes, captura sanitizada e demonstração.

## Critérios adicionais por item

**FE-01:** títulos e controles seguem o padrão aprovado; variantes padrão continuam disponíveis; foco por teclado visível; alterações nos componentes compartilhados validadas também na autenticação.

**FE-03:** busca, paginação, cadastro, edição e confirmação preservados; a lista de usuários e de TCCs não duplica o mapeamento dos registros; ações mobile/desktop continuam com o mesmo comportamento; formulários não cortam campos em telas pequenas.

**FE-04:** filtros de pendência e ordem de assinaturas preservados; visualização de PDF e callbacks funcionam; campos de notas têm rótulos associados; limites, totais, parecer e submissão mantêm o resultado anterior.

**FE-05:** build real aprovado (atendido dentro do Docker); execução direta no Windows corrigida se necessária; testes de integração e cobertura planejados com QA; matriz por perfil executada; manual de uso atualizado; demonstração validada. Estes critérios ainda não foram todos atendidos.

## Sugestão de divisão das PRs

1. Base visual, componentes e variantes opcionais.
2. Home organizada e testes de permissões/renderização.
3. Gestão, listas e detalhes de TCC.
4. Assinaturas, avaliação e testes de campos.
5. Evidências, manual de uso e fechamento das pendências de validação.

Essa sequência representa dependências lógicas, não branches criadas. Os testes da home exigem a implementação da lista e dos componentes. O arquivo `package.json` recebeu o comando que executa ambos os arquivos de teste; ao dividir PRs, adaptar o comando aos testes presentes em cada etapa. Os testes comuns precisam de `tests/load-source.cjs`. Revisar também a divisão dos tokens globais, pois afetam todas as telas.

O working tree já continha mudanças de outras entregas. A separação por commits/branches será feita pela usuária, em ambiente autorizado, depois da revisão. Esta sessão não usa staging, stash, checkout, commits ou push.

## Descrição sugerida da PR — home

> A home repetia os mesmos atalhos em mobile e desktop e apresentava a todos os perfis um texto voltado ao discente. A mudança reúne os atalhos numa lista, organiza pendências, acompanhamento e gestão, aplica o visual plano aprovado e apresenta apoio textual por perfil, mantendo permissões, rotas, handlers e blocos de propostas.
>
> Validação local: lint, TypeScript, testes de permissões/renderização e build Docker aprovados. Build direto no Windows bloqueado pelo carregamento nativo do Lightning CSS; demonstração no navegador e integração pendentes. Atualizar esta descrição com os resultados reais antes da submissão.

## Texto sugerido para changelog futuro

> Padronizada a apresentação da área interna do Gradus Flow com componentes reutilizáveis, atalhos da home por prioridade/perfil, listas responsivas e campos de avaliação acessíveis. Mantidos os fluxos de negócio existentes. Acrescentados testes locais de permissões e renderização.

Publicar esse texto apenas depois da revisão e validação da entrega; definir a versão segundo a convenção da equipe. Não foi alterado o `CHANGELOG.md` principal nem criada release nesta sessão.
