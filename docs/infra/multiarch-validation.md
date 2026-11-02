# Validação isolada AMD64 e ARM64

Este processo não faz deploy, não acessa a OCI, não chama webhooks e não publica
imagens. Os Dockerfiles e o workflow de produção permanecem independentes.
As imagens existentes `latest` e de releases não são alteradas.

## Arquivos e execução

- `.github/workflows/multiarch-validation.yml`: disparo manual, quatro combinações
  de serviço/arquitetura, permissões somente de leitura e `push: false`.
- `frontend/Dockerfile.multiarch`: instalação limpa no destino, testes de UI,
  lint, build Next.js e TypeScript. Nenhum `node_modules` do host é copiado.
- `backend/Dockerfile.multiarch`: testes no SDK da arquitetura de destino e
  publicação framework-dependent para `linux-musl-x64` ou `linux-musl-arm64`.
- `scripts/multiarch-smoke.sh`: confere arquitetura, testa SWC, Lightning CSS e
  Sharp, inicia frontend ou backend e exige resposta HTTP. O backend usa um
  PostgreSQL descartável sem portas públicas nem dados de produção.
- O build de validação limita o Next.js a um worker (`CIRCLE_NODE_TOTAL=2`) e
  o MSBuild a um processo, para reduzir pressão de memória sob QEMU. Esses
  limites não alteram o Dockerfile de produção nem retiram testes.
  `scripts/buildkit-validation.toml` também limita a um estágio ativo por builder
  nos novos builders do workflow e no comando de criação abaixo.
- `scripts/multiarch-inventory.cjs`: consulta os manifests públicos da infraestrutura.

Após revisão e envio autorizado da feature, o workflow pode ser disparado
manualmente. Um workflow novo pode exigir estar na branch padrão para aparecer
na interface de Actions; não faça merge só para executá-lo sem revisão.
Nenhum workflow remoto foi disparado nesta preparação local.

Com Docker Buildx instalado, execute da raiz, substituindo `COMMIT` pelo SHA:

```bash
docker buildx create --name gradus-arm64-validation --driver docker-container \
  --buildkitd-config scripts/buildkit-validation.toml
docker buildx inspect gradus-arm64-validation --bootstrap
# Docker Desktop pode já fornecer emulação. Em Linux sem emulação, instale
# binfmt/QEMU conscientemente; essa instalação altera o host Docker.
for service in frontend backend; do
  for arch in amd64 arm64; do
    docker buildx build --builder gradus-arm64-validation \
      --platform "linux/$arch" -f "$service/Dockerfile.multiarch" \
      -t "gradus-validation/$service:sha-COMMIT-$arch" --load "$service"
    bash scripts/multiarch-smoke.sh "$service" "linux/$arch" \
      "gradus-validation/$service:sha-COMMIT-$arch"
  done
done
node scripts/multiarch-inventory.cjs
```

Para exportar uma imagem com ambas as plataformas sem publicar:

```bash
docker buildx build --builder gradus-arm64-validation \
  --platform linux/amd64,linux/arm64 -f frontend/Dockerfile.multiarch \
  -t gradus-validation/frontend:sha-COMMIT \
  --output type=oci,dest=frontend-COMMIT.oci.tar frontend
# Repita para backend com seu contexto e Dockerfile.
```

O input manual `export_oci` faz essa exportação no GitHub somente após todos os
smokes passarem. Os arquivos grandes são opcionais e expiram em um dia; revise
limites de armazenamento/minutos do GitHub antes de habilitar. Nenhum recurso
OCI ou crédito de trial é necessário. Não há autenticação GHCR neste fluxo.
Uma futura publicação de teste exige autorização separada e permissão de pacote;
não envie tokens no chat. Use apenas tags novas `validation-SHA`, nunca `latest`
ou versões existentes.

## Infraestrutura e limites da evidência

Em 2026-11-01, os manifests de `postgres:15`, `dxflrs/garage:v1.0.1`,
`jc21/nginx-proxy-manager:latest`, `portainer/portainer-ee:latest`,
`prom/prometheus:latest`, `grafana/loki:latest`, `grafana/tempo:latest`,
`grafana/grafana:latest` e `axllent/mailpit:latest` anunciaram `linux/amd64`
e `linux/arm64`. As bases Node 24 Alpine, .NET SDK 8 e ASP.NET 8 Alpine também.
Tags mutáveis precisam ser verificadas novamente e fixadas por versão/digest
na preparação do ambiente final. O manifest não comprova configuração, licença
Portainer EE, persistência nem funcionamento integrado de todos os serviços.

O Compose de produção não foi alterado. Ele usa `APP_VERSION` para selecionar
as imagens e continua dependendo das redes externas de gerenciamento e
observabilidade. O Garage do Compose de desenvolvimento não está definido no
Compose de produção: a implantação futura precisa configurar o endpoint S3
ou acrescentar a stack de armazenamento sem remover os demais componentes.

Os endereços API usados nesta validação são fictícios. A URL pública real precisa
ser configurada no build do frontend antes de uma futura publicação de produção.
Nenhum secret foi alterado. A validação integrada posterior corrigiu um conflito
de portas em `garage.toml`: `[s3_web]` passou a escutar em 3903, deixando 3902
exclusiva para RPC. Não é necessário publicar a porta web para usar a API S3.

Os testes PostgreSQL que exigem `GRADUS_PROPOSAL_TEST_DATABASE` são explicitamente
ignorados quando essa variável não existe. O smoke do backend testa migrations,
seed e inicialização contra banco descartável; ele não substitui esses testes
de integração nem valida upload S3, SMTP ou o fluxo inteiro de assinaturas.
Emulação QEMU não equivale a uma medição de desempenho numa Ampere A1 real.

## Resultados locais

Base: `origin/main` em `162cf2e`, feature `feat/oci-arm64-validation`, sem commit,
push ou execução remota de workflows. Docker Engine AMD64, Buildx 0.38.1 e
BuildKit 0.33.1 locais; ARM64 executado com QEMU disponibilizado pelo Docker.

| Serviço | Plataforma | Testes | Build | Smoke |
| --- | --- | --- | --- | --- |
| Frontend | linux/amd64 | 96 aprovados; lint e tipos aprovados | aprovado | SWC, Sharp, Lightning CSS e HTTP 200 |
| Backend | linux/amd64 | 335 aprovados; 11 ignorados; PDF real incluído | linux-musl-x64 aprovado | migrations, seed, Hangfire e HTTP 200 em /metrics |
| Frontend | linux/arm64 | 96 aprovados; lint e tipos aprovados | aprovado após limitar workers e executar isoladamente | SWC, Sharp, Lightning CSS e HTTP 200 sob QEMU |
| Backend | linux/arm64 | 335 aprovados; 11 ignorados (log local e etapa reaproveitada do cache) | linux-musl-arm64 aprovado | migrations, seed, Hangfire e HTTP 200 em /metrics sob QEMU |

Na primeira tentativa concorrente, o Docker local passou a retornar HTTP 500 em `/version` e na consulta dos
processos do builder enquanto os dois builds ARM64 rodavam simultaneamente.
Os clientes de build foram interrompidos; o agente não reiniciou o Docker Desktop,
pois isso poderia afetar outros containers. Após o usuário reiniciar o Docker,
o frontend ARM64 foi reconstruído e seu smoke passou com o limite de workers.
Em nova retomada local, o backend ARM64 concluiu o publish e o smoke com PostgreSQL
15 ARM64 descartável. O runtime confirmou `arm64` e `linux-musl-arm64`; a API
respondeu HTTP 200 em `/metrics`. O log salvo da execução anterior confirmou
335 testes aprovados e 11 ignorados; essa etapa foi reaproveitada do cache no
build retomado. O erro 500 do motor não reapareceu durante essa execução isolada;
isso não determina a causa original. Os arquivos OCI multi-plataforma continuam pendentes.
O smoke do frontend ARM64 também foi repetido após essa retomada: SWC, Sharp,
Lightning CSS e HTTP 200 aprovados. Ambos os smokes terminaram com código zero
e removeram seus containers e redes temporários.
O verificador de índices OCI foi testado com arquivos sintéticos: rejeitou
AMD64 sozinho e aceitou AMD64+ARM64, incluindo um índice aninhado.

Os builds AMD64 de ambos os serviços foram repetidos com a configuração final:
backend aprovado; frontend com 96 testes aprovados, lint, build e TypeScript
aprovados. As imagens ARM64 também concluíram seus builds e smokes.
Os Dockerfiles, Composes e workflow de produção continuam independentes.

Tags locais usam o prefixo `gradus-validation/` e o sufixo
`162cf2e-working-ARQUITETURA` para distinguir o código ainda não commitado.
Não são imagens de produção e contêm URL de API fictícia.

O inventário está em `multiarch-inventory-2026-11-01.jsonl`. O hash registrado é
do JSON inspecionado; não deve ser tratado como digest de uma imagem publicável.
O suporte de manifests foi comprovado. PostgreSQL, Garage e Mailpit também foram
executados na stack descartável descrita abaixo. Proxy, Portainer e observabilidade
continuam com evidência de manifests apenas; não foram validados em execução.

## Validação integrada antes do envio ao GitHub

`scripts/multiarch-stack-smoke.cjs` cria uma rede exclusiva e cinco containers:
frontend, backend, PostgreSQL 15, Garage 1.0.1 e Mailpit. Não lê `.env`, não expõe
portas do host e não monta volumes de dados existentes. Usa dados e credenciais
descartáveis; remove os containers, volumes anônimos e a rede no final.

Da raiz, com as imagens locais da arquitetura já construídas:

```bash
node scripts/multiarch-stack-smoke.cjs linux/amd64 162cf2e-working-amd64
STACK_GARAGE_DB_ENGINE=sqlite node scripts/multiarch-stack-smoke.cjs linux/arm64 162cf2e-working-arm64
```

No PowerShell, defina `$env:STACK_GARAGE_DB_ENGINE = 'sqlite'` antes do comando
ARM64. A configuração padrão do teste é LMDB. O sufixo das tags pode ser passado
como terceiro argumento para testar builds futuros.

Checks executados: HTML do frontend e URL de API em runtime; `/metrics`;
login da estudante do seed; consulta autenticada de `/api/User/me`; rejeição
HTTP 401 sem token; preflight CORS; upload e download S3 com assinatura SigV4;
envio SMTP e confirmação da mensagem pela API do Mailpit.
S3 e SMTP são exercitados diretamente pelo probe, sem passar pelos gateways
.NET; este teste não comprova o fluxo completo de anexos, e-mails ou assinaturas.

Resultado final local (2026-11-01, horário de São Paulo):

| Verificação | Resultado |
| --- | --- |
| Stack AMD64 com `garage.toml` corrigido e LMDB | aprovada; cinco serviços e todos os checks |
| Stack ARM64 com `garage.toml` corrigido e SQLite sob QEMU | aprovada; cinco serviços e todos os checks |
| Backend completo com PostgreSQL descartável (AMD64) | 346 aprovados, zero falhas, zero ignorados |
| Workflow novo / smoke Bash / scripts CJS | sintaxe aprovada |

A suíte completa de backend utilizou o banco `gradus_proposal_test` na porta
55439 de um namespace de rede compartilhado apenas pelos containers de teste,
sem publicar a porta no host. O teste exige host `127.0.0.1`, porta 55439 e esse
nome de banco, impedindo o uso acidental do banco da aplicação.
No SDK de testes, configure `GRADUS_PROPOSAL_TEST_DATABASE` com essa conexão e
execute `dotnet test gestaotcc.Test/gestaotcc.Test.csproj --configuration Release`.

Na primeira execução completa houve 344 aprovações e duas falhas: os testes
`EmailMigrationPreservesExistingInvitationsAndHidesThemUntilConsent` e
`DowngradeRefusesToDiscardInvitationsWithoutAnAccount` acessavam a coluna
`FinalSubmissionDeadline` após reverter a migration que a criou. A preparação
do primeiro teste passou a salvar a proposta antes do downgrade; o segundo
consulta somente `Status` no esquema antigo e exige SQLSTATE `P0001` e a mensagem
específica do bloqueio de convites sem conta. A nova execução aprovou os 346
testes. Nenhuma migration ou regra da aplicação foi alterada nessa correção.
Essa execução completa foi AMD64; a etapa ARM64 anterior teve 335 aprovações e
11 integrações ignoradas por ausência de conexão de teste. As imagens de runtime
continuam equivalentes: as correções posteriores afetam apenas o código dos testes.

A primeira execução ARM64 com LMDB encerrou no startup do Garage com
`Cannot open LMDB database: Function not implemented (os error 38)` neste
Docker Desktop com QEMU. O teste ARM64 com SQLite passou. Isso não comprova
LMDB em ARM64 nativo e não justifica trocar o engine de um volume existente.
A futura VM precisa de validação própria. A documentação do Garage explica
as opções de metadados e a incompatibilidade dos arquivos LMDB entre arquiteturas:
https://garagehq.deuxfleurs.fr/documentation/reference-manual/configuration/

O `garage.toml` original também foi iniciado em um container AMD64 vazio:
o conflito RPC/web em 3902 produziu `AddrInUse` e saída 139. Após a correção para
3903, a stack AMD64 com o arquivo do repositório e LMDB passou integralmente.
Os containers da aplicação que já estavam ativos não foram recriados.

`actionlint` aprovou o workflow novo; `shellcheck` aprovou o smoke Bash;
`node --check` aprovou os scripts CJS. As referências das actions Docker
setup-qemu v4, setup-buildx v4 e build-push v7 foram conferidas nos releases
oficiais. Nenhum workflow remoto foi disparado.

O envio da feature ao GitHub serve para revisão e validação. O workflow existente
de produção ainda usa `docker build` sem matriz de plataformas; não publica
automaticamente as imagens ARM64 destes Dockerfiles de validação. Antes do
deploy na OCI, preparar a publicação multi-plataforma, a configuração final,
persistência e teste na VM. Arquivos OCI exportados são opcionais e continuam
pendentes. Não há deploy nem publicação de imagens nesta validação.

## Referências

- https://docs.docker.com/build/ci/github-actions/multi-platform/
- https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/containers
- https://github.com/docker/build-push-action
