# Manutenção do changelog e política de imagens candidatas

## Objetivo

Corrigir duas pendências da avaliação: o histórico de versões desatualizado e a diferença entre a política de vulnerabilidades da publicação de imagens candidatas e a política da release principal.

## Histórico de versões

O `CHANGELOG.md` passa a reproduzir as notas e datas de publicação das releases públicas do GitHub. A reconstrução inicial contém 63 releases, até a versão 2.13.3. Não foram inventadas descrições para versões sem notas. O registro manual anterior foi preservado em `changelog-legado.md`; suas referências a tecnologias e limites antigos são históricas.

O histórico de releases não comprova, por si só, que todas as versões possuem imagens no GHCR. A conciliação de tags, releases e imagens antigas permanece uma tarefa separada.

## Atualização automática

Após publicar e verificar uma release, `production.yml` executa `scripts/changelog-pull-request.cjs`. O script consulta todas as páginas de releases, compara o conteúdo com a `main` e, quando necessário, cria uma branch e uma PR que altera somente `CHANGELOG.md`.

A PR marca `outros` no template para não gerar outra release. A automação não atualiza branches protegidas, não aprova revisões e não faz merge. A equipe deve revisar as notas, autorizar os workflows caso o GitHub solicite e aguardar o Quality gate antes da integração.

Também é possível executar manualmente o workflow `Synchronize release changelog`, selecionando a `main`. Essa opção atende à recuperação de falhas e às mudanças posteriores nas notas de uma release. Falhas de permissão são informadas no job; a publicação já realizada não é desfeita.

## Imagens candidatas

O workflow manual de imagens candidatas passa a gerar JSON do Trivy e aplicar `security-gate.cjs`, o mesmo avaliador usado na release principal. Vulnerabilidades altas e críticas impedem a publicação, inclusive quando ainda não existe correção disponível. A política é mais restritiva que o mínimo solicitado na avaliação. O relatório fica disponível como artifact por 14 dias, inclusive quando o gate falha.

As imagens candidatas continuam separadas das tags de release. Essa alteração não publica imagens por conta própria, não aciona deploy e não modifica os testes ou os limites de cobertura.

## Validação após integração

Validação local desta entrega: 51 testes dos scripts aprovados, sem falhas ou testes ignorados; workflows alterados aprovados pelo Actionlint 1.7.7. A abertura real da PR automática depende da execução no GitHub e ainda precisa ser confirmada após a integração. Os testes da aplicação e o gate de cobertura não foram alterados.

1. Conferir o CHANGELOG com a página de releases, incluindo versões sem notas.
2. Publicar a próxima release pelo fluxo normal e verificar o step de sincronização.
3. Se houver diferenças, revisar a PR automática e confirmar que altera somente o changelog.
4. Confirmar que o template está em `outros` e que o merge não gera nova release.
5. No workflow manual de imagens candidatas, conferir o relatório do Trivy e o resultado do gate antes de qualquer publicação.

## Pendências fora desta entrega

- Finalizar as migrações de dependências em suas branches próprias.
- Definir responsáveis para um eventual CODEOWNERS com a equipe.
- Concluir a confirmação de rotação da chave JWT nos ambientes dos integrantes.
- Verificar os alertas privados e conciliar imagens antigas com tags e releases.
- Atualizar Wiki e README e o plano de testes com os integrantes responsáveis.
- Provisionar e validar a OCI quando houver acesso à conta.
