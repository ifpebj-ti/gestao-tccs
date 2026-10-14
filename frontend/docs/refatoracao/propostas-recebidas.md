# Propostas recebidas pelo orientador

O professor acessa `/receivedProposals` pelo menu **Propostas recebidas** e pelo atalho de pendências do início. A lista em modal foi removida. Cada proposta apresenta título, estudantes, resumo e ações; a recusa abre um formulário junto ao item, com justificativa obrigatória de até 1000 caracteres.

Aprovação e recusa usam os contratos existentes da API: `PATCH /api/Tcc/{id}/approve-proposal` e `PATCH /api/Tcc/{id}/reject-proposal`. A chamada anterior usava POST e uma rota inexistente, causando 405. O cancelamento discente também passou a usar `PATCH /api/Tcc/{id}/cancel-proposal`.

O perfil ADVISOR não pode criar propostas de gestão, tanto pela interface e middleware quanto pela autorização da API. ADMIN, COORDINATOR e SUPERVISOR preservam o cadastro. Perfis de orientação e gestão continuam sujeitos ao vínculo de orientador para avaliar uma proposta. A API continua exigindo os aceites dos integrantes.

A página contém estados de carregamento, erro com nova tentativa e lista vazia. Uma falha de decisão preserva o item; o sucesso o remove da lista. As ações ficam bloqueadas durante a requisição para evitar decisões duplicadas. Erros da API em ProblemDetails são apresentados ao usuário.

Validação automatizada: 77 testes de frontend, TypeScript sem emissão e ESLint; 278 testes de backend aprovados, com 10 testes de PostgreSQL ignorados porque exigem um banco de testes dedicado. Os novos testes HTTP executam os controllers reais em Kestrel, verificando PATCH com ID na rota, transições de status, conflito na segunda decisão, bloqueio de outro orientador e bloqueio de criação pelo professor. Os testes frontend verificam os contratos de rede, carregamento dos resumos, erros e permissões de rota/menu.

O script `scripts/preview-received-proposals.cjs` valida a interface no Chrome com todas as chamadas de API interceptadas e dados fictícios. As evidências ficam em `evidencias/propostas-recebidas`; nenhuma proposta do banco de desenvolvimento é aprovada ou recusada por esse script.

As seis verificações do navegador passaram: página/menu, erro sem remoção do item, aceite, recusa com justificativa no celular, estado vazio e nova tentativa após falha. Capturas: [desktop](evidencias/propostas-recebidas/desktop.png), [recusa no celular](evidencias/propostas-recebidas/recusa-mobile.png) e [resultado](evidencias/propostas-recebidas/resultado.json).

Frontend e backend foram reconstruídos e reiniciados no Docker em 14/10/2026. Conferência local: frontend HTTP 200, Swagger HTTP 200 e contratos PATCH de aprovação/recusa presentes. Imagens: frontend `21d05fb6079a`, backend `5c629f67d208`.

## Campo de justificativa compacto — 14/10/2026

A justificativa da recusa usa `rows={1}` e remove a altura mínima apenas nesse campo. O componente compartilhado continua usando dimensionamento pelo conteúdo: o campo cresce conforme o texto e volta à altura compacta ao apagar. A borda inferior acompanha o conteúdo; o redimensionamento manual foi desabilitado nesse formulário. O limite de 1000 caracteres e a justificativa obrigatória foram preservados.

O script `scripts/preview-received-proposals.cjs` verifica a altura vazia, o crescimento com texto longo, a ausência de conteúdo cortado e o encolhimento ao apagar, junto às ações existentes. Evidências deste ajuste ficam em `evidencias/recusa-campo`.

Validação: quatro testes de propostas, TypeScript, ESLint e build aprovados; sete verificações do navegador passaram após ativar a imagem Docker `d131ddf6a3f1`, incluindo campo compacto e crescimento/encolhimento. Captura do campo vazio conferida visualmente. Usados apenas dados fictícios nas ações de aprovação e recusa.
