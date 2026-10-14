# Issues para copiar no GitHub Projects

Rascunhos locais de 13/10/2026. Os labels abaixo são sugestões; conferir os nomes existentes no repositório. As implementações estão no working tree, sem commits ou branches criadas nesta sessão. Não marcar Done antes da revisão, pipeline e demonstração exigidas pela equipe. Planejar conclusão até 30/11/2026.

## 1. Padronizar as telas internas e extrair componentes compartilhados

**Labels:** `frontend`, `refactor`, `ui/ux`, `segunda-unidade`

**Branch sugerida:** `refactor/frontend-interface`

**Descrição para copiar:**

As telas internas usam estilos diferentes e repetem estruturas entre desktop e mobile. Padronizar a interface com a identidade visual aprovada no fluxo de autenticação e reduzir a duplicação com componentes compartilhados.

Escopo: títulos, seções, carregamento/ausência de registros, cartões de atalhos, listas responsivas, abas e campos de notas. Preservar ações, cálculos, informações necessárias e contratos existentes.

Revisão visual solicitada após a criação desta issue: lateral clara com destaque verde escuro na página ativa, remoção de atalhos/textos repetidos, home da estudante somente com pendências e senha oculta até abrir pelo card. Registro e evidências em `design-aluna.md` e `evidencias/design-aluna`. O backend permaneceu inalterado nessa revisão.

Critérios de aceite:
- [ ] Componentes compartilhados usados nas telas internas, com variantes compatíveis onde necessário.
- [ ] Interface validada em desktop, tablet e celular, com teclado e foco visível.
- [ ] Fluxos anteriores preservados e revisados pela equipe.
- [ ] Testes de componentes, lint, TypeScript e build aprovados.
- [ ] Evidências visuais sanitizadas e documentação vinculadas ao PR.

**Título sugerido do PR:** `refactor(frontend): padroniza telas internas e componentes compartilhados`

## 2. Corrigir a navegação lateral conforme o perfil e o estado do TCC

**Labels:** `frontend`, `bug`, `ui/ux`, `segunda-unidade`

**Branch sugerida:** `fix/navigation-tcc-status`

**Descrição para copiar:**

A navegação pode indicar mais de um item ativo e manter a proposta disponível para o estudante que já possui TCC aprovado. Ajustar os destinos e estados visuais conforme o estado real do trabalho.

Escopo: alinhamento da marca com a barra lateral, indicação única da rota ativa, transição imediata da seleção e troca de Proposta por Meu TCC. Diferenciar carregamento, falha, ausência, andamento e conclusão. Preservar usuários com múltiplos perfis.

Critérios de aceite:
- [ ] Estudante com TCC aprovado/concluído recebe Meu TCC com o ID correto.
- [ ] Proposta não aparece durante carregamento/falha nem quando há TCC aprovado.
- [ ] Falha ao consultar o estado não libera o formulário de proposta.
- [ ] Apenas uma opção fica ativa, inclusive após voltar no navegador.
- [ ] Matriz de perfis, testes de navegação e validação mobile aprovadas.

**Título sugerido do PR:** `fix(frontend): corrige navegação e acesso à proposta conforme estado do TCC`

## 3. Permitir ao estudante corrigir os dados da própria conta

**Labels:** `frontend`, `backend`, `enhancement`, `security`, `segunda-unidade`

**Branch sugerida:** `feat/student-self-profile`

**Descrição para copiar:**

O estudante precisa corrigir dados digitados no cadastro sem depender da gestão para toda alteração. Criar Minha conta com edição de nome, telefone, CPF, matrícula, turma e turno. O e-mail é fixo; campus, curso, permissões e status permanecem sob controle da gestão. Outros perfis podem atualizar apenas o telefone.

Escopo: formulário em componente próprio, validação compartilhada no frontend e API autenticada da própria conta. A identidade vem exclusivamente do JWT validado pelo backend. Atualizar somente as colunas autorizadas, sem sobrescrever alterações administrativas.

Critérios de aceite:
- [ ] Aluno edita somente os seis campos autorizados; outros perfis editam apenas telefone.
- [ ] E-mail, campus, curso, perfis, status e senha não são aceitos no payload desse fluxo.
- [ ] CPF valida formato/dígitos; telefone, textos e turno respeitam limites.
- [ ] Nome corrigido aparece na navegação sem mudar o JWT ou permissões.
- [ ] Falhas preservam o preenchimento e permitem nova tentativa; descarte restaura os valores.
- [ ] Testes HTTP comprovam autenticação, identidade própria, recusa de campos extras e proteção contra mudança concorrente de perfil.
- [ ] CPF é consultável apenas na própria conta do estudante e não entra em logs ou evidências reais.

**Título sugerido do PR:** `feat(profile): permite edição segura dos dados próprios do estudante`

## 4. Validar entradas antes do envio dos formulários internos

**Labels:** `frontend`, `bug`, `validation`, `segunda-unidade`

**Branch sugerida:** `fix/frontend-form-validation`

**Descrição para copiar:**

Alguns formulários aceitam textos só com espaços, telefones inválidos ou datas inconsistentes. No cadastro de semestre, datas vazias podem causar erro antes do envio. Corrigir essas validações e apresentar mensagens claras.

Escopo: propostas de estudante/gestão, edição de usuário/TCC, cancelamento, campus, curso, semestre, agendamento e limites de nota. Manter endpoints, payloads e cálculos existentes. Gestão exige de um a quatro estudantes válidos, curso selecionado e ausência de duplicação.

Critérios de aceite:
- [ ] Entradas obrigatórias vazias ou só com espaços não são enviadas.
- [ ] Limites dos campos respeitam os contratos e o banco existentes.
- [ ] Datas impossíveis/intervalos invertidos e horários inválidos são rejeitados.
- [ ] Erros são associados aos campos compartilhados para leitores de tela.
- [ ] Testes cobrem valores válidos, inválidos e limites; navegador comprova ausência de envio inválido nos cenários exercitados.

**Título sugerido do PR:** `fix(frontend): valida formulários e impede submissões inválidas`

## 5. Alterar senha dentro da área autenticada com confirmação da senha atual

**Labels:** `frontend`, `backend`, `enhancement`, `security`, `segunda-unidade`

**Branch sugerida:** `feat/account-password-change`

**Descrição para copiar:**

Usuários autenticados são encaminhados ao fluxo externo de recuperação ao escolher Alterar senha. Disponibilizar a troca dentro de Minha conta, confirmando a senha atual. Manter a recuperação externa para quem esqueceu a senha.

Escopo: formulário de senha atual/nova/confirmação e endpoint autenticado da própria conta. Limitar tentativas, usar bcrypt, validar o limite de bytes e atualizar somente o hash se a conta/senha anterior ainda forem as mesmas. Após sucesso, limpar a sessão local e solicitar novo login.

Critérios de aceite:
- [ ] Menu e alertas internos levam à seção Alterar senha de Minha conta.
- [ ] Senha atual obrigatória, nova senha diferente com mínimo de 8 caracteres, confirmação e limite de 72 bytes UTF-8.
- [ ] API exige autenticação e verifica a senha atual; cliente não escolhe ID/e-mail.
- [ ] Limite de cinco tentativas por minuto por usuário, resposta 429 tratada no frontend.
- [ ] Senhas e hashes não são retornados, registrados em logs nem colocados em evidências.
- [ ] Testes com bcrypt real e integração HTTP aprovados; falha concorrente não sobrescreve senha.
- [ ] Sessão local encerrada após sucesso; documentado que JWTs já emitidos para outras sessões continuam válidos até expirar.

**Título sugerido do PR:** `feat(account): adiciona troca de senha autenticada em Minha conta`

## 6. Ampliar a regressão automatizada e registrar evidências por perfil

**Labels:** `frontend`, `backend`, `tests`, `documentation`, `segunda-unidade`

**Branch sugerida:** `test/frontend-regression`

**Descrição para copiar:**

A compilação e a resposta HTTP do site não comprovam seus fluxos. Ampliar a estratégia de testes com matriz de perfis, validações de formulário e interações no navegador, registrando limites e resultados reproduzíveis.

Escopo: suites locais de componentes/schemas/middleware, integração HTTP da própria conta, navegador isolado com dados fictícios, roteiro manual e documentação. Os testes de cada funcionalidade devem acompanhar o respectivo PR; esta issue consolida a estratégia e a evidência.

Critérios de aceite:
- [ ] Matriz dos sete perfis e de usuários com múltiplos perfis documentada/testada.
- [ ] Cenários de sucesso, erro, carregamento, entradas inválidas e limites relevantes testados.
- [ ] Navegador isolado não usa contas reais nem escreve no banco.
- [ ] Resultados e capturas sanitizadas vinculados aos PRs pela equipe autorizada.
- [ ] Cobertura crítica medida e comparada à meta de 90%; quantidade de testes não usada como substituto da cobertura.
- [ ] Integração com banco/serviços e validação manual por perfil executadas no ambiente de teste.

**Título sugerido do PR:** `test(frontend): amplia regressão de formulários, perfis e navegação`

## 7. Atualizar dependências vulneráveis e tratar a pendência de braces

**Labels:** `dependencies`, `security`, `frontend`, `segunda-unidade`

**Branch sugerida:** `chore/frontend-security-dependencies`

**Descrição para copiar:**

O relatório público do npm identificou 13 ocorrências: 12 altas e uma moderada. Atualizar dependências com versões corrigidas compatíveis e registrar a pendência sem aplicar migrações automáticas forçadas.

Implementação local: Next.js e eslint-config-next alinhados em 15.5.27, sharp e source-map-js atualizados por overrides. O relatório posterior contém 10 ocorrências altas derivadas de braces na cadeia das ferramentas de desenvolvimento. O registro não disponibilizou braces 3.0.4; o downgrade automático do eslint-config-next não foi aplicado.

Critérios de aceite:
- [ ] Manifesto e lockfile consistentes; testes, lint, TypeScript e build aprovados.
- [ ] Relatórios anterior/posterior preservados, sem credenciais.
- [ ] Tratamento de braces revisado com responsáveis de segurança/DevSecOps.
- [ ] Resultado final atende ao marco de zero vulnerabilidades altas/críticas, ou pendência formalmente registrada para resolução antes da entrega.

**Título sugerido do PR:** `chore(deps): atualiza dependências do frontend e registra pendência de segurança`

## Publicação e PRs para develop

Criar as issues como tarefas do marco da segunda unidade e vinculá-las ao Projects. Sugestão de estados: Todo → In progress → In review → Done, conforme o fluxo já adotado pela equipe.

Para cada PR, informar problema, comportamento resultante, escopo, testes executados, evidências sanitizadas e limitações. Usar o número real da issue para `Closes #N` somente quando os critérios dessa issue forem atendidos. Destino previsto: `develop`.

Ordem sugerida: componentes visuais → navegação → conta própria → validações → troca de senha → consolidação de testes/evidências. Dependências podem seguir em PR separado, coordenado com as verificações.

Como as mudanças estão misturadas no working tree e alguns arquivos atendem a várias issues, não copiar a versão final inteira de cada arquivo para todas as branches. Separar os trechos correspondentes e manter os testes da funcionalidade junto dela. Ajustar dependências entre PRs ou agrupar alterações inseparáveis com justificativa. Preservar os edits anteriores da equipe, especialmente templates, seeds, Compose e Dockerfile.

Branches só devem ser criadas/publicadas pela usuária após organizar e revisar o trabalho local. Nenhuma dessas operações foi executada pelo assistente.

## Backlog identificado nesta revisão

- Medir cobertura crítica e executar regressão com banco e serviços fictícios.
- Revisar o fluxo legado de recuperação de senha: comprovação do código deve estar vinculada à própria requisição/sessão e ser consumida atomicamente. A nova troca autenticada não resolve esse fluxo externo.
- Avaliar revogação de JWTs de outras sessões após troca de senha e limitação distribuída de tentativas em ambientes com várias instâncias.
- Separar composições específicas de cada perfil mantendo componentes compartilhados, depois da estabilização atual.

Esses itens não foram implementados nem encerrados. A autorização de backend desta etapa cobre a própria conta e sua troca de senha, não uma reescrita geral da autenticação.
