# Changelog

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/)
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

---

## [2.10.0] - 2026-09-23

### Adicionado
- **Esteira de Produção & Validação de Release:** Teste e validação fim a fim do fluxo de release automatizado com geração de imagens de contêineres e sincronização de ambientes.
- **Estruturação de Telas de Avaliação e Assinatura:** Adicionados novos fluxos de avaliação de banca (`AvaliacaoForm.tsx`, `/avaliacao`) e assinaturas pendentes (`BankingSignatures.tsx`).

### Modificado
- **Cobertura de Código:** Atualização e consolidação da suíte de testes com cobertura de código acompanhada continuamente.

---

## [2.9.3] - 2026-09-23 - Fechamento do Marco 1

### Segurança (DevSecOps)
- **SAST (Static Application Security Testing):** Integração do Semgrep no fluxo de CI (`ci.yml`) para análise estática contínua do código-fonte C# e TypeScript ([#592](https://github.com/ifpebj-ti/gestao-tccs/issues/592)).
- **SCA (Software Composition Analysis):** Implementação de varreduras automáticas de repositório e dependências com Trivy ([#592](https://github.com/ifpebj-ti/gestao-tccs/issues/592)).
- **DAST (Dynamic Application Security Testing):** Adição de varredura dinâmica automatizada contra a aplicação com OWASP ZAP ([#592](https://github.com/ifpebj-ti/gestao-tccs/issues/592)).
- **Varredura de Contêineres:** Substituição do Docker Scout pelo Trivy Container Scan nos workflows de produção (`production-back.yml` e `production-front.yml`), garantindo zero vulnerabilidades críticas na imagem Docker ([#591](https://github.com/ifpebj-ti/gestao-tccs/issues/591)).
- **Vulnerabilidades do Dependabot:** Resolução completa de mais de 110 vulnerabilidades no frontend via upgrade do Next.js para 15.5.25 e dependências correlatas (0 vulnerabilidades no `npm audit`) ([#586](https://github.com/ifpebj-ti/gestao-tccs/issues/586)).
- **Dependência Backend (OpenTelemetry):** Atualização do pacote `OpenTelemetry.Exporter.OpenTelemetryProtocol` para 1.15.3, eliminando vulnerabilidade moderada GHSA-4625-4j76-fww9 e zerando o `dotnet list package --vulnerable` ([#597](https://github.com/ifpebj-ti/gestao-tccs/issues/597)).
- **Modelagem de Ameaças:** Elaboração da documentação formal de Threat Modeling e mapeamento de superfícies de ataque do sistema ([#585](https://github.com/ifpebj-ti/gestao-tccs/issues/585)).

### Infraestrutura e CI/CD
- **Versionamento Unificado do Sistema:** Resolução do desalinhamento histórico de versões entre backend (2.8.9) e frontend (2.8.6), unificando os contratos de API e o ciclo de releases ([#533](https://github.com/ifpebj-ti/gestao-tccs/issues/533), [#605](https://github.com/ifpebj-ti/gestao-tccs/issues/605)).
- **Autenticação Nativa no GHCR:** Migração de credenciais manuais expiradas para autenticação nativa com `GITHUB_TOKEN` e permissão `packages: write` na organização `ifpebj-ti` ([#605](https://github.com/ifpebj-ti/gestao-tccs/issues/605)).
- **Resiliência do Deploy:** Adição de `continue-on-error: true` no webhook do Portainer para garantir a continuidade do workflow mesmo em caso de indisponibilidade momentânea da VM na nuvem Oracle ([#605](https://github.com/ifpebj-ti/gestao-tccs/issues/605)).
- **Permissões Mínimas & Sintaxe YAML:** Definição do escopo restrito `permissions: contents: read` e correção de sintaxe no comando `npm audit` em bloco multilinha (`run: |`) ([#596](https://github.com/ifpebj-ti/gestao-tccs/issues/596)).

### Corrigido (Qualidade & Backend)
- **Mock de Teste Unitário (iText):** Correção do mock de arquivo assinado em `SignSignatureUseCaseTests.cs` através da criação de estrutura de PDF assinada válida em memória (`CreateSignedPdfBytes`), eliminando `NullReferenceException` ([#589](https://github.com/ifpebj-ti/gestao-tccs/issues/589)).
- **Tratamento Defensivo contra Nulos:** Refatoração de expressões lambda em `SignSignatureUseCase.cs` e validação defensiva com retorno `404 Not Found` para convites inexistentes em `VerifyCodeInviteTccUseCase.cs` (em conformidade com o CodeQL) ([#598](https://github.com/ifpebj-ti/gestao-tccs/issues/598)).
- **Suíte de Testes:** 113 testes unitários rodando e passando com 100% de sucesso.

### Governança
- **Templates de Issues:** Criação e padronização dos templates formais sem emojis em `.github/ISSUE_TEMPLATE/` (`bug_report.md`, `feature_request.md`, `task.md`) ([#593](https://github.com/ifpebj-ti/gestao-tccs/issues/593)).

---

## [2.9.0] - 2025-11-20

### Adicionado
- Implementação inicial de rotas de autorregistro e melhorias no fluxo de primeiro acesso.
- Integrações de banco de dados e migrações estruturais para membros de bancas e avaliações.

---

## [2.8.0] - 2025-11-01

### Adicionado
- Monitoramento e observabilidade através da stack Prometheus, Grafana, Promtail e Loki.
- Integração e armazenamento de documentos com MinIO.
