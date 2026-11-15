# Triagem do Gitleaks na PR #785

A execução [CI #154](https://github.com/ifpebj-ti/gestao-tccs/actions/runs/43500917192) bloqueou o job de histórico por uma ocorrência `generic-api-key` no `.env.example`, linha 25, commit `c8b72dbef15319eaf0113d12d286e5bad1893c2d`.

A variável identificada é `COMBINATION_STRING_FOR_ACCESSCODE`. Seu conteúdo define os caracteres disponíveis para gerar códigos; não é um código emitido, senha, token ou chave de assinatura. A comparação local confirmou que a linha é idêntica ao alfabeto público já triado no commit `6167e1ef0e4fa3be575b8899b7d4f53be53d6383`, linha 23 do mesmo arquivo.

Foi adicionada ao `.gitleaksignore` somente a identificação exata dessa ocorrência: commit, arquivo, regra e linha. Não foram liberados arquivos inteiros, padrões de valores ou regras. O comando de varredura integral e o bloqueio por novos achados permanecem ativos.

Essa triagem não se aplica às chaves JWT ou RPC antigas nem encerra a verificação de rotação nos ambientes da equipe. A execução do CI precisa ser repetida após a atualização da PR para confirmar que não há outros achados.
