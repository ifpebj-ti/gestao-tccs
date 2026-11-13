# Atualização do badge com branches protegidas

Relacionada à #748.

O badge utiliza a cobertura global de sequências do backend aprovada pelo CI do commit publicado. O percentual inteiro é arredondado para baixo. A publicação não faz mais uma atualização direta do README na main.

## Funcionamento

Após a publicação e a verificação das imagens, o script `scripts/badge-pull-request.cjs` prepara uma alteração somente na linha identificada por `COVERAGE_BADGE`. Cria uma branch vinculada ao SHA e à execução e abre uma PR para main com a opção `outros` selecionada. Não aprova, integra ou habilita auto-merge.

Se o badge já contém o percentual aprovado, nenhuma PR é necessária. Se a main avançou antes da criação da branch, o script interrompe a atualização para não propor uma medição antiga sobre alterações posteriores. Nesse caso, revisar a situação e preparar uma atualização por PR, com identificação do commit medido.

Uma reexecução reutiliza a branch e a PR abertas somente após conferir que a branch contém um único commit adicional e apenas a alteração esperada no README. Branch alterada ou PR fechada exige revisão manual; o script não força atualizações nem reabre PRs.

## Configuração do GitHub

Em Settings → Actions → General → Workflow permissions, habilitar **Allow GitHub Actions to create and approve pull requests**. Apesar do nome da configuração, esta automação cria PRs e não as aprova. Se a opção estiver bloqueada, a política precisa ser revista por quem administra a organização.

O job de publicação solicita `contents: write` e `pull-requests: write` para criar o commit, a branch e a PR. Utiliza o GITHUB_TOKEN temporário do próprio job; não exige PAT ou chave privada adicional. As listas de bypass das branches devem permanecer sem exceção para essa automação.

Conforme a [documentação do GITHUB_TOKEN](https://docs.github.com/en/actions/concepts/security/github_token), os eventos de abertura e atualização de PRs gerados com esse token podem criar execuções que aguardam aprovação. Uma pessoa com acesso de escrita deve selecionar **Approve workflows to run**, quando solicitado, e aguardar o Quality gate. Essa autorização de execução não substitui a revisão e a aprovação da PR por outro integrante.

## Validação após integrar

1. Publicar uma release pelo fluxo normal, respeitando os checks e as aprovações.
2. Conferir no resumo se o badge já corresponde à medição ou se foi aberta uma PR.
3. Quando houver PR, conferir versão, SHA e link da medição na descrição.
4. Confirmar que o diff altera somente a linha do badge.
5. Autorizar os workflows se solicitado e confirmar que o merge continua bloqueado até os requisitos serem atendidos.
6. Após revisão e checks, integrar e conferir o README.
7. Registrar as evidências na #748. Não iniciar outra release apenas por existir uma PR de documentação do badge.

A falha administrativa dessa etapa continua não bloqueante para a release já publicada, mas aparece no step e no resumo da execução. Em caso de falha, o badge pode permanecer com o valor anterior e precisa de acompanhamento. Não contornar a proteção com push direto ou bypass amplo.
