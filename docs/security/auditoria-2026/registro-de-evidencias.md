# Registro de evidências da preparação de auditoria

Data: 15/11/2026. Referência principal: release [2.14.0](https://github.com/ifpebj-ti/gestao-tccs/releases/tag/2.14.0), publicada em 15/11/2026 às 18:09:47 UTC, correspondente ao commit `e18544f81c815925d9c42027806eed73b80cfbce`.

## Método de coleta

- Consulta por leitura à API pública do GitHub e aos manifestos/configurações do GHCR.
- Conferência do código e das configurações no commit selecionado.
- Resumo das capturas dos painéis privados fornecidas pela responsável no atendimento.
- Verificação direta do ambiente local, sem exibição de credenciais, e confirmação relatada da responsável pelo outro ambiente.

O [arquivo JSON](evidencias-publicas-2026-11-15.json) contém o instante UTC da coleta pública e seus resultados estruturados, sem token de autenticação. A coleta é pontual; estados de branches, alertas e artifacts podem mudar.

## Inventário

| ID | Controle | Origem | Resultado e limite |
| --- | --- | --- | --- |
| EV-01 | CI da main publicada | [Run 43742819872](https://github.com/ifpebj-ti/gestao-tccs/actions/runs/43742819872) | Sete jobs aprovados, incluindo testes/cobertura, SAST, SCA/segredos, DAST e Quality gate; SHA e18544f81c815925d9c42027806eed73b80cfbce. |
| EV-02 | Publicação | [Run 43742819831](https://github.com/ifpebj-ti/gestao-tccs/actions/runs/43742819831), [PR #824](https://github.com/ifpebj-ti/gestao-tccs/pull/824) | Publicação aprovada, scans das imagens e preparação de branch do changelog concluídos. Deploy manual pulado. O campo head_sha da execução disparada por PR identifica a develop 66d2840; o commit da release e das imagens foi conferido separadamente como e18544f. |
| EV-03 | Validação prévia multiarch | [Run 43738127264](https://github.com/ifpebj-ti/gestao-tccs/actions/runs/43738127264) | Quatro combinações aprovadas, no commit 96efe3e9f8b74b45c79787d430cf79915509905f. Esta validação é anterior à release e não deve ser apresentada como execução no SHA e18544f. |
| EV-04 | Imagens publicadas | GHCR, digests abaixo | Frontend e backend 2.14.0 com linux/amd64 e linux/arm64 e revisão e18544f. Não comprova deploy. |
| EV-05 | Proteção de branches | [Ruleset main](https://github.com/ifpebj-ti/gestao-tccs/rules/27234217), [ruleset develop](https://github.com/ifpebj-ti/gestao-tccs/rules/27234055) | Ativos; uma aprovação, descarte de aprovação após novos commits, resolução de conversas, Quality gate do GitHub Actions, bloqueio de exclusão/force push e zero atores de bypass. Atualização obrigatória da branch antes do merge não está habilitada. |
| EV-06 | CodeQL na main | [Run 43742819309](https://github.com/ifpebj-ti/gestao-tccs/actions/runs/43742819309) | Execução aprovada no SHA e18544f; a captura do painel registra os alertas separadamente. |
| EV-07 | Dependabot Alerts | [Painel](https://github.com/ifpebj-ti/gestao-tccs/security/dependabot), captura fornecida pela responsável | Filtro is:open: zero abertos e 118 encerrados. Os motivos dos 118 encerramentos não foram auditados individualmente. |
| EV-08 | Code scanning | [Painel](https://github.com/ifpebj-ti/gestao-tccs/security/code-scanning), captura fornecida pela responsável | Filtro is:open branch:main: zero abertos e sete encerrados. Resultado restrito à referência e ao momento mostrados. |
| EV-09 | Secret scanning | [Painel](https://github.com/ifpebj-ti/gestao-tccs/security/secret-scanning), capturas fornecidas pela responsável | Default: zero abertos. Generic: captura inicial com um aberto e três encerrados; alerta #4 associado à chave antiga. A triagem de encerramento foi conduzida com a responsável após a verificação local e a confirmação do outro ambiente; o painel final não foi reconferido por esta coleta pública. |
| EV-10 | Rotação da chave | [Issue #794](https://github.com/ifpebj-ti/gestao-tccs/issues/794) | Issue confirmada como encerrada na API pública. Conferência local e confirmação do outro ambiente descritas abaixo. |
| EV-11 | Histórico e triagens | [Triagem do Gitleaks](../triagem-ci-pr-785.md), [atualização das dependências](../atualizacao-dependencias-compativeis-2026-11-15.md) | Justificativas específicas e limites registrados; não equivalem a ausência de riscos em todo o histórico. |
| EV-12 | Trabalho e arquitetura | [Projects 38](https://github.com/orgs/ifpebj-ti/projects/38), [ADRs](../../adr/0001-gates-de-seguranca-e-deploy-separado.md), [CONTRIBUTING](../../../CONTRIBUTING.md) | Fontes de processo e decisões. Confirmar leitura pelos auditores; pacote não altera o plano de testes ou a Wiki. |

## Identidade das imagens

Verificação por leitura, com conferência dos digests, da configuração de plataforma e da etiqueta `org.opencontainers.image.revision` de cada imagem executável.

| Imagem 2.14.0 | Digest do índice |
| --- | --- |
| ghcr.io/ifpebj-ti/gestaotccs-backend:2.14.0 | sha256:93fe446623c3d04a21968d99684caa53a5d18b22787690090334894c10106030 |
| ghcr.io/ifpebj-ti/gestaotccs-frontend:2.14.0 | sha256:8e6d46c5a31a33de37866220750545e6a783877de5b6a66fa212d9c04d59262e |

Ambos os índices apresentaram exatamente linux/amd64 e linux/arm64 como imagens executáveis, com revisão `e18544f81c815925d9c42027806eed73b80cfbce`. Os digests por arquitetura constam no JSON. Nenhum manifesto ou tag foi alterado para produzir essa evidência.

Para repetir a verificação por PowerShell, na raiz do repositório:

```powershell
$env:GITHUB_SHA = 'e18544f81c815925d9c42027806eed73b80cfbce'
node scripts/controlled-publication.cjs verify-published ghcr.io/ifpebj-ti/gestaotccs-backend 2.14.0
node scripts/controlled-publication.cjs verify-published ghcr.io/ifpebj-ti/gestaotccs-frontend 2.14.0
Remove-Item Env:GITHUB_SHA
```

## Relatórios completos e retenção

Metadados de sete artifacts da publicação foram consultados; nenhum estava expirado. Os arquivos completos não foram baixados pela consulta pública. Um integrante autenticado deve preservá-los antes de 29/11/2026; o horário exato de expiração de cada artifact está no JSON.

| Artifact | Conteúdo a conferir |
| --- | --- |
| backend-evidence | TRX, OpenCover e auditoria NuGet. |
| frontend-evidence | Auditoria npm. Resultados dos testes, lint e build também constam nos logs do job. |
| sast-evidence | SARIF do Semgrep e Opengrep. |
| sca-secrets-evidence | Trivy, SBOM e Gitleaks com achados redigidos. |
| dast-evidence | Relatórios JSON/HTML e logs do ambiente temporário. |
| release-backend-image-scans | Relatórios das imagens backend por arquitetura. |
| release-frontend-image-scans | Relatórios das imagens frontend por arquitetura. |

Procedimento de preservação:

1. Abrir EV-02 autenticado e baixar os sete artifacts pelo Summary.
2. Guardar os arquivos originais em local restrito da equipe, identificados pelo run e SHA. Não incluir automaticamente arquivos completos no Git público.
3. Conferir se os relatórios pertencem ao run, se estão completos e se o coletor não ignorou testes. Verificar achados de severidades inferiores ao bloqueio.
4. Produzir cópias sanitizadas para a apresentação, removendo credenciais, cookies, conteúdo privado e dados pessoais. Manter o original restrito sem alteração.
5. Registrar nome, tamanho e SHA-256 de cada arquivo arquivado, além da pessoa que conferiu e da data.

No PowerShell, o hash de um ZIP pode ser obtido sem abrir o conteúdo:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath 'CAMINHO_DO_ARQUIVO.zip'
```

As capturas privadas recebidas não foram copiadas para o repositório. O resumo acima preserva os resultados observados. Caso sejam anexadas à entrega, ocultar os valores de segredos e quaisquer dados privados antes de compartilhar.

## Alcance dos testes e do DAST

A validação registrada da migração apresentou 573 testes de backend, 107 de frontend e cobertura global de sequências do backend de 95,96%, com gate de 95%. Os arquivos TRX/OpenCover do run escolhido devem ser preservados para comprovar as contagens e a medição daquele SHA, sem substituir por um relatório local anterior. O badge mostra percentual inteiro arredondado para baixo e não representa cobertura de frontend.

O DAST usa banco descartável e API em Development para expor OpenAPI ao scanner; o frontend é construído para execução no ambiente temporário. A varredura web é baseline; a API é examinada a partir do OpenAPI. Não há sessão autenticada configurada no script nem serviço de armazenamento completo para todos os fluxos. Isso limita a análise de autorização entre perfis, uploads reais e assinatura de documentos. O gate ZAP bloqueia risco alto; achados médios/baixos precisam ser lidos no relatório e triados.

## Rotação e alerta genérico #4

Verificação direta do ambiente local em 15/11/2026: backend em execução; chave no contêiner igual à configuração local; ambas diferentes da chave antiga conhecida; chave em uso com pelo menos 32 bytes. Somente resultados booleanos foram exibidos; nenhum valor de credencial foi armazenado neste pacote.

A responsável pelo outro ambiente informou uso da chave nova, recriação de contêineres e funcionamento da aplicação. Trata-se de confirmação relatada, não de inspeção remota realizada por esta coleta. Outros computadores e instâncias devem ser considerados no inventário de ambientes se também utilizaram a chave antiga.

O código publicado rejeita a chave antiga por comparação de hash na inicialização. A ocorrência mostrada pelo GitHub corresponde a um commit histórico que continha uma comparação literal para bloquear essa chave. Remover a literal do código não seria, isoladamente, prova de rotação. A evidência de fechamento da issue não substitui a confirmação do estado privado do alerta. Não foi executado neste atendimento um teste manual com um token antigo em cada máquina.

## Pendências do pacote

- Arquivamento dos relatórios completos com autenticação e hashes dos arquivos preservados.
- Captura sanitizada do estado final do alerta genérico #4, se necessária à apresentação.
- Confirmação dos acessos dos auditores, ambiente e entrevista.
- Revisão de Wiki/README e plano de testes pela responsável.
- Conciliação das imagens históricas e atualização documental da última release.
- Definição dos responsáveis para eventual CODEOWNERS, sem atribuições presumidas.
- Auditoria independente do outro projeto após acesso, análise e entrevista.
