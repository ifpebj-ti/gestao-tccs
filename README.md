![CI](https://github.com/ifpebj-ti/gestao-tccs/actions/workflows/ci.yml/badge.svg)
![Release](https://github.com/ifpebj-ti/gestao-tccs/actions/workflows/production.yml/badge.svg)
![License](https://img.shields.io/github/license/ifpebj-ti/gestao-tccs)
![Last Commit](https://img.shields.io/github/last-commit/ifpebj-ti/gestao-tccs)
![Top Languages](https://img.shields.io/github/languages/top/ifpebj-ti/gestao-tccs)
![Repo Size](https://img.shields.io/github/repo-size/ifpebj-ti/gestao-tccs)
![Contributors](https://img.shields.io/github/contributors/ifpebj-ti/gestao-tccs)
![Open Issues](https://img.shields.io/github/issues/ifpebj-ti/gestao-tccs)
![Open PRs](https://img.shields.io/github/issues-pr/ifpebj-ti/gestao-tccs)
![Forks](https://img.shields.io/github/forks/ifpebj-ti/gestao-tccs)
![Stars](https://img.shields.io/github/stars/ifpebj-ti/gestao-tccs)
![Coverage](https://img.shields.io/badge/Coverage-95%25-brightgreen) <!-- COVERAGE_BADGE -->

<!--![Tags Versions](https://img.shields.io/github/v/tag/ifpebj-ti/gestao-tccs)--> <!--Adicionar caso o sistema venha a ter versões separadas por tags-->

# Sistema de Gestão de TCCs

Sistema web para gerenciar Trabalhos de Conclusão de Curso (TCC), automatizando processos como cadastro, orientação, defesa e avaliação.

##  Tecnologias Utilizadas

O ambiente OCI já está em uso segundo a equipe. A promoção de novas versões continua manual. [Controles de CI/CD e evidências de auditoria](docs/security/ci-cd-auditoria.md)

Entrega de segurança da aplicação: [tasks, testes, ativação e pendências para 11/11/2026](docs/security/tasks-seguranca-2026-11-11.md).

- **Front-end:** React, Next.js
- **Back-end:** .Net
- **Banco de Dados:** PostgreSQL

![React](https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB) ![Next.js](https://img.shields.io/badge/Next.js-000000?style=for-the-badge&logo=nextdotjs&logoColor=white) ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white) ![PostgreSql](https://img.shields.io/badge/postgresql-4169e1?style=for-the-badge&logo=postgresql&logoColor=white)

## Arquitetura

```mermaid
flowchart LR
    Browser[Navegador] --> Proxy[Nginx Proxy Manager]
    Proxy --> Next[Frontend Next.js]
    Proxy --> WebApi[API .NET 10 · WebApi]
    WebApi --> Application[Application · casos de uso]
    Application --> Domain[Domain · entidades e contratos]
    WebApi --> Infra[Infra · implementações]
    Infra --> Domain
    Infra --> PostgreSQL[(PostgreSQL)]
    Infra --> Garage[(Garage S3)]
    WebApi --> Hangfire[Hangfire]
    Hangfire --> PostgreSQL
    WebApi -->|métricas| Prometheus[Prometheus]
    WebApi -->|logs e traces| Observability[Loki e Tempo]
    Prometheus --> Grafana[Grafana]
    Observability --> Grafana
```

##  Documentação e Wiki

Acesse a [Wiki do Projeto](https://github.com/ifpebj-ti/gestao-tccs/wiki) para mais detalhes sobre requisitos, arquitetura e fluxo do sistema.

Para contribuir, consulte o [guia de contribuição](CONTRIBUTING.md). Para relatar vulnerabilidades em privado, siga a [política de segurança](SECURITY.md).

**Apresentações do Projeto:**
*  [Sprint Report](https://www.canva.com/design/DAGyh_WU0jU/3eeINFORZW4p_HAiuHqmEg/view?utm_content=DAGyh_WU0jU&utm_campaign=designshare&utm_medium=link2&utm_source=uniquelinks&utlId=hfb7fd3492f)
*  [Sprint Report Melhorias](https://canva.link/o1j60bny3bs02z6)
  
##  Protótipo

Confira o protótipo interativo no [Figma](https://www.figma.com/design/gaL3ToIzCBEcyh09FpyXE1/Gest%C3%A3o-de-TCCs---Gov.BR?node-id=4002-2726&t=aqQQCIGpvvDAxGUC-1).

## Armazenamento de documentos

A API guarda documentos e assinaturas no Garage por meio da interface compatível com S3 e do AWS SDK para .NET. As variáveis `STORAGE_*` do `.env.example` descrevem a conexão.
