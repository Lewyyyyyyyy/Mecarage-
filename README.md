# 🔧 MecaRage — Garage Management Platform

Full-stack garage management system with AI-powered diagnostics, repair lifecycle tracking, invoicing, and real-time notifications.

---

## 📐 Architecture Overview

```
MecaRage/
├── frontend/        Angular 21 (Tailwind CSS 4)
├── backend/         ASP.NET Core 9 — Clean Architecture (CQRS + MediatR)
├── ia-service/      FastAPI + Google Gemini RAG diagnosis engine
├── docker/          Docker Compose (MySQL · Redis · n8n · Nginx · Prometheus · Grafana)
└── n8n/             Workflow automation
```

---

## ✅ Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| **Node.js** | 22 LTS | https://nodejs.org |
| **Angular CLI** | 21 | `npm install -g @angular/cli@21` |
| **.NET SDK** | 9.0 | https://dotnet.microsoft.com/download/dotnet/9.0 |
| **MySQL** | 8.0+ | https://dev.mysql.com/downloads/mysql/ |
| **Redis** | 7+ | https://redis.io/docs/getting-started/ (or via Docker) |
| **Python** | 3.11+ | https://www.python.org/downloads/ |
| **Docker + Docker Compose** | Latest | https://www.docker.com/products/docker-desktop _(optional, for full stack)_ |

---

## 1 — MySQL Setup

### Install MySQL 8

**Windows:**
1. Download MySQL Installer from https://dev.mysql.com/downloads/installer/
2. Run the installer, choose **Developer Default**
3. Set root password to `root` (or customize and update `appsettings.Development.json`)

**macOS (Homebrew):**
```bash
brew install mysql
brew services start mysql
mysql_secure_installation
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt update && sudo apt install mysql-server
sudo systemctl start mysql
sudo mysql_secure_installation
```

### Create the database
```sql
-- Connect as root
mysql -u root -p

-- Create database and user (or just use root for dev)
CREATE DATABASE mecamanage CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'mecamanage_user'@'localhost' IDENTIFIED BY 'pass123';
GRANT ALL PRIVILEGES ON mecamanage.* TO 'mecamanage_user'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

> The `appsettings.Development.json` uses `root/root` by default for simplicity.

---

## 2 — Redis Setup

**Windows** (easiest — via Docker):
```bash
docker run -d -p 6379:6379 --name redis redis:7-alpine
```

**macOS:**
```bash
brew install redis
brew services start redis
```

**Linux:**
```bash
sudo apt install redis-server
sudo systemctl start redis
```

---

## 3 — Backend (.NET 9)

### Install .NET 9 SDK
Download from: https://dotnet.microsoft.com/download/dotnet/9.0

Verify:
```bash
dotnet --version  # should print 9.x.x
```

### Install EF Core CLI tools
```bash
dotnet tool install --global dotnet-ef
```

### Configure the connection string
Edit `backend/MecaManage.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=mecamanage;User=root;Password=root",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "MecaManage_Super_Secret_Key_2026_MinLength32Chars!",
    "Issuer": "MecaManage",
    "Audience": "MecaManageClients",
    "ExpiryMinutes": "60"
  }
}
```

### Restore & run migrations
```bash
cd backend

# Restore all packages
dotnet restore

# Apply all migrations (creates all tables)
dotnet ef database update --project MecaManage.Infrastructure --startup-project MecaManage.API

# Run the API
dotnet run --project MecaManage.API
```

The API will start on **http://localhost:5073**
Swagger UI: **http://localhost:5073/swagger**

A **Super Admin** account is auto-created on first run:
- Email: `admin@mecamanage.local`
- Password: `SuperAdmin@123`

---

## 4 — Frontend (Angular 21)

### Install Node.js 22 LTS
Download from: https://nodejs.org/en/download

Verify:
```bash
node --version   # v22.x.x
npm --version    # 10+
```

### Install Angular CLI 21
```bash
npm install -g @angular/cli@21
```

### Install dependencies & run
```bash
cd frontend
npm install
npm start
```

The app opens at **http://localhost:4200**

#### Environment configuration
`frontend/src/environments/environment.ts` — update `apiBaseUrl` if needed:
```ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5073/api',
  aiServiceUrl: 'http://localhost:8000'
};
```

---

## 5 — AI Diagnosis Service (Python + FastAPI)

### Install Python 3.11+
Download from: https://www.python.org/downloads/

Verify:
```bash
python --version  # 3.11+
```

### Setup virtual environment
```bash
cd ia-service

# Create venv
python -m venv venv

# Activate (Windows)
venv\Scripts\activate

# Activate (macOS/Linux)
source venv/bin/activate

# Install dependencies
pip install -r requirements.txt
```

### Configure environment variables
Copy the example file:
```bash
# Windows
copy .env.example .env

# macOS/Linux
cp .env.example .env
```

Edit `.env` and fill in your Google Gemini API key:
```
GEMINI_API_KEY=your_google_ai_studio_key_here
```

> Get a free API key at: https://aistudio.google.com/app/apikey

### Run the service
```bash
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

API docs: **http://localhost:8000/docs**

---

## 6 — Full Stack with Docker Compose

This is the easiest way to run everything together.

### Setup environment file
```bash
# From project root
cd docker

# Windows
copy .env.example .env

# macOS/Linux
cp .env.example .env
```

Edit `docker/.env` and fill in the required values (especially `GEMINI_API_KEY`).

### Start all services
```bash
cd docker
docker compose up -d
```

### Service URLs
| Service | URL |
|---------|-----|
| Frontend (via Nginx) | http://localhost |
| Backend API | http://localhost:5073 |
| AI Service | http://localhost:8000 |
| Swagger | http://localhost:5073/swagger |
| n8n Workflows | http://localhost:5678 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 |

### Stop all services
```bash
docker compose down
```

### Reset all data (destructive)
```bash
docker compose down -v
```

---

## 7 — Development Workflow (All Services at Once)

Open 3 terminals:

**Terminal 1 — Backend:**
```bash
cd backend
dotnet run --project MecaManage.API
```

**Terminal 2 — Frontend:**
```bash
cd frontend
npm start
```

**Terminal 3 — AI Service:**
```bash
cd ia-service
venv\Scripts\activate   # or: source venv/bin/activate
uvicorn main:app --reload --port 8000
```

Or use the Turborepo dev script from root:
```bash
npm run dev
```

---

## 8 — Running Tests

### Backend (xUnit)
```bash
cd backend
dotnet test
```

### Frontend (Vitest)
```bash
cd frontend
npm test
```

### AI Service (pytest)
```bash
cd ia-service
pytest tests/
```

---

## 9 — EF Core Migrations (for contributors)

If you change domain entities, create a new migration:
```bash
cd backend

# Build first
dotnet build MecaManage.Infrastructure
dotnet build MecaManage.API

# Add migration
dotnet ef migrations add YourMigrationName \
  --project MecaManage.Infrastructure \
  --startup-project MecaManage.API \
  --no-build

# Apply
dotnet ef database update \
  --project MecaManage.Infrastructure \
  --startup-project MecaManage.API \
  --no-build
```

---

## 🔐 Default Accounts (Development)

| Role | Email | Password |
|------|-------|----------|
| Super Admin | `admin@mecamanage.local` | `SuperAdmin@123` |

> Additional accounts (GarageAdmin, ChefAtelier, Mechanic, Client) are created via the Super Admin dashboard.

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21, Tailwind CSS 4, TypeScript 5.9 |
| Backend | ASP.NET Core 9, EF Core 9, MediatR 14, CQRS |
| Database | MySQL 8 (dev) / PostgreSQL 16 (prod via Docker) |
| Cache | Redis 7 |
| AI Service | Python 3.11, FastAPI, Google Gemini, BM25 RAG |
| PDF Generation | QuestPDF 2026 |
| Auth | JWT Bearer tokens |
| Automation | n8n |
| Monitoring | Prometheus + Grafana + cAdvisor |
| Containerization | Docker + Docker Compose |

---

## ❓ Troubleshooting

### `dotnet ef` not found
```bash
dotnet tool install --global dotnet-ef
# Restart terminal after install
```

### MySQL connection refused
Make sure MySQL is running:
```bash
# Windows Services → MySQL80 → Start
# macOS: brew services start mysql
# Linux: sudo systemctl start mysql
```

### Port 5073 already in use
```bash
# Find and kill the process using the port
netstat -ano | findstr :5073
taskkill /PID <PID> /F
```

### Angular build fails with `NG5002`
Arrow functions are not allowed directly in Angular templates. Move logic to a `computed()` property in the component class.

### Redis connection refused
Start Redis or use Docker:
```bash
docker run -d -p 6379:6379 redis:7-alpine
```

---

## Déploiement Docker
- L’environnement de production est piloté depuis `docker/docker-compose.yml`.
- Les variables réelles sont générées via `ansible/roles/deploy/templates/env.prod.j2`.
- En production, `nginx` reste la porte d’entrée publique ; `n8n`, `Prometheus`, `Grafana` et `cAdvisor` sont limités à `127.0.0.1`.
- Le job IA a été retiré de la CI/CD ; le pipeline ne teste et ne pousse plus que le backend.

### Points d’accès utiles
- API / Swagger : via `http://localhost` ou via un reverse proxy externe vers `nginx`
- IA : via `http://localhost/ia/`
- n8n : `http://127.0.0.1:5678`
- Prometheus : `http://127.0.0.1:9090`
- Grafana : `http://127.0.0.1:3000`
- cAdvisor : `http://127.0.0.1:8081`

### Variables d’environnement
| Nom | Description | Exemple |
|-----|-------------|---------|
| `MYSQL_ROOT_PASSWORD` | Mot de passe root MySQL | `root` |
| `MYSQL_DATABASE` | Nom de la base de données | `mecamanage` |
| `MYSQL_USER` | Nom d’utilisateur MySQL | `mecamanage_user` |
| `MYSQL_PASSWORD` | Mot de passe de l’utilisateur MySQL | `pass123` |
| `REDIS_PASSWORD` | Mot de passe Redis | `redispass` |
| `JWT_SECRET` | Clé secrète pour JWT | `MecaManage_Super_Secret_Key_2026_MinLength32Chars!` |
| `GEMINI_API_KEY` | Clé API pour Google Gemini | `votre_cle_api_google` |

### Commandes Docker Compose
```bash
# Démarrer en arrière-plan
docker compose up -d

# Voir les logs
docker compose logs -f

# Accéder au conteneur d’un service
docker exec -it <nom_du_conteneur> /bin/bash

# Arrêter les services
docker compose down
```
