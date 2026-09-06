![CI/CD](https://github.com/ifpebj-ti/gestao-tccs/actions/workflows/production-back.yml/badge.svg)
![CI/CD](https://github.com/ifpebj-ti/gestao-tccs/actions/workflows/production-front.yml/badge.svg)
![License](https://img.shields.io/github/license/ifpebj-ti/gestao-tccs)
![Last Commit](https://img.shields.io/github/last-commit/ifpebj-ti/gestao-tccs)
![Top Languages](https://img.shields.io/github/languages/top/ifpebj-ti/gestao-tccs)
![Repo Size](https://img.shields.io/github/repo-size/ifpebj-ti/gestao-tccs)
![Contributors](https://img.shields.io/github/contributors/ifpebj-ti/gestao-tccs)
![Open Issues](https://img.shields.io/github/issues/ifpebj-ti/gestao-tccs)
![Open PRs](https://img.shields.io/github/issues-pr/ifpebj-ti/gestao-tccs)
![Forks](https://img.shields.io/github/forks/ifpebj-ti/gestao-tccs)
![Stars](https://img.shields.io/github/stars/ifpebj-ti/gestao-tccs)
![Coverage](https://img.shields.io/badge/Coverage-73%25-brightgreen) <!-- COVERAGE_BADGE -->

<!--![Tags Versions](https://img.shields.io/github/v/tag/ifpebj-ti/gestao-tccs)--> <!--Adicionar caso o sistema venha a ter versões separadas por tags-->

# 📘 Sistema de Gestão de TCCs

Sistema web para gerenciar Trabalhos de Conclusão de Curso (TCC), automatizando processos como cadastro, orientação, defesa e avaliação.

## 🛠 Tecnologias Utilizadas

- **Front-end:** React, Next.js
- **Back-end:** .Net
- **Banco de Dados:** PostgreSQL

![React](https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB) ![Next.js](https://img.shields.io/badge/Next.js-000000?style=for-the-badge&logo=nextdotjs&logoColor=white) ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white) ![PostgreSql](https://img.shields.io/badge/postgresql-4169e1?style=for-the-badge&logo=postgresql&logoColor=white)

## 📖 Documentação e Wiki

Acesse a Wiki do Projeto para mais detalhes sobre requisitos, arquitetura e fluxo do sistema.

Acompanhe o [Sprint Report](https://www.canva.com/design/DAGyh_WU0jU/3eeINFORZW4p_HAiuHqmEg/view?utm_content=DAGyh_WU0jU&utm_campaign=designshare&utm_medium=link2&utm_source=uniquelinks&utlId=hfb7fd3492f)!

## 🎨 Protótipo

Confira o protótipo interativo no [Figma](https://www.figma.com/design/gaL3ToIzCBEcyh09FpyXE1/Gest%C3%A3o-de-TCCs---Gov.BR?node-id=4002-2726&t=aqQQCIGpvvDAxGUC-1).

## 📦 Minio

📦 Dependências de Terceiros
Este sistema utiliza o serviço de armazenamento de objetos MinIO para gerenciamento de arquivos, como documentos de TCC.

📜 Licenciamento do MinIO
O MinIO Server é licenciado sob a GNU Affero General Public License v3 (AGPL v3).
De acordo com esta licença:

O código-fonte do MinIO está disponível em: https://github.com/minio/minio

O texto completo da licença pode ser encontrado em: https://www.gnu.org/licenses/agpl-3.0.html

Este projeto utiliza o MinIO como um serviço externo via API REST, sem modificações em seu código-fonte.

A integração é feita por meio do SDK oficial do MinIO para .NET, licenciado sob a Apache 2.0 License, o que permite seu uso livre nesta aplicação.
