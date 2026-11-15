# Integração com a develop e cobertura — 11/11/2026

Base avaliada: `cff0d8960c3cedc56800ac41882704a625a4cf81`, após os PRs de testes #750 e #752. A integração reúne as branches de CI/CD e segurança. A medição usa a mesma configuração `backend/coverlet.runsettings`, Release, PostgreSQL descartável e OpenCover, sem filtros ou exclusões adicionais.

| Evidência | Develop original | Integração validada |
| --- | ---: | ---: |
| Testes de backend aprovados | 504 | 572 |
| Falhas / ignorados | 0 / 0 | 0 / 0 |
| Sequências visitadas / totais | 25.069 / 28.249 | 27.176 / 28.340 |
| Cobertura global de sequências | 88,74% | **95,89%** |
| Cobertura global de decisões | 57,34% | 69,13% |

O percentual global é ponderado pela quantidade de sequências, incluindo migrations. Ele **não significa 95% de cada módulo** nem 95% de cobertura de decisões:

| Módulo | Cobertura de sequências na integração |
| --- | ---: |
| Domain | 87,76% |
| Application | 93,21% |
| Infra | 99,19% |
| WebApi | 68,17% |

## Preservação dos testes

- Os 504 nomes de casos executados na base aparecem também no TRX final; nenhum ficou de fora.
- Os dez arquivos alterados nos dois lotes da colega mantêm os mesmos blobs Git da base. A comparação local considera a conversão automática de finais de linha do Git no Windows.
- Nenhum arquivo de teste foi removido. As adaptações anteriores da branch de segurança conservam os cenários existentes e adequam a preparação às operações atômicas de recuperação e primeiro acesso.
- A comparação por classe, agrupando máquinas de estado assíncronas, não encontrou queda de cobertura de sequências ou decisões em relação à base.

## Cenários acrescentados

- Senhas inválidas e maiores que o limite do BCrypt, concorrência no consumo de códigos, convites expirados e contas inativas.
- Rejeição de HTML disfarçado de PDF e limites de entrada do conversor.
- HTTP real com JWT validado contra PostgreSQL: permissões administrativas, revogação após mudança de senha, cadastro, login, proposta recusada/reformulada/aprovada e upload.
- Respostas para TCCs e documentos inexistentes, sem criar dados; contrato OpenAPI e cabeçalhos de segurança.
- Persistência e isolamento de campus, cursos e semestres; inicialização idempotente; rollback e reaplicação de todas as migrations em esquema vazio, exclusivo de testes.
- Contrato S3 com cliente substituído: bytes, MIME, namespace, bucket, objetos ausentes, erros de acesso, paginação e ZIP.

## Esteira e reprodução

A etapa `Enforce 95 percent coverage` bloqueia o backend abaixo de 95% global, sem arredondar para aprovar. O teste de fronteira verifica 95%, 94,999% e 90%. Os testes de publicação normalizam CRLF ao ler o workflow, preservando as mesmas verificações em Windows e Linux.

Validações complementares: 107 testes de frontend, 39 testes Node da esteira, sete testes Python e Actionlint aprovados. Os relatórios desta execução local estão em `reports/merge-validation/baseline` e `reports/merge-validation/merged-8` (ignorados pelo Git). No GitHub, os TRX/OpenCover serão os artefatos `backend-evidence` do commit executado.

Para reproduzir, use somente o PostgreSQL temporário descrito em `ci-cd-auditoria.md`, configure `GRADUS_PROPOSAL_TEST_DATABASE` e execute, no diretório `backend`:

```sh
dotnet test --configuration Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory coverage --logger "trx;LogFileName=tests.trx"
python3 ../scripts/check-coverage.py coverage 95
```

Essa validação não comprova aprovação dos scanners remotos nem implantação na OCI. O deploy continua pendente da liberação da conta e da configuração do ambiente.
