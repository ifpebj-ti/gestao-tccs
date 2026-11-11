# Tarefas de segurança — apresentação de 11/11/2026

Implementação realizada em 10/11/2026. Responsabilidade sugerida: DevSecOps, com revisão de backend/frontend. Os itens abaixo estão implementados localmente e podem ser usados como descrições de issues; nenhuma issue foi aberta automaticamente no GitHub. Não representam certificação de segurança ou implantação em produção.

## Tasks implementadas

| ID | Título para a task | Problema corrigido | Critérios de aceite e evidências |
| --- | --- | --- | --- |
| SEG-01 | Exigir comprovação protegida para redefinir senha | Após validar o código, a API aceitava e-mail e senha nova sem comprovar que o solicitante participou da validação | Sem cookie protegido ou com cookie falsificado: 401. Cookie HttpOnly, Secure fora de Development, SameSite=Strict, caminho restrito e duração de até 5 minutos. Testes `KnowingEmailAndVerifiedDatabaseStateDoesNotAuthorizePasswordReset` e `ResetProofIsHttpOnlySecureStrictAndCannotBeForgedOrUsedForAnotherAccount` |
| SEG-02 | Consumir códigos de senha e convite de forma atômica | Marcadores e atualização de senha não formavam uma operação única; o convite de nova senha não era consumido nessa operação | PostgreSQL permite um único vencedor em solicitações simultâneas. Falha na comparação da senha anterior desfaz o consumo do código. Testes `ConcurrentPasswordResetsConsumeTheCodeOnlyOnce`, `ConcurrentFirstAccessRequestsCannotReuseTheInvitation` e `FailedPasswordCompareRollsBackCodeConsumption` |
| SEG-03 | Impedir upload em TCC de outro usuário ou curso | O upload inicial recebia o ID do TCC sem verificar vínculo; gestão podia enviar versão final fora do curso | Discente/orientador precisam de vínculo válido; coordenação/supervisão precisam do mesmo curso; ADMIN mantém acesso global. Conta inativa não envia. Testes `StudentCannotOverwriteAnotherStudentsTcc`, `CoordinatorCannotWriteOutsideTheirCourseButAdminCan` e `LinkedStudentCanStillUploadAValidPdf` |
| SEG-04 | Restringir uploads de monografia e versão final | Extensão e MIME não eram suficientes para verificar conteúdo; leitura do arquivo podia alocar memória antes de um limite explícito | Limite de 20 MiB por arquivo e limite multipart com margem de 64 KiB. Exigir extensão PDF, MIME application/pdf, assinatura %PDF- e marcador final. Gerar nome de armazenamento no servidor. Testes `PdfPolicyRejectsRenamedHtmlWrongMimeAndTruncatedFiles` e `OversizedUploadIsRejectedBeforeReadingFileIntoMemory` |
| SEG-05 | Limitar tentativas nos endpoints públicos sensíveis | Login, códigos e recuperação não tinham limite específico de frequência | Limite compartilhado de 10 solicitações por minuto/IP para autenticação pública, códigos e avaliação externa; sem fila; 429 com Retry-After. Requests de autenticação/código limitados a 16 KiB. Header X-Forwarded-For arbitrário não troca o IP usado. Teste `PublicAuthenticationThrottlesAndDoesNotTrustSpoofedForwardedHeaders` |
| SEG-06 | Endurecer JWT e revogar sessões após alterações de conta | Emissor/destinatário não eram validados; bloqueio ou troca de senha/perfil não revogava imediatamente o token | Assinatura HS256, validade, issuer e audience obrigatórios. Chave mínima de 32 bytes; rejeição da chave pública antiga de exemplo. Preservar a unidade de horas da configuração, com máximo de 8 horas. A cada requisição autenticada, conferir conta ativa, perfis atuais e comprovante HMAC vinculado à senha atual. Testes `JwtRequiresSignatureExpirationIssuerAudienceAndApprovedAlgorithm`, `JwtConfigurationRejectsShortSecrets`, `PasswordRoleAndStatusChangesRevokePreviouslyIssuedSessions` e `TokenLifetimeKeepsTheExistingHoursConfiguration` |
| SEG-07 | Impedir vazamento de detalhes internos em erros | A API retornava classe da exceção e mensagem interna; erros de FluentValidation eram tratados como 500 | Erro interno retorna ProblemDetails genérico e traceId; validação retorna 400. Logs do handler guardam tipo/status/identificador sem serializar exceção bruta. Falhas de e-mail dos fluxos alterados retornam mensagem pública genérica. Testes `InternalExceptionsNeverExposeInfrastructureMessages` e `InvalidInputFromFluentValidationIsAClientError` |
| SEG-08 | Proteger geração, transporte e logs de códigos/tokens | Havia Random() para códigos e logs com segredos; login de banca enviava token na query da API | Usar RandomNumberGenerator nos quatro geradores alterados. Remover os códigos/tokens dos logs específicos de criação, verificação, convite, login e avaliação. Banking login recebe JSON no corpo do POST, com cliente atualizado. Evidência: diff dos geradores, use cases e AvaliacaoForm |
| SEG-09 | Aplicar cabeçalhos defensivos e CORS restrito | Frontend sem proteção contra enquadramento, sniffing e referências; origens locais eram permitidas também em produção | X-Frame-Options=DENY, nosniff, no-referrer, Permissions-Policy e CSP inicial no frontend; remover X-Powered-By. API aplica headers antes de Swagger/erros e desabilita Server do Kestrel. CORS produtivo permite só a origem configurada. Teste `ProductionCorsAllowsOnlyConfiguredOrigin`; build Linux e verificação HTTP do frontend |
| SEG-10 | Eliminar segredo padrão e reduzir superfície local | O .env local reutilizava a chave JWT pública de exemplo; serviços de desenvolvimento publicavam portas em todas as interfaces | Chave local substituída por valor aleatório de 48 bytes sem exibi-lo. .env permanece ignorado pelo Git; exemplo não fornece chave pronta. Compose de desenvolvimento publica somente em 127.0.0.1. Dashboard Hangfire deixa de ser exposto; jobs continuam cadastrados. Evidências: validação do .env sem revelar valores e diff de Compose/Hangfire |
| SEG-11 | Impedir execução de scripts no visualizador HTML de documentos | O iframe combinava allow-same-origin e allow-scripts; o código de captura manipulava o HTML de textareas | Remover allow-scripts, mantendo leitura dos campos pelo formulário pai. Capturar conteúdo de textarea com textContent. Templates atuais não contêm scripts. Validar os testes existentes de assinatura e o build Linux; a CSP completa continua como tarefa separada |
| SEG-12 | Impedir busca de recursos externos na conversão de PDF | HTML fornecido ao conversor podia referenciar URLs de rede e arquivos do servidor | ResourceRetriever rejeita todos os URLs de rede/arquivo; manter imagens data-URI dos modelos. Limitar HTML de conversão a 2 MiB. Testes `ResourceRetrieverNeverReadsUrls`, `HtmlConversionDoesNotRequestReferencedNetworkResources` e `EmbeddedImagesRemainAvailableWithoutNetworkAccess` |

## Validação registrada

- Backend .NET 8 em contêiner, com PostgreSQL temporário: **572 testes aprovados, zero falhas, zero ignorados**, após integrar os dois novos lotes de testes da develop. Cobertura global de sequências: **95,89%**, sem exclusões adicionais. O banco utilizado é exclusivamente `gradus_proposal_test`, em esquema temporário, sem usar dados da aplicação. Comparação e limites da medição em [integração e cobertura](integracao-develop-cobertura.md).
- Frontend: suíte existente com **107 testes aprovados**; TypeScript sem erros. Build Linux e verificação de resposta HTTP real com os cabeçalhos defensivos aprovados. O mesmo smoke está em `scripts/verify-security-headers.cjs` e foi adicionado ao CI.
- Os testes da colega foram preservados; a integração acrescenta regressões dos controles de segurança e testes HTTP, de persistência e armazenamento. A esteira passa a exigir 95% global de sequências do backend.
- Compatibilidade: `ValidRecoveryStillWorksOverHttpWithCorsAndTheProtectedCookie` exercita controllers e JSON reais por HTTP, com gateways controlados: recusa sem comprovação, verificação do código, transporte do cookie/CORS, troca válida, limpeza do cookie, recusa de repetição e login de banca pelo novo corpo JSON. Os testes PostgreSQL verificam separadamente a persistência e a concorrência reais.
- A duração da sessão mantém a unidade original de horas. O teste `TokenLifetimeKeepsTheExistingHoursConfiguration` evita mudar silenciosamente essa unidade; autenticação de sessões ainda válidas continua retornando 200. Alteração de curso/campus também exige renovar o token, para não preservar escopo antigo.
- Nenhuma imagem foi publicada, nenhuma release criada e nenhum deploy OCI acionado. Os arquivos locais ainda precisam de PR, revisão e execução da esteira no GitHub.

Para reproduzir os testes da aplicação:

```text
dotnet test backend/gestaotcc.Test/gestaotcc.Test.csproj --configuration Release
cd frontend
npm run test:ui
npm run lint
npx tsc --noEmit --incremental false
```

Os testes PostgreSQL exigem o banco descartável indicado em `GRADUS_PROPOSAL_TEST_DATABASE`, conforme o job backend-ci; sem essa variável eles são ignorados. Não apontar essa variável para o banco de trabalho ou de produção.

## Compatibilidade e ativação

### Organização das branches

- `feat/melhora-esteira-ci-cd`: esteira, manutenção de dependências e documentação de CI/CD, baseada na develop em `9074001`.
- `feat/reforca-seguranca-aplicacao`: derivada da branch de CI/CD, com a implementação de segurança, o smoke HTTP e a documentação da aplicação em commits separados.

Aguardar as alterações restantes da integrante responsável pelos testes antes de integrar. A ordem preparada é CI/CD primeiro e segurança da aplicação depois. Enquanto a primeira não for integrada, uma eventual PR da aplicação deve usar a branch de CI/CD como base para mostrar somente o trabalho adicional. Antes de integrar à develop, atualizar e validar novamente caso a base tenha avançado. Nenhum merge é realizado pela criação dessas branches.

1. As mudanças precisam ser compiladas e os contêineres recriados para entrarem em vigor. **Os contêineres locais que já estavam rodando não foram atualizados nesta tarefa.** A chave nova está no .env; a instância antiga ainda usa a configuração que recebeu quando foi criada. A rotação efetiva exige reinício/recriação com a configuração nova.
2. Tokens antigos deixam de ser aceitos no backend atualizado: fazer login novamente. `ACCESSTOKEN_EXPIRATION_TIME` **continua representando horas**, com limite de 8 horas; configuração ausente/inválida usa 1 hora. Isso evita converter silenciosamente a configuração existente de 8 horas em 8 minutos, sem um fluxo de renovação de sessão. Os defaults de issuer/audience são `gradusflow` e `gradusflow.web`. Uma política futura de tokens curtos deve vir acompanhada de renovação e proteção contra perda de formulários em edição.
3. A recuperação precisa ocorrer no mesmo navegador. Frontend e API devem usar HTTPS e o mesmo site, preferencialmente pelo proxy da aplicação. Em desenvolvimento usar localhost em ambos, ou 127.0.0.1 em ambos; misturar esses hosts impede o envio do cookie SameSite=Strict. Os fetches de verificação/alteração agora usam credentials=include.
4. A origem produtiva deve estar em `CORS_SETTINGS__URL_FRONT`, sem path. Não há permissão automática para localhost fora de Development. Em múltiplas instâncias, compartilhar de forma protegida o keyring do ASP.NET Data Protection; sem isso, o comprovante pode ser invalidado por reinícios/troca de instância e exigir novo código.
5. O comprovante HttpOnly é separado do marcador `access_token_temp` usado pela interface. Forjar o marcador da interface não autoriza o backend.
6. O cliente de banca foi atualizado para POST com corpo `{ "token": "..." }`. Integrações externas que chamem o contrato antigo com query precisam ser adaptadas.
7. A validação atual de JWT consulta a conta no banco por requisição autenticada, para evitar autorização com senha, status ou perfis desatualizados. Considerar esse custo nos testes de carga.
8. Recuperação de senha usa alfabeto fixo de 32 símbolos para que configurações fracas não reduzam a entropia. O parâmetro antigo de combinação continua aceito por compatibilidade, mas não determina mais os caracteres desses códigos.

## Pendências que precisam continuar no backlog

| Prioridade | Próxima task | Motivo |
| --- | --- | --- |
| Alta | Migrar .NET 8 para LTS suportada | Fim do suporte em 10/11/2026; endurecimento da aplicação não resolve ciclo de vida do runtime |
| Alta | Aplicar mudanças via PR e ativar regras no GitHub | Código local não comprova revisão nem bloqueio efetivo de merge/release |
| Alta | Provisionar OCI, HTTPS, proxy e segredos exclusivos | Conta OCI ainda indisponível. Rotacionar também quaisquer outras credenciais que tenham sido efetivamente expostas; não considerar o histórico limpo antes do scan |
| Alta | Expandir auditoria de autorização a todos os endpoints | A revisão de vínculo desta tarefa cobre os dois uploads; não certifica todos os fluxos acadêmicos e institucionais |
| Média | Implantar CSP completa com nonce e testar documentos/assinaturas | A CSP atual protege base-uri, object-src, frame-ancestors e form-action. **Não define script-src/default-src e não fecha o risco de XSS.** A política com nonce requer trabalho próprio e testes de compatibilidade |
| Média | Verificar PDFs com parser, antivírus e/ou CDR | Assinatura e EOF são checagens preliminares; não provam integridade completa, ausência de JavaScript embutido ou malware |
| Média | Uniformizar recuperação contra enumeração e limitar por conta | Ainda existem diferenças de resposta para e-mails cadastrados; o limiter atual é por IP e em memória, por instância |
| Média | Configurar proxies confiáveis e limitação na borda | Não confiar em X-Forwarded-For arbitrário. Atrás de proxy ainda não configurado, usuários podem compartilhar o mesmo bucket de IP |
| Média | Revisar armazenamento de tokens e logs de acesso | Tokens de sessão da interface ainda são acessíveis a JavaScript. Links de convite continuam contendo token na navegação inicial; configurar redação nos logs do frontend/proxy e estudar sessão HttpOnly/BFF |
| Média | Restringir observabilidade e interfaces administrativas na infraestrutura | Métricas, storage, banco, backups e painéis devem ter redes/ACLs próprias. O fechamento do dashboard Hangfire não configura firewall ou IAM |

## Texto curto para a apresentação

“Nesta entrega, implementei controles de segurança na aplicação além da esteira: recuperação de senha com comprovante protegido e código de uso único, bloqueio de upload em TCC alheio, limites e verificações de arquivos, redução de vazamento de erros e tokens, proteção contra tentativas excessivas e validação/revogação de sessões JWT. Também retirei execução de scripts do visualizador de documentos e bloqueei buscas externas na conversão de PDF. Validei o backend com 572 testes e 95,89% de cobertura global de sequências, incluindo os testes já integrados pela equipe e os cenários de concorrência em PostgreSQL, e o frontend com seus 107 testes, build e verificação HTTP dos cabeçalhos. As mudanças estão organizadas em branches para revisão; a implantação na OCI e os controles de infraestrutura continuam pendentes da liberação da conta.”

## Referências técnicas

- [OWASP — recuperação de senha](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html)
- [OWASP — segurança de uploads](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html)
- [Microsoft — rate limiting no ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/rate-limit)
- [Next.js — CSP e nonces](https://nextjs.org/docs/app/guides/content-security-policy)
- [Microsoft — ciclo de suporte do .NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [iText — configuração de ResourceRetriever para pdfHTML](https://kb.itextpdf.com/itext/pdfhtml-custom-resourceretriever)
