# Navegação compacta — 13/10/2026

Pedido: aproveitar o espaço do cabeçalho, recolher a lateral, usar somente o símbolo do IFPE nas telas internas e deixar as pendências claramente clicáveis.

- Marca no topo da lateral, usando a imagem existente com recorte visual por CSS. A imagem original e as telas de autenticação foram preservadas.
- Lateral de 240 px expandida e 80 px recolhida no computador. Os ícones mantêm nomes acessíveis, indicação da página atual e título ao passar o mouse. A escolha dura enquanto o layout permanece montado.
- No celular, menu fechado por padrão em diálogo com foco controlado, fechamento por Escape e fechamento ao selecionar uma página.
- Cabeçalho compacto, sem divisória horizontal; conteúdo com menos espaço acima. Menu da conta preservado à direita.
- Variante plana de CardHome com cursor de clique, fundo verde claro no hover/foco, título sublinhado no hover e seta com movimento reduzido respeitado.
- FeatureActionCard extraído da ação de senha. Mantém botão nativo e encaminha propriedades e referência ao DialogTrigger. Seu uso continua pontual na conta.

Arquivos: layout autenticado; Header; Sidebar; AppBrand; CardHome; AccountSecurityCard; novo FeatureActionCard; script de preview e esta documentação. Endpoints, permissões, dados dos formulários e regras de TCC não foram alterados nesta revisão.

Validação: 57 testes automatizados do frontend, lint e TypeScript passaram. O build de produção no Docker compilou e gerou as 28 páginas. O preview executou 26 verificações com identidade fictícia e API interceptada, incluindo recolhimento, cursor, hover, foco, layouts mobile, edição da conta e senha. Evidências em `evidencias/menu-compacto`; as evidências anteriores foram mantidas. Esses resultados não substituem o teste integrado com dados reais nem comprovam cobertura de 90%.

Tudo permanece local e sem commit. Esta revisão complementa a primeira issue de refatoração visual criada pela equipe. Prazo do projeto: concluir até 30/11/2026.
