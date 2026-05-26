# O que fazer na Parte 2 — Integração de Sistemas

## 1. Enquadramento

Assumindo que a Parte 1 já está concluída, a Parte 2 deve evoluir a solução para uma arquitetura mais próxima de uma plataforma real de apostas desportivas live.

A Parte 1 já resolve a integração base entre:

- Aplicação Geradora de Dados;
- Plataforma de Resultados;
- Plataforma de Gestão de Apostas;
- Sistema de Pagamentos;
- Base de dados com Stored Procedures e Triggers.

A Parte 2 não deve repetir essa implementação. O objetivo agora é acrescentar middleware para:

- Propagar informação em tempo quase real;
- Reduzir o acoplamento entre sistemas;
- Permitir múltiplos consumidores da mesma informação;
- Processar tarefas em background;
- Suportar retries, falhas temporárias e reprocessamento;
- Criar dashboard, alertas e analytics;
- Executar a solução em ambiente containerizado.

---

## 2. Objetivo técnico da Parte 2

A Parte 2 deve introduzir duas camadas distintas de middleware:

1. **Camada de filas de mensagens**, para processamento assíncrono e tarefas que não precisam de resposta imediata.
2. **Camada de streaming de eventos**, para disseminação de eventos em tempo quase real entre vários consumidores independentes.

Além disso, deve existir uma componente de:

- Dashboard;
- Alertas;
- Analytics;
- Testes de carga, falha e reprocessamento;
- Docker Compose para levantar a infraestrutura completa.

---

## 3. Tecnologias recomendadas

### 3.1. Filas de mensagens

Tecnologia recomendada: **RabbitMQ**.

Justificação:

- Simples de integrar com .NET;
- Adequado para tarefas assíncronas;
- Suporta acknowledgements;
- Suporta retries;
- Suporta Dead Letter Queues;
- Permite separar o caminho crítico da aplicação de tarefas secundárias.

Usar RabbitMQ para:

- Notificações;
- Auditoria;
- Relatórios em background;
- Reprocessamento de operações falhadas;
- Alertas assíncronos;
- Simulação de envio de email/log/consola.

### 3.2. Streaming de eventos

Tecnologia recomendada: **Kafka** ou **Redpanda**.

Recomendação prática: usar **Redpanda** se quiserem simplificar o Docker Compose, porque é compatível com Kafka mas mais leve para ambiente académico.

Usar Kafka/Redpanda para:

- Eventos de jogos;
- Eventos de apostas;
- Eventos de pagamentos;
- Eventos de resultados;
- Alimentação de dashboards;
- Analytics em tempo quase real;
- Replay de eventos históricos.

### 3.3. Dashboard e analytics

Opções possíveis:

- ASP.NET + Razor Pages;
- Blazor;
- React simples;
- HTML + JavaScript;
- API de métricas + Swagger, caso o tempo seja curto.

A solução mais simples é criar uma **AnalyticsAPI** em ASP.NET que consulta uma base de dados analítica alimentada por um **AnalyticsWorker**.

---

## 4. Arquitetura proposta

```text
Aplicação Geradora
        |
        v
Plataforma de Resultados API
        |
        v
Plataforma de Apostas API
        |
        |-----------------------> SQL Server / BD Apostas
        |-----------------------> SQL Server / BD Pagamentos
        |
        | publica eventos
        v
Kafka / Redpanda
        |
        |-----------------------> AnalyticsWorker
        |-----------------------> DashboardAPI
        |-----------------------> AlertasWorker
        |-----------------------> AuditoriaWorker

Plataforma de Apostas API
        |
        | envia tarefas assíncronas
        v
RabbitMQ
        |
        |-----------------------> WorkerNotificacoes
        |-----------------------> WorkerRelatorios
        |-----------------------> WorkerReprocessamento
        |-----------------------> Dead Letter Queue
```

---

## 5. Eventos a criar

Devem ser definidos eventos JSON para representar acontecimentos importantes do sistema.

Eventos mínimos recomendados:

```text
JogoCriado
JogoAtualizado
ResultadoAtualizado
JogoFinalizado
ApostaCriada
ApostaResolvida
ApostaCancelada
TransacaoCriada
SaldoAtualizado
AlertaGerado
```

### Exemplo de evento `ApostaCriada`

```json
{
  "eventId": "8b5a9b2e-4e20-4cb4-a835-123456789abc",
  "eventType": "ApostaCriada",
  "timestamp": "2026-05-22T15:30:00Z",
  "apostaId": 12,
  "jogoCodigo": "FUT-2026-0103",
  "utilizadorId": 4,
  "tipoAposta": "1",
  "valor": 20.00,
  "odd": 1.85
}
```

### Exemplo de evento `JogoFinalizado`

```json
{
  "eventId": "4dcf0c3d-c2e6-41f3-a1aa-987654321abc",
  "eventType": "JogoFinalizado",
  "timestamp": "2026-05-22T16:00:00Z",
  "jogoCodigo": "FUT-2026-0103",
  "equipaCasa": "Benfica",
  "equipaFora": "Porto",
  "golosCasa": 2,
  "golosFora": 1,
  "estado": 3
}
```

---

## 6. Tópicos de streaming

Criar tópicos Kafka/Redpanda separados por domínio funcional.

Tópicos recomendados:

```text
jogos-events
apostas-events
pagamentos-events
analytics-events
alertas-events
```

### Responsabilidade de cada tópico

| Tópico | Conteúdo | Consumidores possíveis |
|---|---|---|
| `jogos-events` | Criação, atualização e finalização de jogos | Dashboard, AnalyticsWorker, AlertasWorker |
| `apostas-events` | Criação, cancelamento e resolução de apostas | AnalyticsWorker, AlertasWorker |
| `pagamentos-events` | Transações, prémios, reembolsos e saldos | Dashboard, AuditoriaWorker |
| `analytics-events` | Métricas já calculadas | DashboardAPI |
| `alertas-events` | Alertas gerados | DashboardAPI, WorkerNotificacoes |

---

## 7. Filas RabbitMQ

Criar filas para tarefas que não devem bloquear a resposta da API.

Filas recomendadas:

```text
fila-notificacoes
fila-relatorios
fila-alertas
fila-auditoria
fila-reprocessamento
fila-dead-letter
```

### Uso prático das filas

| Fila | Finalidade |
|---|---|
| `fila-notificacoes` | Simular envio de notificações quando uma aposta é resolvida |
| `fila-relatorios` | Gerar relatórios ou resumos em background |
| `fila-alertas` | Processar alertas sem bloquear a API |
| `fila-auditoria` | Guardar logs funcionais de ações importantes |
| `fila-reprocessamento` | Repetir operações que falharam temporariamente |
| `fila-dead-letter` | Guardar mensagens que falharam definitivamente |

---

## 8. Fluxos principais da Parte 2

### 8.1. Fluxo de atualização de jogo

```text
1. A Aplicação Geradora atualiza o jogo na Plataforma de Resultados.
2. A Plataforma de Resultados propaga a atualização para a Plataforma de Apostas.
3. A Plataforma de Apostas atualiza a BD.
4. A Plataforma de Apostas publica evento em `jogos-events`.
5. AnalyticsWorker consome o evento.
6. Dashboard é atualizado com novos dados.
7. AlertasWorker verifica se há alguma situação anómala.
```

### 8.2. Fluxo de criação de aposta

```text
1. Utilizador cria aposta na Plataforma de Apostas.
2. API valida jogo, saldo, tipo de aposta, odd e montante.
3. BD regista a aposta e debita saldo.
4. API publica evento `ApostaCriada` em `apostas-events`.
5. API envia mensagem para `fila-auditoria`.
6. AnalyticsWorker atualiza métricas de volume apostado.
7. Dashboard mostra novo volume em tempo quase real.
```

### 8.3. Fluxo de resolução de aposta

```text
1. Jogo passa para Finalizado.
2. Stored Procedure resolve apostas pendentes.
3. Trigger processa prémios ou reembolsos.
4. API publica evento `ApostaResolvida` em `apostas-events`.
5. API envia tarefa para `fila-notificacoes`.
6. WorkerNotificacoes simula envio de notificação.
7. AnalyticsWorker atualiza margem, ganhos e perdas.
```

### 8.4. Fluxo de alerta

```text
1. AnalyticsWorker deteta exposição financeira elevada num jogo.
2. É criado um evento `AlertaGerado`.
3. O evento é publicado em `alertas-events`.
4. Uma mensagem é enviada para `fila-alertas`.
5. WorkerAlertas escreve alerta em consola/log/API.
6. Dashboard apresenta o alerta.
```

---

## 9. Dashboard

Deve existir pelo menos um dashboard em tempo quase real.

Métricas mínimas recomendadas:

```text
Total apostado por jogo
Total apostado por competição
Número de apostas por minuto
Número de apostas pendentes
Número de apostas ganhas
Número de apostas perdidas
Número de apostas anuladas
Margem da plataforma
Exposição financeira por jogo
Número de alertas gerados
```

### Versão simples do dashboard

Criar uma página com cartões:

```text
Total apostado
Apostas por minuto
Jogo com maior exposição
Margem atual da plataforma
Número de alertas ativos
```

E uma tabela:

```text
Código do jogo | Equipas | Volume apostado | Exposição | Margem | Estado
```

---

## 10. Alertas

Criar pelo menos um mecanismo de alerta automático.

Alertas recomendados:

```text
Alerta se o volume apostado num jogo ultrapassar 1000 €
Alerta se a exposição financeira de um jogo ultrapassar 500 €
Alerta se houver mais de 20 apostas no mesmo resultado em menos de 1 minuto
Alerta se ocorrerem 3 falhas consecutivas num worker
Alerta se uma mensagem for enviada para a Dead Letter Queue
```

Exemplo de alerta:

```json
{
  "eventId": "9bc1ec43-9870-4562-9111-abcdef123456",
  "eventType": "AlertaGerado",
  "timestamp": "2026-05-22T15:45:00Z",
  "tipo": "EXPOSICAO_ELEVADA",
  "jogoCodigo": "FUT-2026-0103",
  "valor": 1250.00,
  "limiar": 1000.00,
  "mensagem": "O jogo FUT-2026-0103 ultrapassou o limite de exposição financeira."
}
```

---

## 11. Analytics

Criar uma camada analítica separada da base de dados transacional.

A ideia é evitar que o dashboard consulte diretamente as tabelas principais da Parte 1.

### Tabelas analíticas sugeridas

```text
MetricasJogo
MetricasCompeticao
MetricasApostasPorMinuto
Alertas
EventosProcessados
```

### Métricas a calcular

```text
Volume total apostado por jogo
Volume total apostado por competição
Número de apostas por minuto
Média móvel de apostas por minuto
Percentagem de apostas por tipo: 1, X, 2
Percentagem de apostas ganhas/perdidas/anuladas
Margem da plataforma por jogo
Máximo e mínimo de volume apostado numa janela temporal
```

---

## 12. Reprocessamento e robustez

A Parte 2 deve mostrar que o sistema aguenta falhas temporárias.

Implementar:

- Retries no RabbitMQ;
- Dead Letter Queue;
- Consumers Kafka/Redpanda com offset controlado;
- Registo de eventos processados;
- Idempotência básica para evitar duplicação de métricas;
- Logs claros dos workers.

### Exemplo de idempotência

Criar uma tabela `EventosProcessados` com:

```text
EventId
EventType
ProcessedAt
ConsumerName
```

Antes de processar um evento, o worker verifica se o `EventId` já foi processado. Se já foi, ignora.

---

## 13. Testes a demonstrar

Devem existir evidências de testes com carga, falhas ou reprocessamento.

### Teste 1 — Carga

Gerar muitas apostas rapidamente e verificar:

- Eventos publicados no Kafka/Redpanda;
- Mensagens processadas pelos workers;
- Dashboard atualizado;
- Sistema sem bloqueios.

### Teste 2 — Falha temporária

Procedimento:

```text
1. Parar o AnalyticsWorker.
2. Continuar a gerar jogos/apostas.
3. Confirmar que os eventos continuam no Kafka/Redpanda.
4. Voltar a ligar o AnalyticsWorker.
5. Confirmar que ele processa os eventos em atraso.
```

### Teste 3 — Retry

Procedimento:

```text
1. Forçar erro num Worker RabbitMQ.
2. Confirmar que a mensagem é reprocessada.
3. Confirmar que o número de tentativas aumenta.
```

### Teste 4 — Dead Letter Queue

Procedimento:

```text
1. Forçar erro permanente numa mensagem.
2. Deixar esgotar o número de retries.
3. Confirmar que a mensagem vai para `fila-dead-letter`.
```

### Teste 5 — Replay

Procedimento:

```text
1. Apagar métricas analíticas.
2. Reprocessar eventos históricos do Kafka/Redpanda.
3. Confirmar que as métricas são reconstruídas.
```

### Teste 6 — Containerização

Procedimento:

```text
1. Executar `docker compose up`.
2. Confirmar que APIs, SQL Server, RabbitMQ, Kafka/Redpanda e workers arrancam.
3. Executar fluxo completo de jogo, aposta, resultado, analytics e alerta.
```

---

## 14. Docker Compose

A solução deve ser executada em ambiente containerizado.

Serviços recomendados no `docker-compose.yml`:

```text
resultados-api
apostas-api
analytics-api
analytics-worker
alertas-worker
notificacoes-worker
sqlserver
rabbitmq
redpanda
redpanda-console
rabbitmq-management
```

Portas úteis:

```text
ResultadosAPI: 5001
ApostasAPI: 5002
AnalyticsAPI: 5003
RabbitMQ Management: 15672
Redpanda Console: 8080
SQL Server: 1433
```

---

## 15. Divisão por 3 pessoas

## Pessoa 1 — Streaming de eventos

Responsável por Kafka/Redpanda e eventos live.

### Tarefas

- Definir contratos JSON dos eventos;
- Criar `EventPublisher` em .NET;
- Integrar publicação de eventos na ResultadosAPI;
- Integrar publicação de eventos na ApostasAPI;
- Criar tópicos Kafka/Redpanda;
- Criar consumer simples para validar eventos;
- Implementar replay básico;
- Documentar a camada de streaming.

### Entregáveis

- Producers de eventos;
- Tópicos configurados;
- Exemplos JSON dos eventos;
- Logs de publicação/consumo;
- Secção do relatório sobre streaming.

---

## Pessoa 2 — Filas, workers e processamento assíncrono

Responsável por RabbitMQ e workers.

### Tarefas

- Configurar RabbitMQ;
- Criar filas principais;
- Criar Dead Letter Queue;
- Criar `WorkerNotificacoes`;
- Criar `WorkerAlertas`;
- Criar `WorkerAuditoria` ou `WorkerRelatorios`;
- Implementar retries;
- Implementar tratamento de erro;
- Testar falhas temporárias;
- Documentar a camada de filas.

### Entregáveis

- Workers funcionais;
- Filas RabbitMQ configuradas;
- Demonstração de retry;
- Demonstração de Dead Letter Queue;
- Secção do relatório sobre processamento assíncrono.

---

## Pessoa 3 — Dashboard, analytics, alertas, Docker e integração final

Responsável pela componente visível e pela integração final.

### Tarefas

- Criar `AnalyticsWorker`;
- Criar base/tabelas analíticas;
- Criar endpoints de métricas;
- Criar dashboard simples;
- Criar regras de alerta;
- Preparar Docker Compose;
- Integrar todos os serviços;
- Preparar testes finais;
- Recolher prints e evidências;
- Organizar relatório final.

### Entregáveis

- Dashboard funcional;
- AnalyticsWorker;
- AnalyticsAPI;
- Alertas automáticos;
- Docker Compose completo;
- Prints dos testes;
- Relatório final integrado.

---

## 16. Prioridades se houver pouco tempo

Se o tempo for limitado, implementar por esta ordem:

```text
1. Publicar eventos de jogos e apostas em Kafka/Redpanda.
2. Criar RabbitMQ com uma fila de notificações.
3. Criar Dead Letter Queue.
4. Criar AnalyticsWorker para calcular volume apostado por jogo.
5. Criar dashboard simples com 4 ou 5 métricas.
6. Criar pelo menos um alerta automático.
7. Criar Docker Compose.
8. Fazer testes com prints.
```

O mínimo aceitável para demonstrar a Parte 2 deve incluir:

- Uma fila RabbitMQ funcional;
- Um tópico Kafka/Redpanda funcional;
- Um producer de eventos;
- Um consumer de eventos;
- Um worker assíncrono;
- Um dashboard ou API de métricas;
- Um alerta automático;
- Um teste de falha/retry/reprocessamento;
- Execução com Docker Compose.

---

## 17. Estrutura sugerida para o relatório

```text
1. Introdução
2. Evolução da arquitetura da Parte 1 para a Parte 2
3. Arquitetura lógica da solução
4. Justificação da escolha das tecnologias
   4.1. RabbitMQ
   4.2. Kafka/Redpanda
5. Camada de filas de mensagens
6. Camada de streaming de eventos
7. Eventos produzidos e consumidos
8. Fluxos principais e assíncronos
9. Dashboard, alertas e analytics
10. Reprocessamento, retries e tolerância a falhas
11. Testes realizados
12. Containerização com Docker Compose
13. Limitações
14. Conclusão
```

---

## 18. Checklist final

Antes da entrega, confirmar:

- [ ] APIs da Parte 1 continuam funcionais;
- [ ] Eventos são publicados quando jogos/apostas mudam;
- [ ] Kafka/Redpanda recebe eventos;
- [ ] Pelo menos um consumer processa eventos;
- [ ] RabbitMQ recebe mensagens assíncronas;
- [ ] Pelo menos um worker processa mensagens;
- [ ] Existe retry;
- [ ] Existe Dead Letter Queue;
- [ ] Existe dashboard ou API de métricas;
- [ ] Existe pelo menos um alerta automático;
- [ ] Existem testes com carga, falha ou reprocessamento;
- [ ] Docker Compose levanta a infraestrutura;
- [ ] O relatório justifica as tecnologias;
- [ ] O relatório inclui prints/logs/evidências.

---

## 19. Ideia central para defender oralmente

A Parte 2 transforma a solução da Parte 1 numa arquitetura orientada a eventos.

Na Parte 1, os sistemas comunicam sobretudo por REST, Stored Procedures e Triggers.

Na Parte 2, os eventos passam a ser publicados e consumidos por vários componentes independentes. O Kafka/Redpanda serve para disseminar informação live e permitir replay. O RabbitMQ serve para tarefas assíncronas, retries e isolamento de operações secundárias. O dashboard e a camada de analytics demonstram que os dados passam a ser explorados em tempo quase real, sem sobrecarregar a base transacional.

Esta evolução melhora o desacoplamento, a resiliência, a escalabilidade e a capacidade de resposta da plataforma.
