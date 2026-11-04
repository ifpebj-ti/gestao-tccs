# Publicação controlada de candidatos AMD64/ARM64

## Base e escopo

Preparação local a partir de `origin/main` em
`275542ab88e8e7b4f690d3034607d75c9ee6f3c0` (release `2.12.0`).
Histórico confirmado: feature `c38ba37`, merge #687 para develop (`974a407`),
merge #688 para main (`f4f7717`) e atualização do badge (`275542a`).
O diretório estava limpo. A branch local nova é
`feat/controlled-multiarch-publication`, sem upstream para evitar push acidental.

Nenhum push, merge, dispatch remoto, publicação ou deploy foi feito nesta preparação.
Nenhum recurso OCI foi criado. Frontend, backend, Composes, Dockerfiles e o servidor
existente não são alterados por estes arquivos locais.

## Release automática preservada; validação em workflow separado

`production.yml` mantém o gatilho de merge para main, geração de SemVer, GitHub Release,
badge, atualização de `latest` e webhooks existentes. **Não executar esse workflow para
validar candidatos.** Nenhuma dessas operações foi executada nesta preparação local.
Quando a mudança for futuramente mergeada com autorização, esse fluxo continuará
automático e poderá atualizar o servidor como já fazia.

O build automático usa Buildx e os Dockerfiles `.multiarch`, com QEMU na produção.
Cada serviço testa e escaneia ambas as arquiteturas, publicando apenas tags intermediárias
inéditas por SHA/run/tentativa. Somente o job conjunto, dependente dos dois serviços,
verifica as quatro origens, cria os índices de versão por digest, cria a GitHub Release
no SHA compilado e promove ambos os `latest`. Os webhooks são chamados depois dessas
etapas; falha HTTP do backend interrompe a chamada do frontend. Destinos e secrets
existentes são preservados. Dois webhooks assíncronos e dois registros `latest` não
formam uma transação: falha posterior ou atualização externa ainda requer intervenção.

`outros` não publica imagens, Release, badge ou webhooks, conforme o template existente.
Os incrementos de patch/minor/major e os gatilhos originais são mantidos. Uma preflight
recusa tags Git ou versões GHCR existentes e falha diante de erro de autenticação/rede.
Todos os checkouts e `target_commitish` usam `github.sha`. O badge é atualizado depois
da Release, sobre um snapshot atual do README e com o blob SHA como trava otimista;
não altera a origem das imagens nem substitui outras edições do README.
Somente a etapa administrativa do badge permite falha (`continue-on-error: true`):
um erro no README não bloqueia os webhooks após as imagens aprovadas. A atualização
automática continua sendo tentada; o resultado dessa etapa deve ser conferido no run.
A tag Git efetivamente criada também é consultada e resolvida até o commit (inclusive
tags anotadas) antes de promover `latest`; divergência de SHA ou erro de API interrompe o fluxo.

A configuração de API do servidor continua sendo recebida em runtime por
`NEXT_PUBLIC_API_URL` no Compose existente e por `next-runtime-env` no frontend.
Não foi criada exigência de variável nova no GitHub, nem alterados endereços,
portas ou secrets. O frontend usa Node 24 também na etapa de lint da produção.

O workflow **separado** `multiarch-publication.yml` é manual e usa exclusivamente tags
de candidatos. Não contém release, badge, `latest` ou webhooks. Sua cobertura é
registrada no resumo, sem escrever no repositório.

`ci.yml` fica intacto: Semgrep, Trivy FS, npm audit e ZAP existentes são preservados.
O ZAP desse CI tem um alvo de produção; esta preparação não o executou. Avaliar esse
comportamento antes de abrir um PR, que pode disparar o CI existente.

## Validação padrão e publicação opcional

Inputs manuais:

| Input | Padrão | Efeito |
| --- | --- | --- |
| `publish_candidates` | `false` | Constrói, testa e escaneia; não faz login ou push no GHCR |
| `confirm_sha` | vazio | Publicação exige o SHA completo idêntico ao commit do ref selecionado |
| `api_url` | `http://127.0.0.1:8080/api` | URL de teste; definir a URL correta para um candidato destinado a uso real |

Os inputs são passados por variáveis de ambiente, sem interpolar texto em comandos shell.
URLs com credenciais, protocolos não HTTP(S) ou quebras de linha são recusadas.
Não colocar secrets em `api_url`: ela vai para o frontend.

Um ref de branch pode mudar entre revisão e disparo: conferir novamente o SHA. A
comparação com `confirm_sha` faz o run falhar antes dos builds se o commit mudou.

Tags permitidas:

```text
candidate-sha-FULL_SHA-run-RUN_ID-attempt-ATTEMPT-amd64
candidate-sha-FULL_SHA-run-RUN_ID-attempt-ATTEMPT-arm64
candidate-sha-FULL_SHA-run-RUN_ID-attempt-ATTEMPT
```

São usados os pacotes existentes:
`ghcr.io/ifpebj-ti/gestaotccs-frontend` e `ghcr.io/ifpebj-ti/gestaotccs-backend`.
Não há tags `latest`, `2.12.0` nem outras versões nesse caminho de escrita.
Run e tentativa distinguem inclusive reexecuções do mesmo commit.

Antes de cada push, o script faz HEAD no manifest: somente HTTP 404 é aceito como
ausência. HTTP 200, falhas de autenticação, rate limit ou indisponibilidade interrompem
a etapa. Não existe fallback para sobrescrever uma tag.
GHCR não oferece aqui uma transação de criação exclusiva de tag; nomes únicos e
concurrency evitam colisões entre estes runs. Atores externos com permissão de escrita
no pacote continuam fora desse controle e devem ser considerados antes de publicar.

## Matriz e verificações

Quatro combinações: frontend/backend × AMD64/ARM64. Buildx usa
`ubuntu-24.04` e `ubuntu-24.04-arm`, com builds nativos, sem QEMU no workflow novo.
Os Dockerfiles usados são os `.multiarch` já existentes; não houve mudança da aplicação.

Cada combinação:

1. Backend executa os testes e OpenCover com PostgreSQL descartável em
   `127.0.0.1:55439`, banco `gradus_proposal_test`; frontend não inicia esse serviço.
2. Buildx constrói e carrega uma única plataforma localmente, com `push: false`.
3. `multiarch-smoke.sh` verifica arquitetura, dependências nativas e HTTP.
4. Trivy 0.76.0 verifica a imagem exata antes de qualquer push dessa combinação.
5. Se autorizado, publica somente a tag individual inédita e verifica a plataforma
   real e o label `org.opencontainers.image.revision` no registry.

Depois que **todas** as combinações passam, o job de manifests verifica ambos os
destinos e as quatro imagens de origem. Monta os índices com referências por digest,
sem reconstruir as imagens, e verifica as duas plataformas e o SHA em cada pacote.
O script confere também o hash dos manifests e configurações retornados pelo registry.
Os descritores do índice devem apontar para digests imutáveis e declarar o mesmo
SO/arquitetura das configurações reais; um índice com plataformas trocadas é recusado.
Attestations `unknown/unknown` não são contadas como arquiteturas executáveis.

Uma falha parcial pode deixar tags individuais candidatas no GHCR. Elas não são
promovidas nem implantadas. Falha no segundo índice também pode deixar apenas o
primeiro índice pronto: o run incompleto não é uma release. Não repetir pushes sobre
as tags parcialmente criadas; novo run/tentativa gera novos nomes. Nenhuma limpeza
automática apaga versões existentes.

## Custo e armazenamento

O conector confirmou o repositório como público. O planejamento falha se o evento
informar uma visibilidade diferente; não escolhe runners pagos como alternativa.
Segundo a documentação consultada, runners padrão em repositórios públicos são
gratuitos, incluindo os labels ARM64 padrão. Isso não foi confirmado pela execução
remota, que continua pendente.

- Máximo de duas combinações simultâneas.
- Timeout de 45 minutos por combinação; plan 5 minutos e manifests 10 minutos.
- Limite nominal somado: 195 minutos de jobs; não é previsão de duração nem orçamento monetário.
- Sem `cache-to` remoto e sem exportar arquivos OCI ou tarballs para artefatos.
- Upload automático dos registros Docker desabilitado.
- Relatórios ficam no runner e resumos nos logs do run.
- Uma publicação completa cria seis referências (quatro tags individuais e dois índices);
  índices reutilizam os blobs por digest, sem duplicar integralmente as camadas.

Pacotes públicos são gratuitos segundo a documentação de Packages; os dois pacotes
existentes puderam ser lidos anonimamente. Confirmar a visibilidade dos pacotes e o
Billing da organização antes do dispatch autorizado. Não foi possível auditar os
orçamentos, consumo acumulado, políticas ou cobranças da organização nesta preparação.
Não prometer custo zero para uma futura mudança de plano, visibilidade ou política.

O workflow antigo `multiarch-validation.yml` permanece disponível e independente:
ele tem cache GHA e exportação opcional de artefatos. Não executar os dois workflows
em duplicidade nem habilitar `export_oci` sem revisar consumo e necessidade.
Não usar a futura VM OCI para estes builds.

## Verificação das imagens já existentes

Consultas de leitura confirmaram `2.12.0` apenas em `linux/amd64`:

| Pacote | Digest do índice da release |
| --- | --- |
| backend | `sha256:b0cc7ac0bf4b490e32563d448c06ef11dc82f679d3c6d8ad07e22d1876fc5d53` |
| frontend | `sha256:6db48b98b754fe9d91795daaddb1bd46ac53aaaaac3a89d44874581a9f13cf21` |

O novo verificador também leu esses manifests públicos corretamente, ignorando
as attestations. Nenhuma dessas referências foi modificada.

## Como executar depois de revisão e autorização separadas

As alterações estão locais e sem commit. Primeiro revisar o diff. Não executar os
comandos de dispatch antes de autorizar commit/push e confirmar a ausência de
atualizadores externos que possam reagir a novas imagens. O workflow não acessa o
Portainer e não confere a configuração do servidor; watchers externos são desconhecidos.

Abaixo são exemplos de **comandos futuros**, não executados. `gh` não está instalado
no ambiente local inspecionado; também é possível usar a interface Actions do GitHub.
O workflow novo ainda não está na branch padrão. `workflow_dispatch` exige que o
arquivo esteja registrado nela; escolher `--ref` não deve ser tratado como bypass.
Antes disso, permanecem os testes locais. Procedimento proposto, não executado:
após autorização, revisar a correção de `outros` e registrar o workflow numa integração
técnica marcada `outros`, que o código corrigido faz terminar sem publicação ou webhooks;
então executar candidatos com publicação desabilitada e, em outra autorização, habilitá-la.
Não usar esse procedimento enquanto a correção de `outros` não estiver efetivamente
incluída na integração. Nenhum gatilho foi alterado para contornar essa exigência.
Marcar exclusivamente `- [x] outros`, mantendo as três opções de incremento desmarcadas.
O dispatch manual de `production.yml` não é esse procedimento: ele seleciona incremento
minor e deve continuar reservado a uma release autorizada. A integração ainda pode
executar o CI existente, incluindo o ZAP; não equivale a nenhuma execução remota nesta revisão.

PowerShell, depois do commit/push autorizado da branch:

```powershell
# Primeiro: apenas validar, sem publicar.
gh workflow run multiarch-publication.yml --repo ifpebj-ti/gestao-tccs --ref feat/controlled-multiarch-publication -f publish_candidates=false -f api_url=http://127.0.0.1:8080/api

# Somente com autorização adicional para publicação e após revisar o SHA.
$candidateCommit = git rev-parse HEAD
gh workflow run multiarch-publication.yml --repo ifpebj-ti/gestao-tccs --ref feat/controlled-multiarch-publication -f publish_candidates=true -f confirm_sha=$candidateCommit -f api_url=http://127.0.0.1:8080/api

# Consultar os runs sem iniciar novas execuções.
gh run list --repo ifpebj-ti/gestao-tccs --workflow multiarch-publication.yml --limit 5
```

O exemplo mantém URL fictícia: a publicação seria apenas um teste de distribuição,
não uma imagem configurada para deploy. Para um candidato utilizável, fornecer e
validar o endpoint público desejado antes do build.

Copiar a tag completa do resumo do run. Consultas de leitura:

```powershell
$candidateTag = 'COLE_A_TAG_COMPLETA_DO_RUN'
docker buildx imagetools inspect "ghcr.io/ifpebj-ti/gestaotccs-frontend:$candidateTag"
docker buildx imagetools inspect "ghcr.io/ifpebj-ti/gestaotccs-backend:$candidateTag"
node scripts/controlled-publication.cjs inspect ghcr.io/ifpebj-ti/gestaotccs-frontend $candidateTag
node scripts/controlled-publication.cjs inspect ghcr.io/ifpebj-ti/gestaotccs-backend $candidateTag
```

Exigir `linux/amd64` e `linux/arm64` em ambos, com revision igual ao SHA aprovado.
O resumo do run registra os digests verificados. Comparar também os digests de
`2.12.0` acima. Para pacotes privados, autenticar somente por mecanismos apropriados,
sem enviar tokens ao chat. Não atualizar `APP_VERSION` nem o servidor para testar.

## Evidências locais desta preparação

- `actionlint`: produção automática, candidatos e validação isolada aprovados.
- `shellcheck`: smoke aprovado no conteúdo versionado LF; o checkout Windows
  converte esse arquivo para CRLF. O arquivo existente não foi modificado.
- 35 testes Node: guardas, versionamento real em Bash, SHA, badge e promoção conjunta aprovados.
- 2 testes Python do relatório de cobertura: aprovados.
- Backend com PostgreSQL descartável e OpenCover: 346 aprovados, zero ignorados.
- Frontend AMD64 reconstruído: 96 testes, lint, build e TypeScript aprovados.
- Backend AMD64 reconstruído: publish aprovado; testes internos sem conexão externa
  mantêm 335 aprovados e 11 ignorados, cobertos pela execução completa separada.
- Quatro smokes individuais AMD64/ARM64: arquitetura, addons nativos e HTTP aprovados.
- Duas stacks integradas: AMD64 com LMDB e ARM64 com SQLite aprovadas; login,
  perfil, CORS, S3 e SMTP passaram. São os probes existentes, com os limites
  documentados em `multiarch-validation.md`.
- Trivy 0.76.0 nas quatro imagens: gate CRITICAL com `--ignore-unfixed` aprovado.
  Isso não significa ausência de vulnerabilidades de outras severidades ou sem correção.
- Recursos temporários removidos; os containers originais permaneceram ativos.

Imagens AMD64 desta sessão usam `gradus-candidate/*:275542a-working-amd64`.
As imagens ARM64 locais anteriores `gradus-validation/*:162cf2e-working-arm64`
são reutilizadas para regressão do runtime sem mudança do código da aplicação ou
Dockerfiles; não são uma reconstrução ARM64 desta branch. A execução nativa ARM64
dos jobs novos continua pendente, assim como login/push, criação de índices remotos,
permissões de pacotes e confirmação dos novos manifests publicados.

Os testes locais não equivalem à execução da orquestração GitHub Actions. O script
de testes usa registry simulado para as situações de erro e os dois manifests reais
existentes para a consulta de leitura. Não houve publicação para testar o caminho de escrita.

Antes de mergear: a release automática e os webhooks continuam ativos no fluxo de
produção, por solicitação do usuário. Não fazer merge para experimentar: isso pode
publicar uma versão e atualizar o ambiente. Os testes de candidatos são separados.
Os tipos e incrementos são os anteriores; `outros` agora respeita o contrato já descrito
no template de não gerar release, em vez de reutilizar uma versão publicada.

## Limites operacionais e recuperação manual

`promoteRelease()` já verifica os dois serviços antes de escrever e promove por digest.
Não existe transação entre os dois `latest`: se a segunda escrita falhar, a primeira
pode permanecer atualizada. O erro interrompe o run e impede seus webhooks, mas um
atualizador externo pode reagir à primeira alteração. Rollback automático não foi
implementado; acrescentá-lo poderia disputar com outros escritores ou também falhar.

HTTP de sucesso do Portainer confirma apenas aceitação da requisição. Não comprova
conclusão da atualização, saúde da aplicação ou sucesso de migrations. Se um serviço
falhar depois de aceitar o webhook, a pipeline atual não consegue detectar isso.
Essa limitação exige aceitação operacional antes de autorizar uma release.

Procedimento proposto, para execução humana futura e autorizada:

1. Antes da release, registrar a versão e os digests da última dupla aprovada e as
   imagens efetivamente executadas, usando a configuração e a interface atuais.
2. Se a promoção falhar, interromper o procedimento; comparar os dois `latest` com
   os digests registrados e conferir se algum contêiner foi atualizado por outro mecanismo.
   Não repetir a release para forçar uma versão existente nem sobrescrever tags SemVer.
3. Depois dos webhooks, conferir no Portainer os dois serviços: imagem esperada,
   estado, reinícios, logs e healthcheck, quando existir. Não assumir que há healthcheck
   configurado para todos os serviços.
4. Pelo acesso já existente, testar carregamento do frontend, login, consulta do
   próprio perfil e uma leitura de documento já disponível. Uma resposta em `/metrics`
   ou um contêiner em execução, isoladamente, não comprova esses fluxos.
5. Havendo falha, suspender novas atualizações e, com autorização, recuperar a dupla
   anterior pela operação já utilizada no Portainer, preservando configurações e volumes.
   Se os `latest` divergirem, sua correção também precisa ser deliberada e por digest.
   Verificar compatibilidade com o banco antes de voltar a versão; não reverter migrations
   nem restaurar banco automaticamente.
6. Repetir as verificações dos dois serviços e registrar o resultado. Não usar
   `down -v`, limpeza de volumes ou novos webhooks como tentativa genérica de recuperação.

Nenhum passo desse procedimento foi executado nesta revisão. Saúde automática e
rollback/orquestração ficam como melhorias futuras, sujeitas a autorização.

## Instalação do Trivy na produção

A produção ainda usa o instalador existente em `main`, sem indicar versão do binário;
o workflow de candidatos usa `v0.76.0`, já validada localmente. O instalador oficial
aceita uma tag como argumento final, portanto fixar o binário nessa versão é possível
sem mudar os URLs atuais. Essa fixação melhora a reprodutibilidade, mas foi classificada
como melhoria futura, não como falha comprovada do gate; não foi aplicada nesta revisão.
Fixar o binário não torna imutável o instalador em `main` nem congela a base de CVEs.

## Investigação local do badge

O comando Debug da produção foi reproduzido com OpenCover em container sem rede:
335 testes aprovados, 11 integrações ignoradas por falta de conexão de teste.
Resumo: 4.202/29.348 pontos de sequência = 14,31%; cobertura de ramos = 43,57%.
O `cut -d. -f1` existente explica a exibição inteira de 14% no badge.

| Assembly | Pontos visitados / total | Cobertura de sequência |
| --- | --- | --- |
| Domain | 777 / 1.162 | 66,87% |
| Application | 3.234 / 4.787 | 67,56% |
| Infra | 13 / 21.419 | 0,06% |
| WebApi | 178 / 1.980 | 8,99% |

Migrations e snapshot somam 20.112 pontos sem execução nessa coleta. O runsettings não
filtra esses arquivos. Não foram alterados filtros, escopo ou testes para elevar o badge.
Essa métrica inclui código gerado e depende dos testes habilitados; não comprova cobertura
dos fluxos críticos, integração real, segurança, navegador ou desempenho. O XML foi
analisado localmente; o artefato exato do run remoto que gerou o badge não foi consultado.

## Referências

- https://docs.docker.com/build/ci/github-actions/multi-platform/
- https://docs.github.com/en/actions/reference/runners/github-hosted-runners
- https://docs.github.com/en/billing/concepts/product-billing/github-actions
- https://docs.github.com/en/billing/concepts/product-billing/github-packages
- https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow
