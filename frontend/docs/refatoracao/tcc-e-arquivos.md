# TCC e documentos assinados — 13/10/2026

## Pedido e decisões

- Controle de recolher movido do cabeçalho para o rodapé da própria lateral, que permanece visível enquanto a página rola no computador.
- Pendências da home usam o FeatureActionCard verde escuro já aprovado. Contagem em verde-lima e ação “Ver pendências”; destinos, contagens e condições de cada perfil foram mantidos. Não foi criado um serviço novo de notificações.
- TCC apresenta o título real da proposta em tamanho menor, etapas e abas, nessa ordem. O nome do estudante não é repetido abaixo do título; os participantes permanecem na seção própria de informações. As etapas usam uma lista acessível, com uma única etapa atual, indicação de concluídas e texto legível no celular.
- Assinaturas, informações e arquivos compartilham a apresentação TccOverview. Edição, cancelamento, agendamento, upload existente e demais ações mantêm os handlers anteriores.
- Botões secundários usam borda verde ou hover verde escuro com texto verde-lima. Ações destrutivas continuam identificadas em vermelho.

A usuária autorizou ampliar a exceção de backend para listagem e downloads com validação de acesso. Depois restringiu a pasta a documentos com assinaturas registradas: arquivo final, modelos, documentos sem assinatura e propostas não entram nela. A contagem informa assinaturas registradas, não comprova que todos os participantes já assinaram.

## Pasta e acesso

As rotas `/myTCC/files`, `/ongoingTCCs/files` e `/completedTCCs/files` reutilizam o mesmo componente. A pasta oferece download individual e ZIP, estado vazio, erro, acesso indisponível e nova tentativa.

Downloads ficam exclusivamente na aba Arquivos. Cada documento tem botão de ícone com nome acessível, indicação de processamento e tooltip; o botão de baixar foi removido da aba Informações.

Endpoints novos:

- `GET /api/Tcc/{tccId}/files`
- `GET /api/Tcc/{tccId}/files/{documentId}/download`
- `GET /api/Tcc/{tccId}/files/download`

O ID do usuário vem do JWT validado, e o backend consulta conta ativa, perfis atuais e vínculos no banco. Aluno e orientador precisam estar vinculados ao TCC no perfil atual. Coordenação, supervisão e biblioteca têm acesso dentro do próprio campus/curso; administração mantém acesso de gestão e banca precisa de vínculo. Pessoas sem acesso recebem 404, sem revelar se aquele TCC existe.

A listagem usa somente documentos persistidos com pelo menos uma assinatura e chave de armazenamento. Não publica as chaves. Os downloads retornam o artefato gravado, PDF ou HTML legado, como anexo; não geram um modelo novo para colocá-lo na pasta. ZIP usa as chaves dos documentos vinculados ao TCC, sem procurar por título/ano, e limita o conjunto a 50 MB. Arquivo ausente ou armazenamento indisponível produz erro, sem ZIP parcial silencioso.

As rotas antigas de busca/download de documento também conferem vínculo, documento pertencente ao TCC e estudante informado. O download antigo em lote utiliza a mesma seleção de assinados. Esta correção não constitui uma auditoria completa das demais rotas da API.

## Inventário

Frontend: layout autenticado, Sidebar, HomeShortcuts, FeatureActionCard, Steps, TccTabs, TccHeader, TccInfoSection, SignaturesClient, DetailsClient, Button; novos TccOverview e TccFiles; três páginas de arquivos. Detalhes concluídos também compartilham cabeçalho e abas com links adequados à rota existente. O handler de download em lote sem consumidores foi removido de useTccDetails. Componentes da tela de informações também receberam formatação, sem mudanças em regras. Testes: route-access, novo tcc-presentation e preview-shell.

Backend: novos TccFilesUseCase e TccFilesController, registro em IocDependencyExtensions, proteção em SignatureController; novos TccFilesUseCaseTests e TccFilesEndpointTests. Sem mudança de schema, migration, seed ou configuração de ambiente.

## Validação

- 63 testes do frontend aprovados; lint e TypeScript aprovados.
- 232 testes do backend aprovados no host com `DOTNET_ROLL_FORWARD=Major`.
- Os mesmos 232 testes passaram no runtime nativo .NET 8, em container temporário sem rede, usando o cache local de dependências públicas.
- 35 verificações no Chrome isolado, com usuário fictício e respostas interceptadas: layout desktop/mobile, teclado, erros de formulário, etapas, pasta vazia, acesso negado, falha de download, recuperação e navegação em TCC concluído.
- Capturas e relatório em `evidencias/tcc-e-arquivos`; evidências anteriores preservadas. Nenhuma gravação desses testes usou banco real.

Ainda é necessário o teste integrado da equipe com arquivos reais e contas dos diferentes perfis. Os resultados não comprovam cobertura de 90%, ausência de vulnerabilidades ou conclusão dos marcos da segunda unidade.

## GitHub e entrega

Tudo permanece local e sem commit. A revisão visual complementa a primeira issue criada pela equipe. A pasta merece uma issue própria: `feat(tcc): disponibilizar documentos assinados com controle de acesso`, labels sugeridas `frontend`, `backend`, `security`, `enhancement`, `tests`. O prazo continua até 30/11/2026. Não foram criados branches, issues ou PRs remotamente.
