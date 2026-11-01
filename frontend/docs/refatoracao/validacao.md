# Validação da refatoração

## Resultado da etapa atual — 13/10/2026

Atualização após revisão visual da estudante: **57 testes frontend, 24 verificações no navegador, lint, TypeScript e rebuild aprovados**. Frontend `a3e7492578b1` em localhost:3000, HTTP 200. Inventário e limites em [Design da aluna](design-aluna.md); novas evidências em `evidencias/design-aluna`. Os resultados abaixo registram a etapa anterior. O backend manteve a versão previamente validada, sem mudanças nesta revisão.

55 testes frontend e 202 backend aprovados; lint e TypeScript aprovados; builds Docker de frontend (Next.js 15.5.27, 28 rotas) e backend (.NET 8) aprovados. Imagens finais: frontend `2c36b5c3572d`, backend `1ca20ec65b98`, iniciadas sem remover volumes. localhost:3000 respondeu HTTP 200; a API local recusou bearer fictício inválido com 401 em GET da conta e POST da senha.

Os 202 testes backend também passaram no .NET 8 nativo dentro de container temporário, sem rede, usando o cache público da imagem como feed local. A execução não acessou banco nem realizou seeds. Os comandos e a resolução da referência flutuante estão em Qualidade e conta.

O Chrome isolado aprovou 15 verificações reais de interação do frontend compilado: validação sem envio indevido, erro/recuperação da API, payload da conta restrito aos seis campos, nome corrigido na navegação, retorno do navegador, TCC aprovado, troca de senha interna e encerramento local da sessão. Desktop 1440 px e mobile 390 px sem overflow da página nos cenários exercitados. A API foi interceptada com dados fictícios; nenhum dado real foi alterado.

Resultados/capturas: `evidencias/preview-checks.json` e imagens com sufixo `demo`. Falhas iniciais do script de navegador foram corrigidas (escape de seletores e serialização de elementos pelo CDP); a captura `falha-preview-demo.png` é diagnóstico anterior, não evidência da execução final.

Contratos, comandos, segurança, limitações e pendências atuais estão em [Qualidade e conta](qualidade-e-conta.md). Abaixo permanece o registro histórico da primeira etapa visual. Os resultados locais não comprovam cobertura crítica de 90%, regressão completa com banco/serviços ou zero vulnerabilidades: 10 alertas altos ligados a braces continuam registrados.

## Rebuild Docker autorizado em 13/10/2026

Alexia autorizou rebuild do frontend após mudanças visíveis e downloads públicos necessários para este rebuild e os próximos. O serviço já existente publica a porta 3000. A reconstrução usa uma configuração temporária vazia do Docker, sem credenciais salvas, e mantém backend/banco em execução.

Revisão adicional por AST: foram comparadas 242 declarações sem JSX nos arquivos que não tinham alterações prévias; 240 permaneceram equivalentes e as duas diferenças foram `buttonVariants` e `labelVariants`, ambas de apresentação. A revisão não compara arquivos com alterações anteriores nem substitui testes de integração. Campos, validações, cálculos e chamadas de API também foram conferidos nos diffs das telas refatoradas.

Comandos usados na raiz do workspace (PowerShell):

```powershell
$env:DOCKER_CONFIG = Join-Path $env:TEMP 'gradus-flow-docker-sem-credenciais'
New-Item -ItemType Directory -Path $env:DOCKER_CONFIG -Force | Out-Null
docker-compose -f docker-compose-development.yml build frontend
docker-compose -f docker-compose-development.yml up -d --no-deps frontend
```

O executável `docker-compose` é usado porque a configuração vazia do Docker não descobre automaticamente o plugin `compose` instalado pelo Docker Desktop. Não foi alterada a configuração pessoal do Docker.

Resultado: imagem `gestao-tccs-frontend:latest` reconstruída com sucesso (ID curto `13f5075f3aae`), container `gestao-frontend` recriado e iniciado. Backend, Postgres, Garage e Mailpit permaneceram em execução; não foram reconstruídos nem removidos volumes. Acesso anônimo a `http://localhost:3000` respondeu HTTP 200 e contém o formulário de login (`login-email`) e identificação Gradus. Login com conta real e fluxos internos ficam para o teste manual da usuária; nenhuma credencial de aplicação foi usada nesta verificação.

Durante `npm install` no container, o npm reportou 13 vulnerabilidades (1 moderada e 12 altas). Esse resumo deve ser encaminhado à revisão de dependências pela equipe; não identifica, sozinho, exposição real da aplicação. Não foi executado `npm audit fix`, não foram alteradas versões no workspace e não foi marcado o marco de segurança como concluído.

## Evidências locais em 13/10/2026

| Verificação | Resultado | Limite da evidência |
| --- | --- | --- |
| `npm.cmd run lint` | Aprovado, sem avisos ou erros de ESLint | O próprio Next avisa que `next lint` será descontinuado; não foi feita migração de ferramentas nesta entrega. |
| `node node_modules/typescript/bin/tsc --noEmit --incremental false` | Aprovado | Valida tipos, não execução no navegador ou integração com API. |
| `npm.cmd run test:ui` | 9 testes aprovados | Não mede cobertura de toda a aplicação, nem exerce backend, cookies reais ou eventos no navegador. |
| `npm.cmd run build` com mock local de fontes | Bloqueado por dependência nativa do ambiente Windows | `lightningcss.win32-x64-msvc.node` existe, mas não carrega (`ERR_DLOPEN_FAILED`). A mensagem externa do build aparece como `Cannot find module '../lightningcss.win32-x64-msvc.node'` porque o loader tenta um fallback. |
| `npm run build` dentro do Docker Linux | Aprovado: compilação, lint, tipos e geração de 27 páginas | Usa as fontes reais, sem mock. Não substitui validação visual ou dos fluxos autenticados. |
| Recriação do frontend e HTTP em localhost:3000 | Aprovado: container iniciado e login respondeu HTTP 200 | Requisição anônima; não valida autenticação ou fluxos internos. |
| Validação visual no navegador | Pendente | Não há build aprovado nem capturas mobile/desktop nesta sessão. |
| Teste de integração frontend/API | Pendente | Necessita ambiente funcional e dados fictícios de teste. |
| Cobertura mínima de 90% | Não aferida | Não declarar o marco de cobertura concluído com base nos nove testes. |

O binário Lightning CSS importa `VCRUNTIME140.dll`; esse arquivo não foi encontrado em `C:\Windows\System32` durante o diagnóstico. Isso é um indício de dependência de runtime ausente; a causa completa exige validação do ambiente. O módulo nativo do Tailwind Oxide carregou sem erro. Não foram baixadas DLLs, instalados runtimes, reinstaladas dependências ou alterados arquivos do sistema.

O PowerShell bloqueou inicialmente `npm.ps1` pela política de execução. Os comandos foram executados por `npm.cmd`, sem alterar a política da máquina. O arquivo ignorado `next-env.d.ts` foi gerado pelo Next durante suas verificações; ele permite a tipagem dos imports de imagens.

## Executar novamente, sem instalações

Dentro de `frontend/`:

```powershell
$env:NEXT_TELEMETRY_DISABLED = '1'
npm.cmd run lint
node node_modules/typescript/bin/tsc --noEmit --incremental false
npm.cmd run test:ui
```

Para repetir a tentativa de build sem baixar fontes:

```powershell
$env:NEXT_TELEMETRY_DISABLED = '1'
$env:NEXT_FONT_GOOGLE_MOCKED_RESPONSES = Join-Path (Get-Location) 'scripts/font-responses-offline.cjs'
npm.cmd run build
Remove-Item Env:NEXT_FONT_GOOGLE_MOCKED_RESPONSES
```

O mock usa a Rawline existente em `public/` apenas para validação. Ele não comprova o visual da Poppins e não deve ser usado para gerar a entrega de produção. O build normal continua com as fontes originais; seu download depende de autorização para rede ou de um ambiente com cache adequado.

## O que os testes verificam

1. Matriz de atalhos para ADMIN, ADVISOR, COORDINATOR, SUPERVISOR, LIBRARY, BANKING e STUDENT; perfil ausente não exibe atalhos.
2. Discente só recebe Meu TCC e assinaturas se houver trabalho em andamento ou concluído.
3. União de permissões em múltiplos perfis e precedência da regra de assinaturas do discente.
4. Descrições diferentes para dados ainda não carregados, zero pendências e contagens positivas.
5. Texto de apoio compatível com o perfil.
6. Destino de assinaturas e callback de abertura do modal de propostas.
7. Renderização conjunta de HomeShortcuts, CardHome e SectionTitle sem grupos vazios ou atalhos duplicados por viewport.
8. Variante padrão de CollapseCard preservada e estado de expansão exposto por `aria-expanded`.
9. Rótulo de nota associado ao input, entrada decimal obrigatória, valor e limite apresentado.

O teste de renderização integra componentes reais, mas não é uma demonstração de fluxo de ponta a ponta. Os testes de condições leem as declarações da própria página e as executam em contexto isolado, sem reimplementar suas regras. O helper de compilação é exclusivo dos testes.

## Roteiro manual antes de PR

Usar somente contas e documentos fictícios em ambiente de teste. Registrar resultado observado, perfil, viewport e evidência sanitizada. Não marcar automaticamente os itens abaixo como aprovados.

- [ ] Conferir 360 px, 768 px e desktop: títulos, campos, listas, botões e navegação sem cortes ou rolagem horizontal indevida.
- [ ] Navegar por teclado: foco visível, ativação de atalhos, expansão de registros, abas e confirmação de modais.
- [ ] Comparar home de todos os perfis e de uma conta com múltiplos perfis; verificar grupos e contagens.
- [ ] Discente sem proposta, com proposta pendente, recusada, TCC em andamento e TCC concluído: conferir banners e destinos.
- [ ] Abrir propostas recebidas, aceitar/recusar e confirmar cancelamento: conferir retorno dos dados e mensagens existentes.
- [ ] Buscar usuário, editar e paginar; confirmar ações expansíveis no celular e acesso direto no desktop.
- [ ] Cadastrar usuário e proposta com entradas válidas e inválidas: conferir validação, envio e estados de espera.
- [ ] Expandir campus; cadastrar/editar/excluir campus, curso e semestre em ambiente de teste; validar cancelamento dos modais.
- [ ] Consultar TCC em andamento e concluído, etapas, dados dos envolvidos e agendamento.
- [ ] Abrir pendências próprias/todas, visualizar documento e assinar com conta de teste autorizada.
- [ ] Inserir notas de banca nos limites e fora deles; comparar totais e parecer com o comportamento anterior; validar envio.
- [ ] Conferir 403/404, retorno ao início e autenticação aprovada após mudanças nos componentes compartilhados.
- [ ] Capturar antes/depois sem dados reais e validar o padrão visual com a equipe/representante do cliente.

## Condição para considerar pronta

A implementação local está disponível para revisão e o build dentro do Docker foi aprovado. O build direto no Windows permanece bloqueado pelo ambiente. Demonstração, integração, cobertura e evidências remotas permanecem pendentes. A entrega só deve ser marcada Done depois da revisão humana, testes exigidos, pipeline, demonstração e documentação aceitas pela equipe, conforme a definição de pronto do documento acadêmico.
