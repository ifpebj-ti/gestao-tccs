# Home, propostas recebidas e assinaturas — 14/10/2026

As homes dos perfis de orientação mostram apenas notificações com pendências positivas. Acompanhamento saiu da home desses perfis, pois as listas permanecem no menu lateral. Os estudantes continuam com a mesma regra de notificações; convites e recusas de participação usam o card verde compartilhado.

Em propostas recebidas, o contêiner passou a aproveitar o espaçamento do layout principal, sem margens internas duplicadas. Os cards ficaram mais compactos, com faixa lateral verde, resumo destacado e ações na base. O selo repetido de avaliação foi removido; a contagem e o contexto permanecem no cabeçalho.

`Button` centraliza o estilo das variantes `destructive` e `cancel`: vermelho 600 e texto branco, hover vermelho 700 e pressionado vermelho 800. Botões de cancelar, recusar, excluir e remover foram migrados, inclusive ações de confirmação e ícones de exclusão. `AlertDialogCancel` usa a variante compartilhada; `AlertDialogAction` recebe a variante explicitamente nas ações destrutivas.

A tela compartilhada de assinatura tem título curto, nome do documento abaixo e um único visualizador responsivo. As etapas usam botões do sistema e o aviso usa `NoticeCard`, com fundo `#183E2D`. O link gov.br é um link estilizado, sem botão aninhado. Agendamento e envio do arquivo do TCC no Anexo VIII foram preservados. Os campos de agendamento têm rótulos associados.

O fluxo só libera o envio após o download bem-sucedido. O painel deixou de ser montado como um componente interno a cada alteração de estado, preservando foco e edição durante o agendamento. A visualização deixou de duplicar iframes entre layouts de celular e desktop.

Validações: 81 testes frontend aprovados, TypeScript sem emissão, ESLint e build de produção. Os testes de renderização cobrem os sete perfis de assinatura, título, aviso, visualizador único, bloqueio de envio antes do download e campos/arquivos extras do Anexo VIII. Os testes de home cobrem contagens positivas e ausência de notificações vazias.

O script `scripts/preview-visual-adjustments.cjs` usa Chrome e intercepta todas as APIs com dados fictícios. Evidências em `evidencias/ajustes-visuais-professor`. Nenhuma proposta ou assinatura do banco de desenvolvimento é alterada pelos testes visuais.

As 11 verificações do navegador passaram: homes com/sem pendências, estudante sem notificações vazias, vermelho nativo e hover dos botões, espaçamento e cards de propostas, assinatura compartilhada, falha de download, falha de envio com arquivo preservado, envio bem-sucedido, foco/agendamento e dois arquivos no Anexo VIII, adaptação ao celular. [Resultado](evidencias/ajustes-visuais-professor/resultado.json), [propostas](evidencias/ajustes-visuais-professor/propostas-desktop.png), [assinatura desktop](evidencias/ajustes-visuais-professor/assinatura-desktop.png) e [ações no celular](evidencias/ajustes-visuais-professor/assinatura-mobile-acoes.png).

Docker atualizado em 14/10/2026: frontend reconstruído na imagem `640aee23dd40` e reiniciado; backend preservado em execução. O localhost:3000 respondeu HTTP 200. As 11 verificações do navegador também passaram após o reinício da imagem final.

## Refinamento do painel de assinatura

A pedido da usuária, o painel lateral inteiro passou a usar `#183E2D`, com textos claros, separadores suaves e botões principais em verde-lima. O aviso interno usa um verde mais profundo e os campos do Anexo VIII mantêm contraste no fundo escuro. `CustomFileInput` aceita a variante do botão como propriedade; as demais telas mantêm sua aparência anterior.

O link de retorno agora divide a linha com o título no desktop e o espaçamento abaixo do cabeçalho foi reduzido. Em telas estreitas, o cabeçalho quebra a linha naturalmente. A validação de renderização cobre o painel escuro nos sete perfis e os campos de agendamento; o teste do navegador mede a posição do título e do documento e confere as cores calculadas do painel e dos textos. Novas evidências em `evidencias/assinatura-painel-verde`.

Validação do refinamento: 81 testes aprovados, TypeScript, ESLint e build sem erros; as 11 verificações do Chrome passaram na imagem `95065e08747c`, já reiniciada no localhost. O título ficou antes de 105 px e o documento antes de 190 px do topo no viewport desktop de teste. Capturas conferidas em desktop e celular, com APIs fictícias.
