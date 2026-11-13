# Política de segurança

## Como relatar uma vulnerabilidade

Use o [reporte privado de vulnerabilidades do GitHub](https://github.com/ifpebj-ti/gestao-tccs/security/advisories/new). Esse é o canal do projeto para discutir possíveis falhas sem publicar detalhes de exploração em uma issue aberta.

Informe, quando possível:

- Versão, tag ou commit em que identificou o problema.
- Componente afetado e condições necessárias para reproduzir a falha.
- Passos de reprodução com dados fictícios e resultado observado.
- Impacto esperado e evidências sem dados pessoais ou credenciais.

Não envie senhas, tokens, arquivos `.env`, documentos acadêmicos privados ou dados reais de usuários. Se a falha envolver uma credencial exposta, informe sua localização sem reproduzir o valor.

A equipe deve analisar o relato, confirmar o impacto e combinar a correção e a divulgação com quem reportou. Não estabelecemos um prazo fixo de resposta ou correção. O acompanhamento ocorre pelo próprio relato privado.

Problemas funcionais sem implicação de segurança podem ser registrados nas [issues do projeto](https://github.com/ifpebj-ti/gestao-tccs/issues).

## Versões e correções

As correções são preparadas no fluxo de desenvolvimento e disponibilizadas em novas releases. O projeto não mantém uma política de backport para todas as versões antigas. Ao reportar, indique a versão utilizada; a equipe deve avaliar se a falha também ocorre no código atual.

Publicar uma imagem não atualiza automaticamente os ambientes que a utilizam. Cada responsável pelo ambiente precisa aplicar a versão corrigida e revisar suas configurações.

## Credenciais e ambientes

Credenciais devem ser fornecidas pela configuração do ambiente, sem inclusão no Git, em imagens ou em logs. O `.env.example` serve como referência e não deve fornecer segredos prontos para uso.

Quando uma credencial for exposta, remover o valor do código não encerra o tratamento. É necessário substituir ou revogar a credencial nos ambientes afetados e verificar os serviços que ainda possam utilizá-la. Não registre a nova credencial em issues, comentários ou evidências.

O deploy na OCI ainda depende do provisionamento da conta. A aprovação dos testes e scans não comprova implantação, proteção da infraestrutura ou ausência de vulnerabilidades.

## Controles e limitações

O CI inclui testes, cobertura, análise estática, verificação de dependências, busca de segredos e DAST em ambiente temporário. Os resultados devem ser avaliados no commit correspondente; alertas precisam de triagem, inclusive quando houver suspeita de falso positivo.

Os controles implementados, as validações e as limitações conhecidas estão descritos em:

- [Segurança da aplicação](docs/security/tasks-seguranca-2026-11-11.md).
- [Controles de CI/CD e pendências de infraestrutura](docs/security/ci-cd-auditoria.md).

Esses registros não substituem a revisão de segurança de novas alterações nem a validação do ambiente implantado.
