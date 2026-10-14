# E-mail, notificações e seletores — 13/10/2026

`resultado.json` registra as 17 verificações aprovadas em Chrome isolado contra o frontend compilado em Linux. Os dados e tokens são fictícios e toda chamada de API é interceptada.

`testes.json` reúne os resultados das suítes e do runtime nativo .NET 8. Os relatórios TRX detalhados estão no diretório temporário `gradus-proposal-test-results` do host.

- `seletor-padrao-verde.png`: opção marcada com o verde exato do card.
- `proposta-grupo-desktop.png`: margem corrigida e integrantes adicionados pelo e-mail.
- `convite-desktop.png` e `convite-mobile.png`: projeto, solicitante, resumo e consentimento.
- `notificacao-recusa-mobile.png`: orientação para revisar/submeter novamente e marcação de leitura.
- `seletores-autocadastro-mobile.png`: campus e curso dependentes no autocadastro.
- `seletores-gestao-desktop.png`: opções de perfil, campus e curso no padrão compartilhado.

O backend foi testado separadamente em PostgreSQL descartável, incluindo o cadastro posterior, preservação dos convites e envio ao professor após todos aceitarem. Consulte o [registro atualizado](../../../proposta-em-grupo.md).
