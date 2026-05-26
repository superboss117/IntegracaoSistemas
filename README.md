# BetStrike - Evidências e Demonstração da Parte 2 (Integração de Sistemas)

Este documento descreve o funcionamento, a topologia de rede, os testes automatizados e o roteiro recomendado para demonstrar o sistema ao professor.

---

## 🛠️ Portas e Serviços Disponíveis

Após levantar a infraestrutura com Docker Compose, os seguintes serviços estarão disponíveis:

| Serviço | URL / Porta | Descrição |
| :--- | :--- | :--- |
| **ResultadosApi** | [http://localhost:5001/swagger](http://localhost:5001/swagger) | Registo de jogos e resultados (Parte 1) |
| **ApostasApi** | [http://localhost:5002/swagger](http://localhost:5002/swagger) | Gestão de apostas e utilizadores (Parte 1) |

| **AnalyticsAPI** | [http://localhost:5003/swagger](http://localhost:5003/swagger) | API REST para métricas analíticas e alertas (Parte 2) |
| **Analytics Dashboard** | [http://localhost:5003/dashboard](http://localhost:5003/dashboard) | Interface Gráfica interativa de monitorização (Parte 2) |

| **Redpanda Console** | [http://localhost:8080](http://localhost:8080) | Painel visual para exploração de tópicos Kafka |
| **RabbitMQ Management** | [http://localhost:15672](http://localhost:15672) | Consola administrativa do broker de mensagens RabbitMQ |

---

## 🚀 Como Executar o Sistema Completo

Para iniciar todo o ecossistema (Base de Dados, Corretores de Mensagens, APIs e Workers), executa o seguinte comando na raiz do projeto:

```bash
docker compose up --build -d
```

---

## 📊 Roteiro Recomendado de Demonstração (Guião para Apresentação)

Segue a sequência lógica recomendada para apresentar as funcionalidades de integração de sistemas ao professor:

### Passo 1: Mostrar a Saudabilidade (Healthchecks)
*   Acede aos endpoints `/health` de cada API para comprovar que estão ativos e saudáveis:
    *   `GET http://localhost:5001/health`
    *   `GET http://localhost:5002/health`
    *   `GET http://localhost:5003/health`

### Passo 2: O Dashboard de Analytics
*   Abre no browser: **[http://localhost:5003/dashboard](http://localhost:5003/dashboard)**
*   Verifica que os contadores e a tabela começam a 0 (ou com os dados acumulados anteriormente). O dashboard atualiza-se de forma 100% dinâmica a cada **5 segundos** sem necessidade de refrescar a página.

### Passo 3: Ingestão de Dados e Disparo de Evento Kafka
*   Utiliza a Swagger da **ResultadosApi (5001)** para criar um novo jogo.
*   Utiliza a Swagger da **ApostasApi (5002)** para colocar uma aposta pequena (ex: 10€) para o utilizador 1.
*   **Demonstração Visual no Redpanda:** Abre o **Redpanda Console (8080)** e mostra a mensagem estruturada `ApostaCriadaEvent` a entrar no tópico `apostas-events`.
*   **Demonstração de Consumo (Analytics):** Mostra que o Dashboard de Analytics atualizou instantaneamente o contador "Total Apostado" para 10€ e adicionou o jogo correspondente à tabela detalhada.

### Passo 4: O Corretor Assíncrono (RabbitMQ Auditoria)
*   Abre o **RabbitMQ Management (15672)** (guest/guest) e mostra os gráficos de atividade.
*   Mostra os logs do container `workersrabbitmq`:
    ```bash
    docker logs workersrabbitmq
    ```
    Comprova que a auditoria da aposta foi processada assincronamente em background:
    `[Auditoria] Received: Action=CriarAposta, Details=Aposta XXX criada...`

### Passo 5: Teste Automático de Segurança (Limite de Risco)
*   Faz uma aposta de valor elevado (ex: 1050€) para disparar as regras de exposição de limite (> 1000€).
*   Mostra que:
    1.  Um alerta `EXPOSICAO_ELEVADA` de nível `CRITICO` surge imediatamente no Dashboard de Analytics.
    2.  O `WorkerAlertas` do RabbitMQ recebe o comando e avisa na consola: `[Alertas] Alert received! Level: CRITICO...`
    3.  O Kafka emite um evento no tópico `alertas-events`.

---

## 📈 Resiliência e Desacoplamento (Demonstração de Força)

Para provar que o sistema é resiliente e tolerante a falhas:
1.  Para o container `analyticsworker`:
    ```bash
    docker compose stop analyticsworker
    ```
2.  Cria mais duas apostas na ApostasApi. Nota que o Dashboard não atualiza (porque o motor analítico está offline), mas **as apostas não são perdidas e a API não crasha**.
3.  Volta a ligar o worker:
    ```bash
    docker compose start analyticsworker
    ```
4.  Verifica que o worker recupera instantaneamente os eventos em espera no Kafka e atualiza o Dashboard de forma retroativa, mantendo a consistência.

---

## 🧪 Pipeline de Testes Automatizados

O repositório inclui scripts de teste robustos localizados em `tests/manual/` que executam validações de ponta a ponta sem necessidade de interações manuais:

*   `./tests/manual/validar_healthchecks.sh`: Valida a resposta das 3 APIs.
*   `./tests/manual/validar_demo_completa.sh`: Valida a criação de jogo, sincronização, aposta, transmissão no Kafka e gravação no dashboard.
*   `./tests/manual/validar_carga.sh`: Avalia a performance enviando apostas em rajada.
*   `./tests/manual/validar_reprocessamento.sh`: Testa a resiliência desligando o worker analítico.
*   `./tests/manual/validar_alertas.sh`: Força o limite de 1000€ e valida o disparo sincronizado no Kafka/RabbitMQ.

Para correr toda a suite de testes e validar o sistema de uma só vez, executa:
```bash
./tests/manual/validar_sistema.sh
```
