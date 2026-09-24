# RabbitMQ (local, via Docker) — Driver Trip Scheduler

Phase 1 introduces a local **RabbitMQ** message broker that runs in Docker.
Nothing in the app depends on it yet — this just gives us a running broker to
build against in later phases.

## Prerequisites
- Docker Desktop installed and running.

## Start the broker
```powershell
docker compose -f docker-compose.rabbitmq.yml up -d
```

## Stop the broker
```powershell
docker compose -f docker-compose.rabbitmq.yml down
```
Add `-v` to also delete stored messages/queues (the volume):
```powershell
docker compose -f docker-compose.rabbitmq.yml down -v
```

## Endpoints
| Purpose | Address |
|---|---|
| Management UI (browser) | http://localhost:15672 |
| AMQP (the app connects here) | localhost:5672 |

## Default credentials
| User | Password |
|---|---|
| `dts` | `dts_password` |

Override them by setting `RABBITMQ_USER` / `RABBITMQ_PASS` before `up`.

## Handy commands
```powershell
# Is it running / healthy?
docker ps --filter name=dts-rabbitmq

# Live logs
docker logs -f dts-rabbitmq

# List queues (once we create some in later phases)
docker exec dts-rabbitmq rabbitmqctl list_queues
```

## What each Docker concept means here
- **Image** (`rabbitmq:3.13-management`) — the read-only template for the broker.
- **Container** (`dts-rabbitmq`) — the running broker instance.
- **Volume** (`dts_rabbitmq_data`) — durable storage so messages survive restarts.
- **Network** (`dts-network`) — a private network so future containers can reach
  RabbitMQ by name.
