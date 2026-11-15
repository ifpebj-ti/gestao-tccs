# Atualização conjunta das dependências do backend e frontend

## Escopo

Esta entrega reúne as atualizações compatíveis que estavam distribuídas entre PRs do Dependabot. A integração depende do CI da PR e da revisão de outro integrante. Nenhuma PR original deve ser encerrada como corrigida antes da integração da substituta.

## Backend

- Projetos, SDK do CI e imagens Docker passam a utilizar .NET 10.
- `backend/global.json` seleciona a família de SDK 10, sem aceitar versões de prévia.
- Hangfire.Core e Hangfire.AspNetCore passam a 1.8.25; Hangfire.PostgreSql passa a 1.21.1.
- FluentValidation passa a 12.2.0. O pacote descontinuado FluentValidation.AspNetCore é substituído pelo pacote DependencyInjectionExtensions da mesma versão. A aplicação já utiliza a validação manual e o registro por assembly; nenhum controller ou validador foi removido.
- FluentAssertions passa a 8.11.0 e coverlet.collector a 10.1.0.
- `.dockerignore` impede que binários e resultados locais entrem no contexto de build. Os arquivos de testes continuam incluídos.

Os pacotes Entity Framework Core permanecem em suas versões atuais. A atualização do framework não representa atualização automática de todos os pacotes Microsoft. Não foram geradas migrações de banco nesta entrega.

### Validações do backend

- Suíte .NET 10 com PostgreSQL descartável: 573 testes aprovados, zero falhas, zero ignorados.
- Cobertura OpenCover: 95,96% (54.388 de 56.676 pontos de sequência).
- Gate mínimo mantido em 95%, sem novas exclusões.
- Auditoria NuGet de dependências diretas e transitivas: nenhum bloqueio alto ou crítico.
- Imagem Linux AMD64 com runtime 10.0.15: inicialização, migrações existentes, seed, Hangfire e HTTP de métricas aprovados em banco descartável.
- Trivy 0.76.0: nenhuma vulnerabilidade reportada na imagem local examinada em 15/11/2026. O CI utiliza a versão fixada no instalador do projeto e deve repetir o scan; bases de vulnerabilidades podem mudar.

A construção e a execução ARM64 desta migração ainda precisam ser confirmadas no workflow de candidatos antes da integração. Os resultados AMD64 não substituem essa confirmação.

## Frontend

- Radix Tabs passa a 1.2.1.
- A versão mínima de @eslint/eslintrc passa a 3.3.7 e a de @types/react-dom a 19.3.0.
- TypeScript é fixado em 5.9.3, validado com as ferramentas existentes.
- Next.js e eslint-config-next permanecem em 15.5.28.

Validação da instalação final pelo lockfile: `npm ci` concluído; 107 testes aprovados, sem falhas ou testes ignorados; lint e TypeScript sem erros; build de produção e verificação HTTP dos cabeçalhos aprovados. A auditoria npm da entrega não reportou vulnerabilidades. Os resultados do CI devem ser anexados à PR antes da integração.

### Preservação dos testes

Nenhum arquivo ou cenário existente do frontend foi excluído. O teste de compatibilidade de minimatch continua percorrendo todas as cópias presentes no lockfile. O npm deduplicou essas cópias de sete para uma, reduzindo seis repetições da mesma verificação em caminhos que deixaram de existir.

Foram acrescentadas seis regressões do Radix Tabs, verificando nome e papéis acessíveis, seleção inicial, seleção controlada, atributo disabled, orientação vertical e associação entre IDs das abas e painéis. Essas verificações exercitam a renderização no servidor; não substituem testes de interação por teclado no navegador.

Nenhum teste antigo foi marcado como ignorado. Os arquivos de teste do backend e a configuração do coletor de cobertura permanecem preservados; o projeto de testes muda apenas o framework e as versões de dois pacotes.

## Tratamento das PRs originais

| PR | Tratamento após integração e CI aprovado |
| --- | --- |
| #806 | Substituída pela atualização conjunta do Hangfire. |
| #807 | Substituída pela atualização do coletor, com formato e gate de cobertura validados. |
| #808 | Substituída pela atualização de FluentAssertions, preservando a suíte. |
| #809 | Substituída pela atualização de FluentValidation e sua integração de DI. |
| #810 | Substituída pela remoção do pacote descontinuado; não é necessário manter seu patch em paralelo. |
| #800 | Substituída pela migração conjunta para .NET 10; a proposta isolada para imagens 11 não é integrada. |
| #804 | Substituída pela atualização de Radix Tabs. |
| #802 | Substituída pela atualização de @eslint/eslintrc. |
| #801 | Substituída pela atualização do limite de @types/react-dom. |
| #803 | Migração Next.js 16 pendente; não considerar atendida por esta entrega. |
| #805 | Migração TypeScript 7 pendente; não considerar atendida por esta entrega. |

As tentativas com Next.js 16 exigiram adequações ao lint. TypeScript 7 apresentou incompatibilidade com APIs usadas pelas ferramentas de teste. Não foram desativadas regras ou removidos testes para aceitar essas migrações.

## Integração e recuperação

1. Aguardar CI completo, incluindo SAST, SCA, segredos, DAST e cobertura.
2. Executar o workflow de candidatos na branch desta entrega, com publicação desmarcada, conferindo as quatro combinações de serviço e arquitetura.
3. Conferir os fluxos principais da interface antes da aprovação.
4. Integrar à develop com revisão, aguardar o CI e publicar pela PR normal para main.
5. Encerrar somente as PRs originais efetivamente substituídas, vinculando a entrega integrada.

A mudança de runtime exige reconstrução da imagem do backend. Não modifica os contêineres locais em execução nem implanta na OCI. Para recuperar uma publicação com problema, usar uma versão anterior já validada e reverter a alteração por PR, conferindo antes a compatibilidade do banco. Nenhuma alteração de esquema foi adicionada nesta entrega.
