# Correções da primeira execução de segurança do PR #753

Execução analisada: [CI #126](https://github.com/ifpebj-ti/gestao-tccs/actions/runs/43031375751), em 11/11/2026. Os artefatos mostram que os testes e a cobertura mínima de 95% passaram. O job de backend falhou depois, por 44 ocorrências de vulnerabilidades em dependências, incluindo repetições da mesma dependência entre projetos.

## Correções

| Controle | Causa | Tratamento |
| --- | --- | --- |
| ShellCheck | `cleanup` invocada indiretamente por `trap EXIT` foi marcada como inalcançável | Exceção SC2317/SC2329 limitada à função; o tratamento de falhas e a limpeza continuam ativos. Scripts `.sh` usam LF também no Windows |
| NuGet | Dependências transitivas vulneráveis | Newtonsoft.Json 13.0.3, Cryptography.Pkcs 8.0.2, Cryptography.Xml 8.0.5 e System.Net.Http 4.3.4; retirada de referências antigas de autenticação, do metapacote Hangfire e do scaffolder não utilizado. EF e suas ferramentas de migrations permanecem |
| DAST | API encerrava antes do HTTP ao acessar `RecurringJob` estático sem armazenamento inicializado | Registro dos três jobs pelo `IRecurringJobManager` resolvido pela injeção de dependências, com expressões tipadas e os mesmos IDs e horários |
| Actions | 54 usos de tags mutáveis | Referências fixadas nos SHAs consultados nos repositórios oficiais, com comentários das versões; setup-python também fixado |
| Dependabot | Ausência de cooldown explícito | Sete dias para atualizações de versão nos quatro ecossistemas; isso não posterga atualizações de segurança |
| XML dos relatórios | Parser padrão aceitava entradas sem defesa explícita contra entidades | defusedxml 0.7.1 instalado com hash do wheel oficial; DTDs proibidos; teste de regressão de entidades externas |
| Containers | Imagens de runtime sem usuário explícito | Backend usa `app`, frontend usa `node`, com permissão para seu cache Next.js |
| Nginx | Repasse de Host e Upgrade controlados pelo cliente | Host vem do upstream configurado; upgrades não são encaminhados pelo proxy de produção, que não utiliza WebSocket |
| E-mails | Nove links apontavam para `http://localhost:3000` | Links usam a URL configurada em `urlfront`, como os demais templates |

Versões de correção consultadas nos avisos oficiais: [Newtonsoft.Json](https://github.com/advisories/GHSA-5crp-9r3c-p9vr), [Pkcs](https://github.com/advisories/GHSA-846f-f764-76mx) e [Xml](https://github.com/advisories/GHSA-c399-9853-6jmj). A política do [cooldown do Dependabot](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference#cooldown) distingue atualizações de versão e segurança.

## Triagem dos quatro achados históricos de segredos

O histórico não foi reescrito. O Gitleaks continua usando `--all`, com valores redigidos e bloqueio de novos achados. `.gitleaksignore` contém somente quatro fingerprints completos, que identificam commit, arquivo, regra e linha; não libera valores nem caminhos inteiros.

| Ocorrência | Conclusão e medida |
| --- | --- |
| Literal da chave antiga no bloqueio de JWT | Era uma comparação de rejeição, não uma chave de assinatura configurada ali. Foi substituída por comparação SHA-256; a chave antiga continua proibida |
| Alfabeto de geração de códigos em `.env.example` | Não é uma credencial: é um conjunto público de caracteres. Exceção limitada à ocorrência histórica |
| Chave RPC pública em `garage.toml` | Removida; substituta gerada aleatoriamente e armazenada somente no `.env` ignorado. Garage recebe `GARAGE_RPC_SECRET`. Nó local único recriado, com os mesmos volumes e estado saudável |
| JWT antigo de exemplo | `TOKEN_KEY` fica vazio no exemplo, exigindo configuração privada. O `.env` local já tinha substituta, mas o backend carregava a antiga. O contêiner local foi recriado com a substituta; a troca foi verificada por hashes, sem registrar o segredo |

O arquivo `.env` privado não foi versionado. A rotação local de JWT invalida as sessões anteriores e exige novo login. Outros ambientes que tenham copiado os valores históricos devem fazer sua própria rotação antes de usar esta versão. A OCI continua sem deploy.

Referência de configuração privada do Garage: [manual oficial](https://github.com/deuxfleurs-org/garage/blob/main-v2/doc/book/reference-manual/configuration.md#rpc_secret). Em um ambiente novo, gere `GARAGE_RPC_SECRET` com 32 bytes aleatórios em hexadecimal antes de iniciar o compose de desenvolvimento; o segredo não possui fallback público.

## Cinco exceções locais de HTTP no Semgrep

As três chamadas de Chrome DevTools em `frontend/scripts/preview-*.cjs` usam exclusivamente `127.0.0.1:9230`, cuja API local não oferece TLS. As duas chamadas em `scripts/multiarch-stack-smoke.cjs` usam o serviço `backend` de uma rede Docker descartável, com dados e credenciais sintéticos. As exceções `nosemgrep` estão nas cinco linhas, com a regra específica. Nenhuma pasta, arquivo ou regra global foi excluída. Links de e-mail reais foram corrigidos, não suprimidos.

## Evidências de validação

Os relatórios da reprodução ficam em `reports/ci-failure-126`, ignorados pelo Git. A primeira reprodução após os patches aprovou 572 testes de backend e registrou zero achados bloqueantes NuGet. Node aprovou 39 testes, Python aprovou oito, incluindo o teste de XML malicioso; os quatro workflows passaram no Actionlint. O scan integral do histórico passou após a triagem acima.

A suíte final com o novo teste do Hangfire aprovou **573 testes**, sem falhas nem ignorados, e manteve **95,96% de cobertura global de sequências** (27.195 / 28.338), sem mudar exclusões ou o mínimo de 95%. O teste isolado da baseline do Gitleaks confirmou que um novo segredo sintético continua sendo bloqueado.

Semgrep e DAST devem ser confirmados pelos relatórios da nova execução do PR. Os bloqueios permanecem ativos; esse registro não substitui a aprovação dos jobs no GitHub.
