# Post-mortem de cinco linhas

**Version-anchor:** Q4-2026 · **Tipo:** template do kit da aula de Integração e Mensageria no Azure

Use ao fim de cada incidente da aula. Uma ou duas frases por linha, **sem culpados**: descreva sistemas, decisões e sinais, nunca pessoas. Se algo deu errado porque alguém fez X, a pergunta é o que no sistema deixou X ser possível (ou fácil) e o que vai impedir na próxima vez.

**Incidente:** _nome curto_ · **Data:** _aaaa-mm-dd_ · **Cenário:** _3.x_ · **Autoria:** _papel ou equipe, nunca nome de pessoa_

| Linha | Resposta |
|---|---|
| **O que aconteceu** | |
| **Impacto** | |
| **Causa raiz** | |
| **O que consertamos** | |
| **O que impede de repetir** | |

## Como preencher

- **O que aconteceu**: o sintoma visto de fora, com hora e duração. O que o cliente, o parceiro ou o painel mostrou.
- **Impacto**: quem foi afetado e quanto, em números (faturas, cobranças, minutos, empresas clientes). Diga também o que **não** foi afetado.
- **Causa raiz**: o mecanismo técnico, não a pessoa. Pergunte "por quê?" até chegar a uma decisão de desenho ou a uma proteção que faltava.
- **O que consertamos**: a mudança feita e a **prova** de que funcionou: o comando, a consulta KQL, o número antes e depois.
- **O que impede de repetir**: um mecanismo que funciona sozinho (teste, alerta, política, limite, Bicep), nunca "ter mais atenção" ou "avisar o time".

Sem culpados, na prática: troque "fulano esqueceu de..." por "o processo permitia...", e "o ERP do cliente errou" por "o hub aceitava...".

## Exemplo preenchido (fictício)

**Incidente:** cobranças duplicadas depois de timeout · **Data:** 2026-11-06 (sexta-feira) · **Cenário:** 3.2, "O timeout de sexta às 18h" · **Autoria:** time de integração da Saldaro

| Linha | Resposta |
|---|---|
| **O que aconteceu** | Das 18h05 às 18h50, o banco parceiro, em janela de manutenção, passou a registrar cobranças em 40 a 90 s; o APIM cortou as chamadas em 30 s (504) e os ERPs reenviaram as faturas. |
| **Impacto** | Cerca de 200 faturas de 9 empresas clientes viraram 2 ou 3 faturas no hub, com cerca de 250 cobranças a mais no banco; 40 pagadores receberam boleto em dobro e ninguém pagou duas vezes. As demais empresas clientes não foram afetadas. |
| **Causa raiz** | O `POST /faturas` era síncrono e registrava a cobrança no banco antes de responder; sem chave de idempotência, o hub não tinha como saber que o reenvio era a mesma fatura (mesma `referenciaErp`). |
| **O que consertamos** | O POST passou a responder 202 e a emitir a cobrança por fila (`sbq-faturas-recebidas`), honrando `Idempotency-Key`. Prova: 202 em menos de 500 ms com o banco a 60 s, 1.000 faturas em rajada sem nenhum 5xx e `kql/faturas-por-referencia-erp.kql` vazio. |
| **O que impede de repetir** | O guia de integração dos ERPs passou a exigir `Idempotency-Key` em todo reenvio; o teste de carga com `-Caos "latencia=60"` roda antes de cada release; um alerta dispara se `faturas-por-referencia-erp.kql` devolver linhas. |
