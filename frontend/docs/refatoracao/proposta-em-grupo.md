# Proposta em grupo — avaliação em 13/10/2026

## Estado atual após a correção de escopo

A usuária esclareceu que os integrantes são adicionados pelo e-mail e podem se cadastrar antes ou depois da submissão. Esse esclarecimento substitui a restrição inicial a contas já cadastradas. O formulário permite até três outros integrantes com “Adicionar outro estudante” e remoção individual. O curso dos integrantes deve coincidir com o do solicitante; para contas futuras, isso é validado no aceite após o cadastro e confirmação do e-mail.

O convite persiste pelo e-mail normalizado, com `UserId` opcional até o destinatário responder. A pessoa segue o primeiro acesso já existente, confirma o e-mail e se cadastra com esse endereço. O cadastro não registra consentimento nem cria automaticamente vínculo ao TCC. Ao entrar, a conta ativa encontra seus convites por e-mail na home e pode aceitar ou recusar. No aceite, vínculo, conta, perfil, curso/campus, limite e conflitos de TCC são revalidados dentro da transação.

Propostas com convites usam o estado `AWAITING_PARTICIPANTS`. Permanecem fora da consulta `PENDING_APPROVAL` das pendências do professor e só avançam a esse estado após o último aceite. A proposta individual continua sendo enviada diretamente. Uma recusa cancela a composição proposta e produz uma notificação persistente na home do solicitante, com ação para submeter novamente e opção de marcar como lida. Também há aviso por e-mail, sem tornar a entrega SMTP uma condição para a notificação no sistema.

A migração incremental `20261013204914_AllowEmailProposalInvitations` acrescenta e-mail, leitura da notificação e permite destinatário ainda sem conta. Ela preenche o e-mail dos convites existentes e reposiciona os que aguardam consentimento no estado correto, preservando seus registros. Um downgrade é recusado enquanto existirem convites sem conta, pois o schema anterior não consegue representá-los; os testes comprovam que essa recusa preserva dados e histórico de migrações.

Rotas adicionais: `GET /api/Tcc/participant-invitations/notifications` e `PATCH /api/Tcc/participant-invitations/{id}/notification`. A identidade vem do JWT; somente o solicitante pode marcar sua notificação como lida. Convites por e-mail exigem que o endereço da conta ativa corresponda ao convite. Não há listagem pública de estudantes ou busca de pessoas para essa função.

## Seletores e revisão do formulário

Foi corrigida a composição de classes de `PageIntro`, permitindo que `mb-0` realmente remova a margem extra abaixo da descrição. Os integrantes usam labels e campos repetíveis no padrão do formulário, com ícone de e-mail e botão de remoção acessível. O botão final diz “Enviar convites aos integrantes” quando há convidados.

Os 11 seletores nativos de autocadastro, cadastro/edição pela gestão, curso do TCC e turno da própria conta, além do seletor de orientador, agora usam `FormSelect`/`PatternSelect`. A opção marcada recebe exatamente o verde do card, `#183E2D`, com texto claro. O componente usa o menu acessível já disponível no projeto, portal, limite de altura, navegação por teclado, fechamento por Escape e retorno de foco. Não foi instalada dependência nova.

`Controller` mantém as seleções sincronizadas com valores iniciais, `reset`, campos dependentes e validação dos formulários. A edição de perfil mantém o valor como array e a edição de turno mantém a conversão anterior. Checkbox e itens de escolha dos menus também passaram ao verde do padrão. Todos os seletores nativos encontrados em `src` foram substituídos.

## Validação da versão atual

- 283 testes do backend, incluindo dez com PostgreSQL real descartável, aprovados. Casos novos cobrem cadastro e confirmação posteriores ao convite, ausência de vínculo automático, envio ao professor somente após todos os aceites, notificação persistente, leitura restrita ao solicitante, backfill dos convites anteriores e downgrade sem perda de dados.
- 72 testes do frontend, TypeScript e lint aprovados. A renderização do novo card de recusa é testada, incluindo o link para novo envio.
- Build de produção do frontend aprovado no Docker.
- Runtime nativo .NET 8: 273 testes aprovados, com os dez de PostgreSQL ignorados no container sem rede porque foram aprovados na execução separada com banco temporário.
- 17 verificações de navegador aprovadas: espaçamento, e-mail inválido, duplicação, limite/remoção de integrantes, payload, cor exata do seletor, Escape/foco, aceite/recusa, conflitos, recuperação, notificação e seletores nos formulários de estudante e gestão.
- Evidências novas em [e-mail e seletores](evidencias/proposta-em-grupo/email-e-seletores/README.md); as evidências anteriores foram preservadas.

Os testes de integração usam somente o banco temporário `gradus_proposal_test` em `127.0.0.1:55439`, com schemas separados por caso. No navegador, todas as chamadas de API usam respostas fictícias. O teste encontrou e corrigiu uma falha de renderização na composição do link do card de recusa antes da entrega. A entrega de e-mail em SMTP real, cobertura percentual e as pendências anteriores de dependências continuam sendo verificações separadas.

As seções abaixo registram a versão inicial e seu histórico; regras superadas pela explicação mais recente são substituídas pelo estado atual acima.

Ambiente local da versão atual: frontend e backend recompilados e recriados no Docker. Após o reinício, o frontend retornou 200 em `http://localhost:3000`; convites e notificações retornaram 401 sem autenticação em `http://localhost:8080`. Swagger publicou as cinco rotas. As migrações seguem o fluxo de inicialização existente da API. Containers temporários de teste foram encerrados; nenhuma conta real ou envio real de e-mail foi usado na conferência final.

Alexia sugeriu informar o e-mail de outro aluno já cadastrado ao enviar a proposta. A primeira análise foi somente local. Na continuação, autorizou a implementação e reforçou que os fluxos deveriam ser validados e testados. A implementação está descrita abaixo; a análise original foi preservada como histórico.

O cadastro de TCC pela gestão já trabalha com uma lista de estudantes (o formulário valida de um a quatro). CreateTccUseCase encontra contas por e-mail, vincula as existentes e cria convites de cadastro para as ausentes. SubmitTccProposalUseCase, usado pelo aluno, vincula somente o solicitante e o orientador. Os convites de cadastro não equivalem a aceitar participação em uma proposta.

Recomendação: campo opcional de e-mail do participante cadastrado, identidade do solicitante obtida do JWT e convite aceito/rejeitado dentro do sistema pelo próprio destinatário. Validar conta ativa e perfil de estudante, mesmo campus/curso, duplicação, auto convite, limite institucional de participantes e existência de outro TCC ou proposta ativa. Revalidar no aceite e gravar de forma atômica para impedir duas propostas concorrentes.

É preciso definir como a proposta aguarda os aceites antes da avaliação do orientador, como cancelar convites e o que fazer quando alguém recusa. O limite de quatro hoje encontrado no frontend deve ser confirmado pela equipe como regra de negócio. Entrada/saída de integrantes depois de documentos assinados exige um fluxo próprio que preserve o histórico.

Os vínculos UserTcc existentes e a pasta compartilhada podem ser reaproveitados: cada aluno vinculado acessa o mesmo projeto, sem copiar o TCC ou compartilhar senha. Os dados de todos os integrantes continuam na seção Informações.

Sugestão de issue: `feat(tcc): permitir convites para propostas em grupo`. Labels sugeridas: `frontend`, `backend`, `enhancement`, `security`, `tests`. A autorização inicial cobria arquivos/downloads; a continuação autorizou propostas em grupo, incluindo backend e testes. Nenhuma issue ou PR foi publicada.

## Implementação da continuação

O formulário discente oferece um e-mail opcional de outro integrante. Propostas individuais mantêm o envio direto ao orientador. O backend recebe `participantEmails` e admite até três convidados, mantendo o limite já existente de quatro integrantes. A interface desta etapa oferece um convidado.

O destinatário deve ter conta cadastrada e ativa, perfil de estudante e mesmo campus/curso do solicitante, sem proposta pendente ou TCC em andamento. E-mails são normalizados e consultados sem diferenciar maiúsculas/minúsculas, inclusive para contas antigas; duplicação e auto convite são rejeitados. O convite não cria conta nem vínculo ao TCC: somente o aceite autenticado inclui o destinatário no mesmo projeto. A listagem expõe apenas projeto, resumo e nome do solicitante, sem CPF, senha ou dados cadastrais do outro usuário.

Na home, o convidado pode aceitar ou recusar. Ambos os botões ficam bloqueados durante a requisição. Falhas preservam o convite e permitem nova tentativa. O solicitante acompanha os aceites. A avaliação do orientador fica bloqueada na interface e no backend enquanto houver convites pendentes. O último aceite notifica o orientador. Uma recusa cancela a proposta inteira e os demais convites pendentes, permitindo um novo envio com composição revisada. Cancelar também encerra os convites. Reformular uma proposta recusada exige novos aceites para o conteúdo revisado.

O ID de quem responde vem exclusivamente do JWT validado. Alterar IDs ou enviar `userId` no corpo não autoriza responder pelo destinatário. No aceite, conta, perfil, campus/curso, limite e TCC ativo são conferidos novamente. Respostas repetidas retornam 409; acesso a convite alheio retorna 404. Aprovação/recusa exige vínculo de orientador, coordenador ou supervisor, além da autorização da rota.

`ProposalGateway.Atomic` mantém validação e gravação na mesma transação PostgreSQL, com lock de transação compartilhado pelas submissões, cadastro pela gestão, aceites, aprovação, recusa, reformulação e cancelamento. Assim, duas propostas simultâneas do mesmo solicitante ou dois aceites do mesmo convidado não criam dois vínculos ativos. A solução serializa essas operações; caso o volume cresça, pode ser substituída por locks mais específicos, mantendo a atomicidade. Não é uma auditoria dos demais fluxos legados de vínculo.

E-mails de convite e do último aceite são enviados após o commit. Falha de SMTP não desfaz o vínculo nem oculta o convite dentro do sistema. A entrega é melhor esforço, com log de falha; não foi criado outbox, reenvio automático ou comprovante de entrega real. O template novo usa a URL configurada do frontend para levar à home.

## Persistência e rotas

A migração `20261013193257_AddProposalParticipants` adiciona a tabela `ProposalParticipants`, relacionamentos, datas de criação/resposta, estado do consentimento e índice único por TCC/destinatário. O snapshot anterior continha uma propriedade `TemplateContent` já ausente das entidades e do designer da última migração; o snapshot foi alinhado ao modelo, preservando qualquer coluna existente. A nova migração não remove coluna de `DocumentTypes` nem apaga dados anteriores.

- `POST /api/Tcc/proposal`: campo opcional `participantEmails`.
- `GET /api/Tcc/participant-invitations`: convites pendentes da conta autenticada.
- `PATCH /api/Tcc/participant-invitations/{id}`: `{ "accept": true }` ou `{ "accept": false }`, obrigatório e booleano.
- `GET /api/Tcc/participant-invitations/proposal/{id}`: estados dos convidados, restritos a estudantes vinculados.
- Detalhes de TCC incluem `pendingParticipantCount` para bloquear a avaliação na interface.

## Validação executada

- 67 testes do frontend aprovados, incluindo quatro novos testes de schema, apresentação, escape de texto e bloqueio de respostas.
- TypeScript e lint de `src` aprovados.
- Build de produção do frontend aprovado no Docker, com 31 entradas na geração de páginas.
- 275 testes do backend aprovados no host, incluindo 30 novos testes de regras, oito de endpoints HTTP e cinco de PostgreSQL real e isolado.
- PostgreSQL temporário: migração de ida/volta, consistência do modelo, concorrência de submissões e aceites, rollback, convite persistido, vínculo compartilhado e aprovação após consentimento.
- No runtime nativo .NET 8, 270 testes aprovados; os cinco de PostgreSQL foram ignorados intencionalmente nesse container sem rede, pois passaram no teste separado com banco temporário.
- Oito verificações no Chrome isolado com API interceptada: campo inválido, payload normalizado, apresentação do convite, conflito, bloqueio dos botões, recuperação da listagem, layout mobile, aceite e recusa.
- Evidências em [proposta-em-grupo](evidencias/proposta-em-grupo/README.md).

Os testes com PostgreSQL exigem `GRADUS_PROPOSAL_TEST_DATABASE` apontando para o banco descartável `gradus_proposal_test` em `127.0.0.1:55439`; recusam outra base/host/porta. Cada caso usa e remove apenas seu schema temporário. Execução no PowerShell: `dotnet test backend/gestaotcc.Test/gestaotcc.Test.csproj --no-restore`. No host .NET 10, foi usado `DOTNET_ROLL_FORWARD=Major`; as referências EF do projeto de testes foram alinhadas em 8.0.15 para testar os gateways reais sem conflito de assemblies.

O preview é reproduzível por `node frontend/scripts/preview-proposal-group.cjs 9230 http://127.0.0.1:3001`, com frontend compilado e Chrome headless isolado na porta de depuração indicada. Todas as chamadas da API são interceptadas, sem login real ou envio de e-mails. O dev server Windows permaneceu inviável por falta do módulo nativo Lightning CSS; a validação visual usou o build Linux. O build ainda informou as dez ocorrências altas de dependências já documentadas na etapa anterior; esta mudança não resolve nem declara encerrada essa pendência.

Os resultados não comprovam cobertura de 90%, ausência de vulnerabilidades ou entrega de SMTP real. A equipe ainda deve revisar o limite institucional, testar com contas fictícias dos demais perfis e validar a entrega de e-mails em seu ambiente. Tudo permanece local, sem commit, branch, PR ou publicação remota.

## Ambiente local atualizado

A usuária também pediu o rebuild dos containers para testar pelo localhost. Frontend e backend foram recompilados com `docker-compose -f docker-compose-development.yml build frontend backend` e recriados com `up -d --no-deps backend frontend`. A inicialização da API executa as migrações locais pelo fluxo existente.

Verificação após o reinício: `http://localhost:3000` respondeu 200; a nova rota `http://localhost:8080/api/Tcc/participant-invitations` respondeu 401 sem autenticação; Swagger respondeu 200 e publicou as três rotas de convite e o campo `participantEmails`. Nenhuma conta real foi usada, nenhum convite real foi criado e nenhum e-mail foi enviado nessa conferência. Os dois containers temporários de testes foram encerrados. O Mailpit local permanece disponível em `http://localhost:8025` para a conferência manual dos e-mails.
