# Validação e edição da própria conta — 13/10/2026

## Escopo autorizado

Alexia autorizou ampliar os testes, corrigir validações e criar o endpoint de edição da própria conta. Depois da versão inicial com telefone, autorizou nome, telefone, CPF, matrícula, turma e turno para o estudante. Confirmou que o e-mail é fixo. Campus, curso, perfis, status, senha e SIAPE não são editáveis por esse fluxo. Para os demais perfis, a edição continua limitada ao telefone.

O frontend não foi separado em aplicações por perfil nesta etapa. Componentes e testes compartilhados mantêm a união de permissões para usuários com vários perfis. Login com Google foi discutido, mas não implementado.

## Mudanças e regras

- `StudentProfileForm` concentra o formulário do aluno; `studentProfileSchema` concentra sua validação. O hook da conta compartilha tratamento de consulta, envio, falha e recuperação. O nome exibido na navegação é atualizado após a resposta da API, sem reescrever o JWT.
- A proposta discente mantém seu payload. A proposta da gestão exige de um a quatro estudantes, e-mail válido, curso positivo e ausência de duplicação. Título e resumo rejeitam espaços e mantêm o limite de 255 caracteres.
- Edição de usuário valida o telefone; edição de TCC e cancelamento rejeitam textos só com espaços. Campus e curso validam campos obrigatórios e limites existentes no banco. Semestres rejeitam datas inválidas ou invertidas antes da conversão ISO. Agendamento valida data, hora e local.
- Campos compartilhados associam mensagens ao input e anunciam erros. Notas mantêm os cálculos e recebem `min` e `max` nativos.
- A navegação diferencia carregamento, erro, ausência, andamento e conclusão do TCC. Falha na consulta não libera o formulário da proposta. A marca ocupa a coluna da barra lateral; apenas um item fica ativo.
- Middleware rejeita sessão malformada, expirada, sem identidade positiva ou sem perfil conhecido. Instituições passou a ter proteção explícita de rota para ADMIN, coerente com a gestão disponível nessa tela. As outras permissões existentes foram preservadas.

## Contrato da API

| Requisição | Uso | Proteção |
| --- | --- | --- |
| GET `/api/User/me` | Consultar a própria conta | Identidade exclusivamente do JWT validado pela API; CPF retornado apenas para estudante, para edição própria |
| PUT `/api/User/me` | Atualizar somente telefone | Campos adicionais recusados; ID recebido por query ignorado |
| PATCH `/api/User/me` | Atualizar os seis campos autorizados do estudante | Papel STUDENT na autenticação e no registro atual; campos adicionais recusados; validação de CPF, telefone, textos e turno |
| POST `/api/User/me/password` | Alterar senha dentro de Minha conta | JWT, senha atual verificada com bcrypt, conta ACTIVE, cinco tentativas/minuto por usuário, payload limitado e atualização condicional do hash |

O gateway atualiza somente as colunas permitidas. Não sobrescreve e-mail, campus, curso, senha ou permissões. Na atualização do estudante, a condição do SQL também exige que ele ainda tenha STUDENT. Alteração concorrente de perfil/remoção da conta retorna conflito, sem anunciar sucesso.

CPF é dado pessoal: aparece somente na consulta da própria conta do estudante. Não é incluído em logs nem usado como prova de identidade. A validação dos dígitos verificadores comprova o formato, não a titularidade. Os testes usam valores exclusivamente fictícios.

## Verificações executadas

- Frontend: 55 testes automatizados aprovados, incluindo schemas, limites, entradas inválidas, navegação, componentes e matriz de rotas para os sete perfis e múltiplos perfis.
- ESLint aprovado; TypeScript `--noEmit` aprovado.
- Backend: 202 testes aprovados, dos quais 60 cobrem a própria conta e senha. Integração HTTP usa controller, binding, autorização e limitação de tentativas reais em host isolado, com autenticação e gateway de teste; não acessa banco real nem inicia os seeds da aplicação. Testes da senha também exercitam bcrypt real.
- O host possui .NET 10; a primeira execução net8 usou `DOTNET_ROLL_FORWARD=Major`. Também passaram os mesmos 202 testes no runtime nativo .NET 8, em container temporário sem rede e sem banco real. O restore offline precisou usar `/root/.nuget/packages` como fonte local para resolver a referência flutuante existente `AWSSDK.S3 3.7.*`; o manifesto backend não foi alterado.
- Rebuild frontend/backend aprovado e serviços iniciados. Chrome isolado: 15 verificações aprovadas, com resultados em `evidencias/preview-checks.json`; imagens demo inspecionadas em desktop/mobile. localhost:3000 HTTP 200; API local recusou token fictício inválido com 401 na conta e na alteração de senha.

Comandos locais:

```powershell
# Em frontend/
npm.cmd run test:ui
npm.cmd run lint
.\node_modules\.bin\tsc.cmd --noEmit
# Com Chrome isolado em depuração local 9227 e o site iniciado:
npm.cmd run test:browser

# Em backend/
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet test gestaotcc.Test/gestaotcc.Test.csproj --no-restore
```

Validação nativa .NET 8, a partir da etapa `build` do Dockerfile existente:

```powershell
$env:DOCKER_CONFIG = Join-Path $env:TEMP 'gradus-flow-docker-sem-credenciais'
docker build --target build --tag gradus-local-dotnet-tests backend
docker run --rm --network none gradus-local-dotnet-tests sh -c 'dotnet restore gestaotcc.Test/gestaotcc.Test.csproj --configfile NuGet.Public.config --source /root/.nuget/packages -p:NuGetAudit=false --verbosity quiet && dotnet test gestaotcc.Test/gestaotcc.Test.csproj --no-restore --verbosity quiet -p:WarningLevel=0 -p:NoWarn=MSB3277'
```

O restore sem rede contra a URL pública falhou por causa da resolução da versão flutuante; a execução usando o cache local como feed passou. Os avisos de nulabilidade/XML existentes e a mistura de versões EF nas referências de teste permanecem para revisão própria; não foi afirmado que o backend compila sem avisos.

O teste de navegador intercepta a API e bloqueia solicitações externas. Ele valida a interação real do frontend compilado, mas não comprova integração com banco, e-mail, armazenamento ou assinatura Gov.br. Capturas com sufixo `demo` contêm dados fictícios.

## Dependências e limitações

Consulta pública do npm autorizada, com configurações de usuário/global vazias. Nenhuma credencial salva foi utilizada. O relatório inicial está em `npm-audit.json`; o posterior, em `npm-audit-after.json`.

Next.js e eslint-config-next foram alinhados em 15.5.27; sharp e source-map-js receberam versões corrigidas. O relatório caiu de 13 ocorrências (12 altas e uma moderada) para 10 altas, todas relacionadas à cadeia `braces` → `micromatch` → `fast-glob` nas ferramentas de desenvolvimento. O registro não disponibilizou `braces@3.0.4`. A sugestão automática de reduzir eslint-config-next para 14 não foi aplicada. Não houve `audit fix --force`.

Essa pendência continua aberta e impede declarar atendido o marco de zero vulnerabilidades altas/críticas. A redução de ocorrências não equivale a uma auditoria completa de segurança.

Ainda precisam de evidência: cobertura medida dos fluxos críticos (meta acadêmica de 90%), matriz completa de interações por perfil, fluxos integrados com banco/serviços em ambiente com dados fictícios, expiração e assinatura de JWT contra a configuração real da API, regressão de documentos e assinaturas, revisão humana e pipeline da equipe. Alterações em validação/backend além da própria conta exigem alinhamento com os responsáveis desses módulos.

Troca de senha: a nova seção fica dentro de Minha conta; a recuperação externa permanece para quem esqueceu a senha. A nova senha deve ter mínimo de oito caracteres, ser diferente da atual e caber no limite de 72 bytes UTF-8 do bcrypt. O frontend limpa os campos e o cookie e solicita novo login após sucesso. Isso encerra apenas a sessão local: outros JWTs já emitidos continuam válidos até expirar. A limitação de tentativas é local à instância; revogação geral e limitação distribuída são melhorias separadas. Não foi alterado o fluxo legado de recuperação.

## Rascunhos locais para revisão

1. Conta: edição dos dados próprios do estudante, e-mail fixo e proteção contra alteração de permissões.
2. Validação: impedir submissões vazias/inválidas em propostas, gestão e agendamento.
3. Testes: matriz de perfis, schemas e interações com falhas de API.
4. Dependências: tratar a pendência de braces e registrar o relatório final.

Nenhum commit, PR, issue ou alteração remota foi criado. Arquivos anteriores da equipe foram preservados. Prazo de planejamento: até 30/11/2026.
