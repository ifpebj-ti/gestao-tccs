# Manutenção do changelog e política de imagens candidatas

## Objetivo

Corrigir duas pendências da avaliação: o histórico de versões desatualizado e a diferença entre a política de vulnerabilidades da publicação de imagens candidatas e a política da release principal.

## Histórico de versões

O `CHANGELOG.md` passa a reproduzir as notas e datas de publicação das releases públicas do GitHub. A reconstrução inicial contém 63 releases, até a versão 2.13.3. Não foram inventadas descrições para versões sem notas. O registro manual anterior foi preservado em `changelog-legado.md`; suas referências a tecnologias e limites antigos são históricas.

O histórico de releases não comprova, por si só, que todas as versões possuem imagens no GHCR. A conciliação de tags, releases e imagens antigas permanece uma tarefa separada.

## Atualização automática

O Release Please gera o changelog na PR de release. Consulte o [fluxo de release](../../CONTRIBUTING.md#release-e-implantação) para configurar e validar a automação.

## Imagens candidatas

O workflow manual de imagens candidatas passa a gerar JSON do Trivy e aplicar `security-gate.cjs`, o mesmo avaliador usado na release principal. Vulnerabilidades altas e críticas impedem a publicação, inclusive quando ainda não existe correção disponível. A política é mais restritiva que o mínimo solicitado na avaliação. O relatório fica disponível como artifact por 14 dias, inclusive quando o gate falha.

As imagens candidatas continuam separadas das tags de release. Essa alteração não publica imagens por conta própria, não aciona deploy e não modifica os testes ou os limites de cobertura.

## Validação após integração

1. Conferir o changelog e a versão propostos na PR de release.
2. Após o merge, verificar a release e os manifestos de ambas as imagens.
3. No workflow de imagens candidatas, conferir o relatório do Trivy e o resultado do gate.

## Diretório de artefatos do VSTest

Na execução 43719464560, o backend ARM64 aprovou 573 testes, sem falhas ou ignorados, mas o processamento posterior dos artefatos foi recusado. O log apontou permissões de acesso a outros usuários e erro de abertura da pasta da sessão. Por isso, o job e a validação da imagem ARM64 não foram concluídos.

O step passa a usar `umask 077` e uma pasta exclusiva criada por `mktemp -d` para `VSTEST_ARTIFACTS_DIRECTORY`. Isso restringe os novos arquivos e diretórios ao usuário da execução, sem desabilitar a verificação do VSTest. A confirmação da correção depende de nova execução no runner ARM64; o teste local de scripts não reproduz esse ambiente.

## Pendências fora desta entrega

- Finalizar as migrações de dependências em suas branches próprias.
- Definir responsáveis para um eventual CODEOWNERS com a equipe.
- Concluir a confirmação de rotação da chave JWT nos ambientes dos integrantes.
- Verificar os alertas privados e conciliar imagens antigas com tags e releases.
- Atualizar Wiki e README e o plano de testes com os integrantes responsáveis.
