# 📊 Monitoring & Observability Guide

Complete monitoring setup for MecaRage infrastructure.

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│        Applications                      │
│  Backend | Frontend | IA Service        │
└────────────────┬────────────────────────┘
                 │ Metrics
                 ▼
┌─────────────────────────────────────────┐
│      Prometheus (Time-Series DB)        │
│    Collects metrics every 15s           │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────┐
│  Grafana (Visualization & Dashboards)   │
│  cAdvisor (Container Metrics)           │
│  Alert Manager                          │
└──────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### Access Monitoring Stack

```
Prometheus  : http://127.0.0.1:9090
Grafana     : http://127.0.0.1:3000
cAdvisor    : http://127.0.0.1:8081
```

**Note**: These are localhost-only for security. Change in `docker-compose.yml` if needed.

---

## 📈 Prometheus

### What it does
- Collects metrics from all services
- Stores time-series data
- Provides query language (PromQL)

### Access

```
http://localhost:9090
```

### Common Queries

```promql
# CPU Usage
rate(container_cpu_usage_seconds_total[5m])

# Memory Usage
container_memory_usage_bytes

# Network I/O
rate(container_network_receive_bytes_total[5m])

# API Response Time
histogram_quantile(0.95, rate(http_request_duration_ms[5m]))
```

### Configuration

Prometheus config at `docker/prometheus/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'docker'
    static_configs:
      - targets: ['cadvisor:8081']
```

---

## 📊 Grafana

### Default Login

```
URL  : http://localhost:3000
User : admin
Pass : admin
```

**⚠️ Change password immediately in production!**

### Setup Data Source

1. Left panel → Data Sources → Add data source
2. Select Prometheus
3. URL: `http://prometheus:9090`
4. Save & Test

### Create Dashboards

#### Dashboard 1: Container Metrics

1. Create Dashboard → Add Panel
2. Metrics:
   - CPU: `rate(container_cpu_usage_seconds_total[5m])`
   - Memory: `container_memory_usage_bytes / 1024 / 1024`
   - Network: `rate(container_network_receive_bytes_total[5m])`

#### Dashboard 2: Docker Stats

1. Import Community Dashboards
2. Search: "Docker Containers"
3. Import ID: 1860 (Node Exporter)

### Create Alerts

```yaml
groups:
  - name: mecarage
    rules:
      - alert: HighMemoryUsage
        expr: container_memory_usage_bytes > 800000000
        for: 5m
        annotations:
          summary: "High memory usage"
          description: "Container using > 800MB"
      
      - alert: HighCPUUsage
        expr: rate(container_cpu_usage_seconds_total[5m]) > 0.8
        for: 5m
        annotations:
          summary: "High CPU usage"
```

---

## 🐳 cAdvisor

### What it does
- Google's container monitoring tool
- Detailed metrics per container
- Real-time statistics

### Access

```
http://localhost:8081
```

### Metrics Provided

- Container CPU usage
- Memory consumption
- Network I/O
- Disk usage
- Uptime

---

## 📝 Application Logging

### View Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f mysql

# Last 100 lines
docker compose logs --tail=100 backend

# Follow with timestamps
docker compose logs -f --timestamps backend
```

### Log Levels

```env
LOG_LEVEL=Information  # Info/Warning/Error/Critical
DEBUG=false
```

### Persist Logs

Create `docker-compose.override.yml`:

```yaml
version: '3.9'

services:
  backend:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
        labels: "service=backend"
  
  frontend:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
        labels: "service=frontend"
  
  mysql:
    logging:
      driver: "json-file"
      options:
        max-size: "50m"
        max-file: "5"
        labels: "service=mysql"
```

---

## 🏥 Health Checks

### Backend Health

```bash
curl http://localhost:5000/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2026-05-16T10:30:00Z",
  "uptime": "2h 15m"
}
```

### MySQL Health

```bash
docker compose exec mysql mysqladmin ping -u root -p$MYSQL_ROOT_PASSWORD
```

### Check All Services

```bash
docker compose ps

# Output should show all "Up X minutes"
```

---

## 📈 Performance Metrics

### Key Metrics to Monitor

| Metric | Alert Threshold | Action |
|--------|-----------------|--------|
| CPU Usage | > 80% | Scale horizontally |
| Memory | > 85% | Increase limit |
| Disk | > 90% | Clean old logs |
| API Response | > 1s | Optimize queries |
| DB Connections | > 50 | Reduce pool size |
| Error Rate | > 5% | Check logs |

---

## 🔧 Advanced Setup

### Centralized Logging (ELK Stack)

```yaml
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.0.0
    environment:
      - discovery.type=single-node
  
  kibana:
    image: docker.elastic.co/kibana/kibana:8.0.0
    ports:
      - "5601:5601"
```

### Alerting

Setup Slack notifications:

```yaml
alerting:
  alertmanagers:
    - static_configs:
        - targets: ['alertmanager:9093']

  global:
    slack_api_url: 'https://hooks.slack.com/services/...'
```

---

## 📊 Dashboards to Create

### 1. System Overview
- CPU, Memory, Disk usage
- Network I/O
- Container uptime

### 2. Application Performance
- Response times
- Error rates
- Request throughput

### 3. Database
- Query performance
- Connection pool
- Replication lag

### 4. Network
- Bandwidth in/out
- Packet loss
- Latency

---

## 🚨 Alert Examples

### Too Much Memory

```promql
container_memory_usage_bytes{name="mecamanage-backend"}
> 1073741824  # 1GB
```

### Slow API Responses

```promql
histogram_quantile(0.95, http_request_duration_seconds_bucket)
> 1  # 1 second
```

### Database Down

```promql
up{job="mysql"} == 0
```

---

## 🔍 Troubleshooting

### Prometheus not scraping metrics

```bash
# Check Prometheus logs
docker compose logs prometheus

# Verify targets
curl http://localhost:9090/api/v1/targets
```

### Grafana not connecting to Prometheus

```bash
# Test connection from Grafana container
docker compose exec grafana curl http://prometheus:9090/-/healthy
```

### High memory usage

```bash
# Find memory hogs
docker stats --no-stream
```

---

## 📚 Resources

- Prometheus: https://prometheus.io/docs
- Grafana: https://grafana.com/docs
- cAdvisor: https://github.com/google/cadvisor
- Docker Metrics: https://docs.docker.com/config/containers/runmetrics/

---

## 🔐 Security

- Change Grafana default password
- Restrict Prometheus access (not exposed externally)
- Use authentication for external access
- Backup Grafana dashboards regularly

---

## 📞 Support

For issues:
1. Check logs: `docker compose logs -f`
2. Verify connectivity: `curl http://service:port`
3. Check docker stats: `docker stats`

