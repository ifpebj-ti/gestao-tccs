# ADR 0002 — Recuperação de senha e autorização de arquivos

Data: 10/11/2026

Status: implementado localmente; revisão e execução no GitHub pendentes.

## Contexto

A recuperação utilizava um marcador de código validado no banco, mas a requisição de troca não comprovava posse do código validado. O upload inicial não vinculava o usuário autenticado ao trabalho alterado. Operações separadas de consumo e alteração de senha não garantiam uso único sob concorrência.

## Decisão

Emitir após a verificação um comprovante com propósito exclusivo via ASP.NET Data Protection, em cookie HttpOnly/SameSite=Strict, Secure fora de Development e válido por até cinco minutos. O comprovante vincula conta, código atual e senha atual, sem colocar essas informações em texto aberto no navegador. O marcador da interface não constitui autorização.

O gateway consome o código e altera a senha em transação PostgreSQL, com atualizações condicionais. A primeira senha utiliza consumo equivalente do convite, e nenhuma dessas operações usa o bypass antigo da conta de demonstração. Alterações de senha, status e perfis invalidam o uso dos JWTs anteriormente emitidos mediante conferência no banco a cada requisição.

Uploads de monografia e versão final usam a identidade autenticada, vínculo ou escopo de gestão por curso, checagem preliminar de PDF e limite de 20 MiB antes de copiar o arquivo para a memória. O servidor gera o nome do objeto.

## Consequências

Tokens antigos exigem novo login. Recuperação depende de cookie enviado entre frontend/API no mesmo site e, em múltiplas instâncias, de keyring compartilhado e protegido. As transações exigem banco relacional com suporte aos comandos utilizados; os testes usam PostgreSQL descartável.

A consulta por requisição adiciona custo ao caminho autenticado, em troca de revogação imediata após mudanças persistidas. A checagem preliminar do PDF não substitui antivírus, parser ou CDR. A política CSP inicial não constitui proteção completa de scripts. Nenhuma dessas decisões substitui TLS, IAM, proteção do banco, backup e firewall da futura OCI.

Tasks, validação, compatibilidade e riscos restantes: [entrega de segurança de 11/11/2026](../security/tasks-seguranca-2026-11-11.md).
