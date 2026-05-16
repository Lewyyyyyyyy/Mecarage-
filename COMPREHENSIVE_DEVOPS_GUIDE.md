# 📚 MecaRage Comprehensive DevOps Guide

Complete DevOps setup, deployment, and operations guide.

---

## 📖 Table of Contents

1. [Local Development](#local-development)
2. [Testing](#testing)
3. [Docker & Compose](#docker--compose)
4. [CI/CD Pipeline](#cicd-pipeline)
5. [Production Deployment](#production-deployment)
6. [Monitoring](#monitoring)
7. [Troubleshooting](#troubleshooting)
8. [Reference](#reference)

---

## 🛠️ Local Development

### Prerequisites

```bash
# Check versions
dotnet --version          # 9.0+
node --version            # 22+
python --version          # 3.11+
docker --version          # Latest
docker-compose --version  # Latest
```

### Setup

```bash
# Clone
git clone https://github.com/your-username/mecarage.git
cd mecarage

# Copy environment
cp .env.example .env

# Edit config (optional for local)
# nano .env
```

### Start Services

**Option 1: Development Mode (All in one)**

```bash
npm run dev
```

**Option 2: Individual Services**

```bash
# Terminal 1 - Backend
cd backend
dotnet run --project MecaManage.API

# Terminal 2 - Frontend
cd frontend
npm start

# Terminal 3 - IA Service
cd ia-service
source venv/bin/activate
uvicorn main:app --reload
```

**Option 3: Docker Compose (Recommended)**

```bash
docker compose up -d
docker compose logs -f
```

### Access Local

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5073 |
| Swagger | http://localhost:5073/swagger |
| IA Service | http://localhost:8000 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 |

---

## 🧪 Testing

### Backend Tests

```bash
cd backend

# Run all tests
dotnet test

# Run specific project
dotnet test MecaManage.Tests/

# With verbose output
dotnet test --verbosity detailed

# With coverage
dotnet test /p:CollectCoverageReport=true
```

### Frontend Tests

```bash
cd frontend

# Run tests
npm test

# Run with watch
npm test -- --watch

# Generate coverage
npm test -- --coverage

# Run specific test
npm test -- services/auth.service.spec.ts
```

### IA Service Tests

```bash
cd ia-service

# Activate venv
source venv/bin/activate  # or: .\venv\Scripts\activate

# Run tests
pytest tests/

# With coverage
pytest tests/ --cov=.

# Specific test
pytest tests/test_main_api.py::test_diagnose_returns_structured_response
```

### Test Results

All tests must pass before deployment:

```bash
✅ Backend: All tests passing
✅ Frontend: All tests passing
✅ IA Service: All tests passing
```

---

## 🐳 Docker & Compose

### Build Images Locally

```bash
# Backend
docker build -f backend/Dockerfile -t mecamanage-backend:local .

# Frontend
docker build -f frontend/Dockerfile -t mecamanage-frontend:local ./frontend
```

### Docker Compose Commands

```bash
# Start services
docker compose up -d

# Check status
docker compose ps

# View logs
docker compose logs -f backend

# Stop services
docker compose down

# Remove all data
docker compose down -v

# Rebuild images
docker compose build --no-cache

# View resource usage
docker stats
```

### Services

```yaml
# Defined services in docker-compose.yml
mysql        - Database (port 3306)
backend      - API (port 5000)
frontend     - Web (port 3000)
redis        - Cache (port 6379)
nginx        - Reverse proxy (ports 80/443)
prometheus   - Metrics (port 9090)
grafana      - Dashboard (port 3000)
cadvisor     - Docker metrics (port 8081)
```

---

## 🚀 CI/CD Pipeline

### Workflow

```
1. Code Push to main
   ↓
2. GitHub Actions Triggered
   ├─ Test Backend
   ├─ Test Frontend
   ├─ Test IA Service
   ├─ Code Quality Check
   └─ Build & Push Docker
      ├─ Docker Hub
      └─ GitHub Container Registry
   ↓
3. Deploy (Optional - requires secrets)
```

### Monitor Pipeline

```bash
# Go to Actions tab
https://github.com/your-username/mecarage/actions

# Watch live logs
# Click on workflow → click on job
```

### Pipeline Stages

**Test Backend** (~5 min)
```
✓ Checkout
✓ Setup .NET 9
✓ Restore dependencies
✓ Build Release
✓ Run tests
```

**Test Frontend** (~3 min)
```
✓ Checkout
✓ Setup Node.js 22
✓ Install dependencies
✓ Run linter
✓ Run tests
✓ Build production
```

**Test IA Service** (~2 min)
```
✓ Checkout
✓ Setup Python 3.11
✓ Install dependencies
✓ Run tests
```

**Build & Push Docker** (~10 min)
```
✓ Build Backend image
✓ Push to DockerHub
✓ Push to GHCR
✓ Build Frontend image
✓ Push to DockerHub
✓ Push to GHCR
```

**Deploy** (Optional)
```
✓ SSH into server
✓ Pull latest images
✓ Update services
✓ Clean up
```

---

## 🌍 Production Deployment

See: [SETUP_PRODUCTION.md](SETUP_PRODUCTION.md)

### Quick Summary

```bash
# 1. Setup server
sudo apt install -y docker.io docker-compose

# 2. Clone & configure
git clone https://github.com/your-username/mecarage.git
cd mecarage
cp .env.example .env.prod
# Edit .env.prod

# 3. Configure SSL
sudo certbot certonly --standalone -d your-domain.com

# 4. Deploy
docker compose -f docker-compose.yml --env-file .env.prod up -d

# 5. Access
# https://your-domain.com
```

---

## 📊 Monitoring

See: [MONITORING.md](MONITORING.md)

### Quick Access

```
Prometheus  : http://localhost:9090
Grafana     : http://localhost:3000
cAdvisor    : http://localhost:8081
```

### Example Queries

```promql
# CPU usage last 5 minutes
rate(container_cpu_usage_seconds_total[5m])

# Memory usage in GB
container_memory_usage_bytes / 1024 / 1024 / 1024

# Uptime
container_start_time_seconds
```

---

## 🔧 Configuration

### Environment Variables

See: [.env.example](.env.example)

Key variables:

```env
# Database
MYSQL_ROOT_PASSWORD=iheb
MYSQL_DATABASE=mecarage

# JWT
Jwt__Secret=your_secure_key_here

# AI
GEMINI_API_KEY=your_api_key

# Docker
BACKEND_IMAGE=username/mecamanage-backend:latest
FRONTEND_IMAGE=username/mecamanage-frontend:latest
```

### Secrets (GitHub)

See: [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md)

Required:
- `DOCKERHUB_USERNAME`
- `DOCKERHUB_TOKEN`

Optional:
- `SONAR_TOKEN`
- `SERVER_HOST`, `SERVER_USER`, `SERVER_SSH_KEY`

---

## 🐛 Troubleshooting

### Tests Failing

```bash
# Backend
cd backend
dotnet clean
dotnet build -c Release
dotnet test --verbosity detailed

# Frontend
cd frontend
rm -rf node_modules
npm ci
npm test -- --run

# IA Service
cd ia-service
python -m venv venv
source venv/bin/activate
pip install -r requirements.txt
pytest tests/ -v
```

### Docker Build Fails

```bash
# View build output
docker build -f backend/Dockerfile -t test:latest . --progress=plain

# Check Dockerfile
cat backend/Dockerfile

# Check working directory
ls -la backend/
```

### Services Not Starting

```bash
# Check logs
docker compose logs backend

# Check health
docker compose ps

# Restart service
docker compose restart backend

# Check port conflicts
lsof -i :5000
lsof -i :3000
```

### Database Connection Error

```bash
# Check MySQL
docker compose exec mysql mysql -u root -p -e "SELECT 1"

# Check connection string
echo $CONNECTION_STRING

# Check network
docker network ls
docker network inspect mecarage-net
```

---

## 📋 Deployment Checklist

- [ ] All tests passing locally
- [ ] `.env` configured (don't commit!)
- [ ] Git repo ready to push
- [ ] GitHub Secrets configured
- [ ] Code pushed to main branch
- [ ] Pipeline executed successfully
- [ ] Images in DockerHub/GHCR
- [ ] Production server ready (if deploying)
- [ ] SSL certificate configured
- [ ] Backups configured
- [ ] Monitoring setup
- [ ] Health checks passing

---

## 🔗 Reference

### Important Files

| File | Purpose |
|------|---------|
| `.github/workflows/ci.yml` | Pipeline configuration |
| `.env.example` | Environment template |
| `docker-compose.yml` | Docker services definition |
| `backend/Dockerfile` | Backend image build |
| `frontend/Dockerfile` | Frontend image build |
| `sonarcloud.yml` | Code quality config |
| `MONITORING.md` | Monitoring guide |
| `SETUP_PRODUCTION.md` | Production setup |

### Commands Quick Reference

```bash
# Development
npm run dev                    # All services
dotnet run --project MecaManage.API  # Backend only
ng serve                       # Frontend only
uvicorn main:app --reload     # IA Service only

# Testing
npm test                       # All tests
dotnet test backend/           # Backend tests
cd frontend && npm test        # Frontend tests
cd ia-service && pytest tests/ # IA tests

# Docker
docker compose up -d           # Start
docker compose down            # Stop
docker compose logs -f         # View logs
docker compose ps              # Status
docker image ls                # List images

# CI/CD
git add .
git commit -m "message"
git push origin main           # Trigger pipeline

# Production
docker compose -f docker-compose.yml --env-file .env.prod up -d
docker compose logs -f
docker stats
```

### URLs

| Service | Local | Production |
|---------|-------|-----------|
| Frontend | http://localhost:4200 | https://your-domain.com |
| API | http://localhost:5073 | https://your-domain.com:5073 |
| Swagger | http://localhost:5073/swagger | https://your-domain.com:5073/swagger |
| Prometheus | http://localhost:9090 | http://127.0.0.1:9090 |
| Grafana | http://localhost:3000 | http://127.0.0.1:3000 |

---

## 📞 Support Documents

- [SETUP_PRODUCTION.md](SETUP_PRODUCTION.md) - Production deployment
- [MONITORING.md](MONITORING.md) - Monitoring & observability
- [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) - GitHub Actions secrets
- [README.md](README.md) - Project overview
- [DOCKER.md](DOCKER.md) - Docker reference

---

## 🎓 Learning Resources

- [Docker Docs](https://docs.docker.com)
- [GitHub Actions](https://docs.github.com/en/actions)
- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core)
- [Angular](https://angular.dev)
- [Prometheus](https://prometheus.io/docs)

---

**Last Updated**: 2026-05-16
**Version**: 1.0
**Maintainer**: DevOps Team

