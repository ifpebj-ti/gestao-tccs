# Histórico e decisões

## 13/10/2026 — escopo autorizado

O pedido inicial anexado limitava a refatoração à home. Alexia ampliou explicitamente o escopo para a área interna do frontend e autorizou a execução por etapas, com componentes reutilizáveis e preservação dos fluxos existentes. Depois solicitou registro permanente das alterações e alinhamento com os marcos da segunda unidade.

Prazo informado por Alexia em 13/10/2026: terminar o projeto antes de 01/12/2026. Planejar implementação, validação, revisão e documentação para estarem concluídas até 30/11/2026.

Autorização adicional em 13/10/2026: reconstruir o frontend no Docker após mudanças visíveis para Alexia testar. Em resposta à confirmação da regra de rede, autorizou downloads de dependências públicas e fontes para este rebuild e os próximos. Continuam proibidos uso de credenciais salvas, operações de escrita no Git e alterações no backend/Compose/Dockerfile. Usar uma configuração vazia do Docker e reconstruir/recriar somente o serviço frontend, sem parar suas dependências ou remover volumes.

Rebuild concluído: `docker-compose -f docker-compose-development.yml build frontend`, seguido de `up -d --no-deps frontend`, com configuração temporária vazia do Docker. Imagem `13f5075f3aae`, frontend iniciado na porta 3000 e formulário de login verificado por HTTP 200. O build real Linux passou com as fontes originais; o problema nativo Windows não impediu a execução no Docker. A restrição a credenciais salvas refere-se ao acesso do assistente à máquina/serviços; a configuração existente da aplicação e o login manual de Alexia foram preservados.

Referência visual: `AuthLayout`, `AuthEditorialPanel`, `AuthNavbar`, login, primeiro acesso e nova senha. O padrão aprovado usa Poppins nos títulos, fundo `#FAF9F5`, verde escuro `#002B18`, verde de interação `#006633`, CTA em `#C8FF47`, linhas finas e poucas superfícies delimitadas. As telas de autenticação aprovadas não foram editadas nesta sessão.

## Situação encontrada antes das edições

Já havia alterações não commitadas nos seguintes arquivos do frontend:

```text
src/app/(auth)/homePage/page.tsx
src/app/(auth)/layout.tsx
src/components/CardHome/index.tsx
src/components/CollapseCard/index.tsx
src/components/Header/index.tsx
src/components/ui/breadcrumb.tsx
src/components/ui/input.tsx
Dockerfile
```

Já existiam como arquivos não rastreados: `src/components/AcademicCapWatermark.tsx` e `src/components/Sidebar/`. Esses arquivos, `Header`, `breadcrumb` e `Dockerfile` não foram editados por esta refatoração. Havia também alterações no backend e em `docker-compose-development.yml`; elas foram preservadas.

Os arquivos compartilhados home/layout/CardHome/CollapseCard/Input receberam mudanças sobre o estado local existente. O diff em relação ao último commit mistura trabalho anterior com esta sessão. Não atribuir automaticamente todas as alterações do working tree à refatoração nem incluí-las indiscriminadamente numa PR. Não foi feita uma cópia integral do estado anterior desses arquivos; a atribuição deve usar este registro e a revisão dos trechos.

## Decisões de implementação

| Decisão | Motivo e comportamento resultante |
| --- | --- |
| Lista única de atalhos na home | Elimina a repetição dos dez atalhos entre mobile e desktop. A página mantém as permissões e passa os dados de apresentação a `HomeShortcuts`. |
| Três grupos de atalhos | Atenção: assinaturas e propostas pendentes. Acompanhamento: em andamento, concluídos e Meu TCC. Gestão: nova proposta, novo usuário, usuários, instituições/cursos e semestres. Grupos vazios não aparecem. |
| Grade de até duas colunas na home | Quatro atalhos ficam em duas linhas de duas colunas; no celular, uma coluna. |
| Variante opcional `flat` | `CardHome` e `CollapseCard` mantêm a variante padrão para consumidores que não optarem pelo novo visual. |
| Apoio textual por perfil | Discente mantém o texto original; administração, orientação/coordenação/supervisão e biblioteca/banca recebem orientações pertinentes às suas atividades. Para múltiplos perfis, o texto prioriza discente, administração, orientação e biblioteca/banca, nessa ordem. As permissões continuam independentes dessa prioridade. |
| Descrição de pendências respeita dados disponíveis | Assinaturas mostram ausência de pendências apenas quando `tccs !== null`. Propostas mostram contagem apenas acima de zero; não foi criado estado de carregamento. |
| Componentes de título e estado | `PageTitle`, `SectionTitle` e `PageState` centralizam hierarquia visual, divisores e feedback de carregamento/ausência de registros. |
| Cartão responsivo de registro | `ResponsiveRecordCard` reutiliza a apresentação de usuários e TCCs sem repetir o mapeamento dos dados. Mantém expansão com ações no celular e acesso direto no desktop. |
| Campos de avaliação compartilhados | `GradeField` elimina a repetição de campos de nota e associa cada rótulo ao input por ID. Cálculos, valores máximos tratados pelos handlers e envio foram preservados. |
| Estados de acesso compartilhados | `AccessPage` reúne apresentação de 403/404, mantendo os redirecionamentos existentes. |
| Testes sem dependências novas | Node Test Runner, React SSR e TypeScript já estavam disponíveis localmente. Os testes não leem cookies reais nem fazem chamadas de API. |

Na home, `checkUserTccStatus`, `fetchAdvisorPendingProposals`, `fetchTccs`, todos os `handle*`, estados, condições `canView`/`isStudent`/`canShowPendingSignatures`, endpoints, modais e blocos de propostas do discente foram preservados. Não foi usado `useStudentTcc`. `isLimitedView` foi removida porque só servia à grade duplicada antiga.

## Inventário dos arquivos alterados nesta sessão

Todos os caminhos abaixo são relativos a `frontend/`.

### Base visual e configuração de testes

```text
package.json — adiciona test:ui; não altera versões de dependências
src/app/globals.css — ajusta tokens de marca, fundo, borda e cores primárias
src/app/(auth)/layout.tsx — envolve a área interna no fundo e cor de texto da marca
src/components/ui/button.tsx — CTA, inversão de cores, foco e transição curta
src/components/ui/input.tsx — campo sublinhado, largura mínima e foco
src/components/ui/label.tsx — tipografia e hierarquia de rótulos
src/components/ui/textarea.tsx — campo plano com foco e estado inválido
src/components/ui/tabs.tsx — abas com linha e estado ativo verde
src/components/CardHome/index.tsx — variante plana com botão semântico
src/components/CollapseCard/index.tsx — variante plana, foco e aria-expanded
src/components/TccTabs/index.tsx — cores e interação da navegação de TCC
src/components/Steps/index.tsx — identidade verde nos indicadores de etapas
```

### Páginas

```text
src/app/(auth)/homePage/page.tsx
src/app/(auth)/users/page.tsx
src/app/(auth)/users/details/page.tsx
src/app/(auth)/newUser/page.tsx
src/app/(auth)/newTCC/page.tsx
src/app/(auth)/instituicoes/page.tsx
src/app/(auth)/semestres/page.tsx
src/app/(auth)/ongoingTCCs/page.tsx
src/app/(auth)/ongoingTCCs/details/page.tsx
src/app/(auth)/ongoingTCCs/signatures/page.tsx
src/app/(auth)/ongoingTCCs/signatures/SignaturesClient.tsx
src/app/(auth)/completedTCCs/page.tsx
src/app/(auth)/pendingSignatures/page.tsx
src/app/(auth)/pendingSignatures/signature/[documentId]/SignatureClient.tsx
src/app/not-found.tsx
src/app/unauthorized/page.tsx
```

Nessas páginas, foram aplicados títulos, campos, divisores, estados e/ou variantes de lista. Usuários, andamento e assinaturas pendentes tiveram listas duplicadas consolidadas. As páginas Meu TCC e detalhes de TCC concluído recebem o padrão por meio dos componentes compartilhados, sem edição direta dos seus arquivos.

### Componentes de TCC e banca

```text
src/components/AvaliacaoForm.tsx
src/components/BankingSignatures.tsx
src/components/tcc-details/ActionPanel.tsx
src/components/tcc-details/AdvisorInfoSection.tsx
src/components/tcc-details/BankingInfoSection.tsx
src/components/tcc-details/ScheduleSection.tsx
src/components/tcc-details/StudentInfoSection.tsx
src/components/tcc-details/TccHeader.tsx
src/components/tcc-details/TccInfoSection.tsx
```

### Arquivos novos

```text
src/components/layout/PageTitle.tsx
src/components/layout/SectionTitle.tsx
src/components/layout/PageState.tsx
src/components/layout/AccessPage.tsx
src/components/home/HomeShortcuts.tsx
src/components/evaluation/GradeField.tsx
src/components/ResponsiveRecordCard.tsx
tests/load-source.cjs
tests/home-shortcuts.test.cjs
tests/grade-field.test.cjs
scripts/font-responses-offline.cjs
docs/refatoracao/README.md
docs/refatoracao/historico.md
docs/refatoracao/validacao.md
docs/refatoracao/issues-e-prs.md
docs/refatoracao/segunda-unidade.md
```

O script de fontes é exclusivo de validação offline; o build normal não o usa. Scripts temporários de edição e leitura local do PDF foram removidos após o uso. Nenhum pacote foi instalado ou atualizado no host. No rebuild Docker posteriormente autorizado, o Dockerfile existente instalou as dependências dentro da imagem Linux.

## Roteiro para explicar em sala

1. Problema: telas internas com linguagens visuais diferentes e repetição entre mobile/desktop.
2. Referência: telas de autenticação já aprovadas, reaproveitadas como padrão visual.
3. Solução: componentes de apresentação pequenos, listas únicas e variantes opcionais.
4. Preservação funcional: regras de perfil, rotas, requisições, cálculos e ações mantidos.
5. Evidências locais: testes de permissões, renderização e campos; lint e TypeScript; build aprovado dentro do Docker Linux, embora bloqueado diretamente no ambiente nativo Windows.
6. Próxima evidência necessária: demonstração no navegador por perfil, integração, cobertura medida, revisão de PR e pipeline real.

## Ampliação autorizada em 13/10/2026

Após a etapa visual, Alexia autorizou navegação por estado do TCC, conta própria, validações, testes adicionais e troca de senha dentro do sistema. Esta etapa inclui mudanças funcionais solicitadas; não deve ser descrita como exclusivamente visual.

Foram adicionados menu/hook de navegação, helpers de sessão/telefone/CPF/data, formulários de conta/senha, schemas e testes. Backend: DTOs e use cases da própria conta/senha, controllers autenticados, métodos específicos de gateway, registros de DI e limitador exclusivo do endpoint de senha. Não houve migração, alteração nos seeds/templates anteriores ou reescrita dos endpoints de TCC/autenticação legados.

Nesta etapa houve atualização local autorizada de dependências, além dos downloads do Docker. Next.js/eslint-config-next 15.5.27 e overrides de sharp/source-map-js foram gravados no manifesto e lockfile. Nenhuma instalação global, credencial salva, commit ou publicação remota foi usada.

Resultados atuais, contratos e limitações: [Qualidade e conta](qualidade-e-conta.md). Títulos, labels, descrições e sugestões de branches/PRs: [Issues para Projects](issues-para-projects.md).

## Revisão visual solicitada após a primeira issue

Continuação em 13/10/2026: [Navegação compacta](menu-compacto.md) e [TCC e documentos assinados](tcc-e-arquivos.md). A segunda continuação inclui a exceção de backend autorizada pela usuária para controle de acesso e downloads da pasta, restrita a documentos com assinaturas registradas.

Alexia pediu lateral clara com item ativo em verde escuro, redução de atalhos/textos duplicados, home da estudante somente com pendências e troca de senha oculta até abrir pelo card. Detalhes, inventário e evidências estão em [Design da aluna](design-aluna.md). Capturas anteriores foram preservadas; a nova execução usa uma subpasta própria. Backend e lógica dos fluxos não foram modificados nesta revisão.

## Proposta em grupo com testes — continuação em 13/10/2026

Alexia confirmou a implementação do convite por e-mail a estudante cadastrado e reforçou a criação e execução dos testes. Foram adicionados consentimento separado do vínculo ao TCC, aceite/recusa na home, acompanhamento de estados, bloqueio da avaliação, notificação de convite, migração, transação PostgreSQL e revalidações de permissões/conflitos. Cancelamento e reformulação tratam os convites; uma recusa cancela a composição proposta.

Validação: 67 testes frontend, lint, TypeScript, build Linux; 275 testes backend com cinco casos de PostgreSQL real descartável; 270 testes no .NET 8 sem rede e cinco casos de banco ignorados nesse container; oito verificações no navegador com API interceptada. O banco temporário encontrou uma consulta EF que falhava por ciclo de navegação, corrigida antes da execução final. Foi removida da migração gerada uma exclusão de coluna não relacionada à funcionalidade, preservando dados anteriores.

Contratos, inventário conceitual, evidências e limitações: [Proposta em grupo](proposta-em-grupo.md). Mantida a regra de trabalho local, sem commit ou publicação remota.

A pedido da usuária, frontend e backend foram recompilados e reiniciados no Docker local. Conferência: frontend 200 na porta 3000; rota de convites protegida por autenticação na porta 8080; Swagger com as novas rotas e contrato. Containers de teste encerrados; aplicação disponível para teste manual no localhost.

## Esclarecimento do fluxo e padrão dos seletores — 13/10/2026

A usuária corrigiu a interpretação: integrantes devem ser adicionados pelo e-mail, inclusive antes de terem conta. A proposta aguarda todos os aceites para chegar ao professor; uma recusa notifica o solicitante para submeter novamente. Foram adicionados o vínculo de convite por e-mail, `AWAITING_PARTICIPANTS`, notificação persistente e migração incremental que preserva convites anteriores. O cadastro verificado existente foi exercitado no teste integrado e mantém consentimento separado do cadastro.

Também foram corrigidos o espaço extra abaixo da descrição, os integrantes repetíveis e os 12 seletores do sistema. `FormSelect`/`PatternSelect` compartilham a opção marcada em `#183E2D`, teclado e controle de foco, preservando os valores e conversões dos formulários.

Resultados: 283 testes backend, dez deles com PostgreSQL real isolado; 72 testes frontend; lint, TypeScript e build; 17 verificações de navegador. Evidências e limitações em [Proposta em grupo](proposta-em-grupo.md). A implementação continua local, sem commit ou publicação remota.

## Ajustes visuais de orientação e assinaturas — 14/10/2026

A home dos perfis de orientação deixou de repetir acompanhamento e passa a mostrar cards verdes somente com pendências positivas. Propostas recebidas ganhou espaçamento alinhado ao layout e cards mais compactos. O componente Button centraliza cancelar/recusar/excluir em vermelho com texto branco, hover e pressionado mais escuros; foram migradas ações e ícones nas telas.

A assinatura compartilhada usa título menor, nome do documento separado, botões do sistema e aviso verde NoticeCard. Há um único visualizador responsivo. O envio só é liberado após download bem-sucedido e o foco do agendamento foi preservado no Anexo VIII. Validado com 81 testes frontend, TypeScript, ESLint, build e 11 verificações no Chrome com APIs fictícias. Evidências e detalhes: [ajustes visuais](ajustes-visuais-professor.md). Trabalho local, sem commit ou publicação remota.
