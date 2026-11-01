# Revisão visual do perfil de estudante — 13/10/2026

Pedido de Alexia após criar a primeira issue da lista: reduzir repetição, usar lateral clara com destaque verde escuro, concentrar a home em pendências e ocultar a troca de senha até a interação.

## Decisões e alterações

- Sidebar com o mesmo fundo creme da página; apenas a opção ativa recebe verde escuro. Mantida a seleção imediata e o funcionamento do histórico do navegador.
- Removidos título decorativo da lateral, avatar/link de conta do rodapé e saída duplicada. Minha conta permanece uma única entrada da lateral; saída e identidade ficam no menu superior direito.
- Removido o nome da página no cabeçalho e o atalho duplicado de conta no dropdown. Mantido o aviso de senha padrão, com acesso à troca interna.
- Home do estudante renderiza somente os cartões de atenção com contagem positiva. Sem pendências, aparece um estado simples; durante consulta, aparece carregamento. Os banners e fluxos de proposta, funções de consulta, callbacks e permissões existentes foram preservados.
- Outros perfis mantêm os grupos anteriores da home. A organização por perfil será revisada em etapas futuras.
- Breadcrumbs retirados das 12 telas internas que já têm a navegação lateral, incluindo os componentes compartilhados por Meu TCC. Abas, etapas, documentos e ações do trabalho foram preservados.
- Minha conta perdeu o subtítulo decorativo e a repetição de nome/matrícula na identificação do estudante. Esses dados continuam nos campos editáveis. E-mail, perfis, campus e curso continuam consultáveis.
- `AccountSecurityCard` abre a senha em diálogo, com indicação de botão, animação rápida e respeito a movimento reduzido. O formulário fica desmontado quando fechado, reduzindo rolagem e descartando senhas digitadas ao fechar.
- Acesso pelo menu/aviso usa `/profile?section=password`; o diálogo abre na própria conta. Ao fechar, o parâmetro é removido sem rolar a página. Foco e Escape seguem o componente Dialog existente.

## Arquivos desta revisão

- Shell: `Sidebar/index.tsx`, `Header/index.tsx`, `UserProfile/index.tsx`, `(auth)/layout.tsx`.
- Home: `(auth)/homePage/page.tsx`, `home/HomeShortcuts.tsx`.
- Conta: `(auth)/profile/page.tsx`, `profile/AccountSecurityCard.tsx`, `profile/ChangePasswordForm.tsx`, `layout/PageIntro.tsx`.
- Breadcrumbs: instituições, usuários/lista/detalhes, semestres, novo usuário, pendências/lista/assinatura, TCCs em andamento/lista/detalhes/assinaturas e concluídos/lista/detalhes.
- Dialog: tradução do texto acessível de fechamento para português.
- Testes: `home-shortcuts.test.cjs` e `scripts/preview-shell.cjs`.

Não houve alteração de backend, endpoints, validações ou regras de acesso nesta revisão. A mudança de exibição dos cartões é intencional e não remove os destinos da lateral.

## Validação

57 testes frontend aprovados, incluindo os novos cenários de home somente com pendências e diferença entre carregamento/estado vazio. Lint e TypeScript aprovados.

Rebuild Docker aprovado (Next.js 15.5.27, 28 rotas); frontend `a3e7492578b1` iniciado na porta 3000. HTTP 200 confirmado. Backend não foi rebuildado nesta etapa.

Chrome isolado: 24 verificações aprovadas com dados fictícios e API interceptada. Foram exercitados card por teclado, foco no primeiro campo, Escape/retorno do foco, acesso pelo menu, descarte das senhas ao fechar, home com e sem pendências e Meu TCC em desktop/mobile. Fluxos de validação, falha/retry e troca de senha anteriores também passaram.

Imagens de conta, diálogo de senha, home e Meu TCC foram inspecionadas. Desktop 1440 px e celular 390 px sem overflow da página nos cenários exercitados. Resultados em `evidencias/design-aluna/preview-checks.json`; capturas anteriores preservadas. Capturas com `falha-preview` são diagnósticos anteriores do script, não a execução final aprovada.

O simulador de teclado precisou enviar o caractere Enter e abrir o dropdown pela interação nativa; esses ajustes ficaram apenas no script de teste. Não houve alteração da API nem uso de contas reais.

Revisão visual da usuária/equipe permanece necessária. A execução com API fictícia não substitui integração completa com banco/serviços, cobertura medida ou as pendências de dependências já registradas.

Esta etapa deve apoiar a primeira issue já criada pela usuária. O número e os vínculos remotos não foram consultados. Nenhum commit, branch, issue ou PR foi criado pelo assistente.
