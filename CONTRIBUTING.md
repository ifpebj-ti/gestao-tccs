# Como contribuir

## Organização do trabalho

Registre o objetivo e os critérios de aceite em uma issue, usando os templates do repositório. Para vulnerabilidades, utilize o canal privado indicado na [política de segurança](SECURITY.md).

Crie uma branch a partir da `develop` atualizada. Use um nome que descreva a alteração, como `feat/validacao-formulario`, `fix/recuperacao-senha`, `docs/politica-seguranca` ou `chore/atualiza-dependencias`.

Abra a PR de trabalho para `develop`. A integração de `develop` para `main` deve ocorrer por uma PR própria, com revisão e evidências do commit avaliado. Não inclua alterações sem relação com o objetivo da tarefa.

Use mensagens de commit com tipo e descrição curta, por exemplo:

```text
feat: adiciona validação ao formulário de proposta
fix: corrige acesso ao documento do estudante
docs: documenta o canal de reporte de vulnerabilidades
chore: atualiza dependências do frontend
test: cobre rejeição de token expirado
```

## Configuração local

Consulte o `.env.example` para identificar as variáveis necessárias e configure valores próprios no ambiente local. Não reutilize chaves públicas de exemplo e não versione o `.env`.

A configuração atual do CI utiliza Node.js 24, .NET SDK 8 e PostgreSQL 15. Isso descreve a configuração do projeto; a atualização dos runtimes deve ser tratada com revisão de compatibilidade e segurança.

## Validação do frontend

Na pasta `frontend`, execute:

```sh
npm ci
npm run lint
npm run test:ui
npm run build
npx --no-install tsc --noEmit --incremental false
```

Após o build, na raiz do repositório, verifique a resposta HTTP e os cabeçalhos:

```sh
node scripts/verify-security-headers.cjs frontend
```

No PowerShell, se a política de execução impedir os atalhos `npm` e `npx`, utilize `npm.cmd` e `npx.cmd`.

Para alterações de interface, descreva os perfis e fluxos verificados no navegador. Inclua estados de carregamento, erro e ausência de dados quando forem afetados. Use apenas dados fictícios nas capturas e demonstrações.

## Validação do backend

Na pasta `backend`, execute:

```sh
dotnet restore --configfile NuGet.Public.config
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory coverage
```

Os testes de integração PostgreSQL exigem `GRADUS_PROPOSAL_TEST_DATABASE` apontando para um banco descartável exclusivo de testes. Sem essa configuração, parte dos testes é ignorada. Não utilize o banco de trabalho ou de produção.

Na raiz do repositório, com Python e as dependências instaladas, valide os scripts e a cobertura:

```sh
python -m pip install --require-hashes -r scripts/requirements-security.txt
python -m unittest discover -s scripts/tests -p "test_*.py"
python scripts/check-coverage.py backend/coverage 95
node --test scripts/tests/*.test.cjs
```

O gate exige 95% de cobertura global de sequências do backend. A medição não representa cobertura do frontend. Não reduza o limite nem acrescente exclusões apenas para fazer a esteira passar.

## Revisão e segurança

Preencha o [template de PR](.github/pull_request_template.md), incluindo issue vinculada, validação, impacto e rollback. Marque os critérios somente quando houver evidência. Solicite revisão de outro integrante e aguarde os checks obrigatórios.

O CI executa SAST, auditorias de dependências, varredura de segredos e DAST temporário. Investigue as falhas e registre a análise dos alertas. Não desative gates para contornar achados. Atualizações de dependências, inclusive do Dependabot, precisam de revisão de compatibilidade; um check verde não substitui essa revisão.

As regras de proteção de branches e as permissões são configuradas no GitHub. Este documento e os workflows não garantem que essas regras estejam ativas.

## Release e implantação

A pipeline de produção é acionada por PRs fechadas para `main`, com os filtros de arquivos definidos em [production.yml](.github/workflows/production.yml), ou por execução manual. O fluxo de publicação requer as validações do commit e das imagens.

O corpo da PR determina o incremento de versão. Marque exatamente uma opção, com `x` minúsculo:

| Opção | Efeito |
| --- | --- |
| `novo-marco` | Incrementa MAJOR e zera MINOR e PATCH. |
| `nova-feature-refactor` | Incrementa MINOR e zera PATCH. |
| `bug-fix` | Incrementa PATCH. |
| `outros` | Não cria uma nova release. |

O título da PR não determina a versão. Na execução manual, o workflow atual utiliza incremento MINOR; confira a execução existente antes de iniciar outra publicação.

Após a publicação, confira tag, release e imagens de frontend/backend com a mesma versão, os manifestos AMD64/ARM64 e a medição de cobertura usada no badge. Atualize o CHANGELOG com as alterações entregues e registre os links das evidências na issue. Os relatórios do CI têm retenção de 14 dias; preserve os necessários à avaliação antes da expiração.

Release e deploy são etapas separadas. O deploy depende de execução manual com a opção correspondente e da configuração do ambiente `production`. Enquanto a conta OCI não estiver disponível, mantenha o deploy desativado.
