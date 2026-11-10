# ADR 0001 — Bloqueios de segurança e separação entre release e deploy

Data: 10/11/2026

Status: implementado nos arquivos da esteira; execução e configuração do GitHub pendentes.

## Contexto

O CI anterior neutralizava falhas de npm audit, Trivy e DAST. O workflow de produção podia publicar sem depender desse CI. O DAST apontava para um domínio público, embora a equipe ainda aguarde a liberação da conta OCI para implantação. A auditoria requer evidências de bloqueio, revisão por pares e testes em ambiente temporário.

## Decisão

- Reutilizar o CI como pré-requisito da release do mesmo commit.
- Bloquear falhas de teste, cobertura inferior a 90%, achados das políticas de segurança e falhas operacionais dos scanners.
- Executar testes de integração com PostgreSQL temporário e DAST com frontend, API e banco descartáveis, sem credenciais de produção.
- Reter relatórios de ferramentas e cobertura por 14 dias; exportar as evidências necessárias à auditoria antes da expiração.
- Separar deploy da publicação. Exigir opt-in manual e usar o environment `production`; as proteções desse environment precisam ser configuradas no GitHub.
- Exigir HTTPS válido nos webhooks. Não contornar erros de certificado.

## Consequências

A esteira pode ficar vermelha até a equipe corrigir os achados reais e alcançar a cobertura exigida. Isso é o comportamento esperado, sem mascarar dívida existente. Os builds e scanners aumentam o tempo de execução, inclusive pela revalidação do commit de release.

O DAST não depende da OCI. Seu escopo autenticado, uploads e serviços externos ainda precisam de testes complementares. A implantação real, os checks de saúde, a promoção por versão/digest e o rollback dependem do provisionamento e de validação em staging.

Branch Protection e qualidade de revisão não são garantidas por YAML. A equipe deve demonstrá-las com regras ativas e PRs reais. O mínimo da esteira é 90% de cobertura de sequências do backend; a meta de 95% da equipe está sendo tratada na frente de testes.

Detalhes e plano de ação: [preparação para auditoria](../security/ci-cd-auditoria.md).
