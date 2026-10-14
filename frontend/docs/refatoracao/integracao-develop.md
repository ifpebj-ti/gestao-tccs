# Integração com develop — 14/10/2026

Merge iniciado pela usuária na branch `feat/interface-conta-e-fluxos-tcc`: trabalho local no commit `61cd357`, develop em `d9da8ad`. Os cinco conflitos foram resolvidos no working tree, sem concluir o commit de merge ou publicar a branch pelo assistente.

- Submissão mantém transação, convites por e-mail e espera pelo consentimento de todos os integrantes; incorpora o semestre ativo obrigatório e a confirmação da FK no banco vindos da develop.
- Gateway mantém apenas uma implementação de cada método, com filtro por semestre, carregamento do semestre e das participações nas consultas necessárias.
- Listas de TCCs em andamento e concluídos mantêm os filtros da develop usando o componente visual compartilhado. Selecionar Todos remove `SemesterId`; seleção envia ID numérico. A restrição por usuário aplicável continua na consulta de andamento.
- Detalhes exibem semestre com valor alternativo quando ausente, preservando edição e apresentação da interface local.
- `TccFilterDTO` recebe `SemesterId` opcional para compatibilidade com consumidores existentes. Fixtures dos convites foram atualizadas para usar semestre ativo, incluindo o PostgreSQL real dos testes.
- Consulta de semestres permite os perfis que acessam as listas de TCC (gestão, orientação, biblioteca e banca). Endpoints de escrita mantêm as restrições anteriores, comprovadas por testes HTTP.

Validação: 292 testes backend aprovados, incluindo os 11 testes com PostgreSQL temporário isolado; 83 testes frontend aprovados; TypeScript e ESLint aprovados. O novo cenário integrado comprova semestre ativo, status de consentimento, filtro por semestre, impedimento de proposta duplicada e erro 422 sem semestre ativo. Testes não utilizam o banco de desenvolvimento.

O script `scripts/preview-merge-semesters.cjs` verifica as duas listas, filtro por semestre, retorno a Todos e seleção no celular, usando exclusivamente APIs interceptadas com dados fictícios.

Durante a integração, a usuária perguntou se a consulta de semestres afetaria o cadastro discente. Conferido: o autocadastro consulta `/api/Campi/public/all`, com acesso anônimo preservado; essa rota e `/api/Campi/public/courses` responderam HTTP 200 no localhost. Semestre é atribuído na submissão da proposta a partir do semestre ativo, sem escolha no cadastro da conta.

Builds de frontend e backend aprovados e serviços reiniciados no Docker. Imagens finais: frontend `86c13312a69a`, backend `b6d374316de3`. As seis verificações do navegador passaram: filtrar, limpar e usar no celular em cada uma das duas listas. Evidências em `evidencias/merge-develop-semestres`. O banco temporário de testes foi encerrado. O índice do Git permanece aguardando `git add` da usuária para registrar a resolução dos conflitos; nenhum commit de merge ou push foi executado pelo assistente.

Refinamento posterior: filtro de semestre movido para baixo do título, alinhado à esquerda, nas listas de andamento e concluídos. Dois testes do seletor, TypeScript, ESLint e build aprovados. Oito verificações de navegador passaram, incluindo alinhamento e proximidade, seleção/limpeza e uso no celular em ambas as páginas. Evidências em `evidencias/filtro-semestre-posicao`. Frontend reconstruído e reiniciado na imagem `33819e55b63f`; o ajuste permanece local para novo commit/push da usuária.
