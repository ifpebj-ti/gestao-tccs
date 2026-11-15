# Atualização das GitHub Actions

Lote preparado em branch independente da atualização de dependências do frontend. Consolida as propostas #762, #763, #765, #766, #767 e #768, sem modificar a PR que aguarda revisão.

## Versões e fontes

Os arquivos action.yml e README.md foram consultados nos repositórios oficiais, nos SHAs propostos pelo Dependabot. As referências continuam fixadas em commits de 40 caracteres.

| PR | Action | Versão proposta | Referência oficial consultada |
| --- | --- | --- | --- |
| #762 | docker/login-action | 4.8.0 | [action.yml](https://github.com/docker/login-action/blob/8aa5831fb8d48d8f76594c7ddde61ae58019f550/action.yml) |
| #763 | actions/upload-artifact | 7.0.2 | [action.yml](https://github.com/actions/upload-artifact/blob/cf430e030ddbb5b0abf93d22962f4752f3646cd9/action.yml) |
| #765 | actions/setup-python | 7.1.0 | [action.yml](https://github.com/actions/setup-python/blob/6d4fd22b20465ccc0f7354476588e8df7c9ca0e0/action.yml) |
| #766 | softprops/action-gh-release | 3.0.4 | [action.yml](https://github.com/softprops/action-gh-release/blob/8a0cd651e5b2365bd640854a3fa5dfc70e4875f3/action.yml) |
| #767 | actions/setup-node | 7.1.0 | [action.yml](https://github.com/actions/setup-node/blob/949feb2413d6458794dcd2491c4babbbce0c15c1/action.yml) |
| #768 | actions/setup-dotnet | 6.1.0 | [action.yml](https://github.com/actions/setup-dotnet/blob/0f4a354dd5547a28c70b7853dbd95d5b67886e73/action.yml) |

## Compatibilidade e limites

As seis versões usam Node 24 internamente. Os READMEs de setup-node, setup-python e setup-dotnet indicam runner 2.327.1 ou posterior para esse runtime. O projeto usa runners hospedados pelo GitHub; a execução na plataforma ainda precisa confirmar a compatibilidade efetiva.

A atualização das Actions não migra a aplicação para outra versão de Node, Python ou .NET. Foram preservadas as versões explicitamente configuradas nos workflows. A migração do backend net8.0 para uma versão suportada permanece como trabalho separado.

setup-node passou a oferecer cache automático por detecção de package manager. O lote define package-manager-cache: false nos passos em que não havia cache configurado, inclusive nos fluxos de publicação. O cache npm explícito do frontend no CI permanece.

upload-artifact mantém nomes, caminhos e retenção de 14 dias, sem ativar upload de arquivos ocultos. Ainda é necessário conferir os artifacts produzidos pelo GitHub, pois a validação local não realiza uploads reais.

action-gh-release recebe o token pelo input próprio, substituindo o fallback por variável de ambiente no step. A permissão continua sendo a já concedida ao job; não foi adicionado PAT, segredo ou bypass. tag_name, target_commitish, nome, notas e corpo da release foram preservados.

docker/login-action continua usando GHCR e o GITHUB_TOKEN do job. Os logins condicionais dos candidatos permanecem condicionais. O lote não executa autenticação ou publicação local com credenciais reais.

## Validação local

- 45 testes Node dos scripts aprovados.
- Quatro workflows aprovados pelo actionlint 1.7.7, mesma versão utilizada pelo CI.
- 27 ocorrências das seis Actions conferidas contra os inputs declarados nos metadados oficiais, incluindo inputs obrigatórios.
- As versões de aplicação, gates, permissões de jobs, aprovação de PR e opt-in do deploy foram preservados.

## Critérios para concluir o lote

- [ ] Aprovar o CI completo da PR, incluindo SAST, SCA, DAST e Quality gate.
- [ ] Conferir a disponibilidade dos relatórios e a retenção dos artifacts no GitHub.
- [ ] Obter revisão de outra integrante e integrar à develop.
- [ ] Aprovar o CI da develop após a integração.
- [ ] Integrar à main pelo fluxo de PR e validar a publicação.
- [ ] Conferir login GHCR, scans, manifestos, tag, release e atualização do badge na execução real.
- [ ] Encerrar as seis propostas antigas como substituídas, com link para a entrega integrada.

Não encerrar as PRs antigas como resolvidas somente com os testes locais. A geração de release e os logins condicionais não são executados pelo CI comum da PR.
