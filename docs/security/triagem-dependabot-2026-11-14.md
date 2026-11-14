# Organização das PRs do Dependabot

Consulta realizada em 14/11/2026 pela API pública do GitHub: 20 PRs abertas, todas direcionadas à main. Os checks consultados mostram 15 PRs sem falhas registradas e cinco com falhas. Esses resultados pertencem às revisões consultadas, não comprovam compatibilidade entre várias atualizações integradas em conjunto e podem ficar desatualizados.

## Tratamento por PR

| PR | Atualização | Resultado consultado | Tratamento |
| --- | --- | --- | --- |
| #769 | Radix Checkbox 1.4.1 | Sem falhas | Incluída no lote local de frontend; encerrar como substituída somente após integração e validação do lote. |
| #770 | React e tipos | Frontend e DAST falharam | Validar com React DOM no mesmo lote; o diff isolado não mantém o par atualizado. |
| #771 | PostCSS 8.5.32 | Sem falhas | Incluída no lote local; o lockfile pode selecionar patch posterior compatível. |
| #772 | Tailwind 4.3.5 | Sem falhas | Atualizar junto de @tailwindcss/postcss; incluída no lote local. |
| #773 | Radix Alert Dialog 1.2.1 | Sem falhas | Incluída no lote local de frontend. |
| #774 | React DOM | Frontend e DAST falharam | Validar com React e seus tipos; incluída no lote local. |
| #762 | docker/login-action 4.8.0 | Sem falhas | Revisar release notes e permissões, preservar SHA fixado e validar publicação. |
| #763 | actions/upload-artifact 7.0.2 | Sem falhas | Revisar compatibilidade, nomes e retenção dos artifacts; validar downloads e relatórios. |
| #765 | actions/setup-python 7.1.0 | Sem falhas | Revisar requisitos do runner e manter a versão de Python explicitamente configurada. |
| #766 | softprops/action-gh-release 3.0.4 | Sem falhas | Validar o contrato usado na publicação; o CI da PR não executa uma release real. |
| #767 | actions/setup-node 7.1.0 | Sem falhas | Revisar requisitos do runner e manter Node 24 no projeto. A versão da action não é a versão do Node da aplicação. |
| #768 | actions/setup-dotnet 6.1.0 | Sem falhas | Revisar requisitos do runner e manter SDK explicitamente configurado. Não representa migração do projeto .NET. |
| #779 | FluentValidation.AspNetCore 11.3.1 | Sem falhas | Revisar com o backend. O pacote foi descontinuado pelo mantenedor; um patch não resolve sua manutenção futura. |
| #778 | FluentValidation 12.2.0 | Sem falhas | Mudança major: revisar guia de migração, integração ASP.NET e validadores antes de integrar. |
| #777 | FluentAssertions 8.11.0 | Sem falhas | Revisar com a responsável pelos testes e executar a suíte. Não altera o plano de testes. |
| #776 | coverlet.collector 10.1.0 | Sem falhas | Mudança major da ferramenta de cobertura: validar formato OpenCover, coleta e gate de 95% antes de integrar. |
| #781 | Hangfire.AspNetCore 1.8.25 | DAST falhou | Revisar junto de Hangfire.Core, ainda em 1.8.20 na Application, e reproduzir o erro do DAST. A causa ainda não está comprovada. |
| #764 | Node 24 → 25 nas imagens | Sem falhas | Não integrar: Node 25 está EOL. Manter Node 24 LTS ou planejar migração coerente para outra LTS; registrar justificativa ao encerrar. |
| #760 | ASP.NET runtime 8 → 11 | DAST falhou | Não integrar isoladamente: projetos ainda usam net8.0. Substituir por uma migração conjunta de framework, SDK, runtime, CI e testes. |
| #761 | SDK .NET 8 → 11 | DAST falhou | Mesmo tratamento da #760. Uma troca de imagem não conclui a migração do runtime da aplicação. |

## Primeiro lote: frontend

O lote reúne as seis PRs npm (#769, #770, #771, #772, #773 e #774). React e React DOM usam a mesma versão; Tailwind e o plugin PostCSS também são atualizados juntos. Next.js permanece na versão atual nesta alteração. O lockfile é regenerado pelo npm, sem edição manual de hashes.

Antes de encerrar as PRs substituídas, exigir instalação pelo lockfile, testes, lint, TypeScript, build, verificação HTTP dos headers, auditoria de dependências e aprovação do CI com DAST. Referenciar a PR integrada no encerramento de cada original. Não considerar esse lote aprovado com base nos checks antigos das PRs individuais.

Validação local do lote:

- `npm ci`: instalação concluída pelo lockfile, incluindo o postinstall existente.
- `npm run test:ui`: 107 testes aprovados, sem falhas ou ignorados.
- `npm run lint`: sem avisos ou erros de lint; Next.js mantém o aviso de descontinuação de next lint.
- `npm run build`: build de produção concluído.
- `tsc --noEmit --incremental false`: sem erros.
- Verificação HTTP: resposta 200 e cabeçalhos defensivos aprovados.
- `npm audit --audit-level=high --json`: zero vulnerabilidades reportadas em todas as severidades.
- YAML do Dependabot: sintaxe e destinos conferidos com js-yaml; isso não substitui a validação do serviço GitHub.

O CI completo, o DAST e a revisão visual dos componentes atualizados permanecem necessários antes da integração. Não foram alterados o plano de testes, as dependências de testes do backend ou seus arquivos de teste.

## Organização das próximas atualizações

A configuração preparada direciona atualizações de versão à develop e agrupa React/tipos, Tailwind/plugin, Hangfire e as Actions. SDK e runtime .NET são agrupados para revisão conjunta. Os limites de PRs são explícitos nos quatro ecossistemas.

Atualizações de segurança npm/NuGet continuam configuradas para a branch padrão, sem abrir outro lote de atualizações de versão nela. A escolha de target-branch para develop não deve ser interpretada como mudança do destino das correções de segurança. Não foram criadas regras globais para ignorar majors ou vulnerabilidades.

As PRs atuais não devem ser encerradas em massa apenas para limpar o quadro. Cada encerramento deve indicar integração, substituição por outra PR validada ou decisão técnica acompanhada de uma tarefa de migração. Não marcar como corrigida uma vulnerabilidade apenas porque a PR foi fechada.

## Referências

- [Opções do Dependabot: grupos, branch de destino e limites](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference).
- [Ciclo oficial do Node.js](https://nodejs.org/en/about/previous-releases): Node 25 EOL e Node 24 LTS na consulta.
- [Migração para FluentValidation 12](https://docs.fluentvalidation.net/en/latest/upgrading-to-12.html).
- [Situação do FluentValidation.AspNetCore](https://github.com/FluentValidation/FluentValidation.AspNetCore).
- [Ciclo de suporte .NET](https://learn.microsoft.com/dotnet/core/releases-and-support): revisar a migração para uma versão suportada, sem manter .NET 8 indefinidamente.

A API pública não permite consultar a contagem dos alertas de vulnerabilidade sem autenticação. A existência de PRs e seus checks não comprova ausência de alertas. A conclusão da triagem exige consultar essa área com acesso adequado.
