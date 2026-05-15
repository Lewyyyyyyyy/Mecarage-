# Docker — MecaManage

Cette arborescence contient l’orchestration locale du projet.

## Services
- `postgres` : base de données principale
- `redis` : cache / messages
- `backend` : API ASP.NET Core
- `ia-service` : service FastAPI
- `n8n` : automatisation de workflows
- `nginx` : reverse proxy
- `prometheus`, `grafana`, `cadvisor` : supervision

## Démarrage
1. Copier l’exemple d’environnement :
   - `cp .env.example .env`
2. Renseigner les secrets dans `.env`
3. Lancer l’ensemble :
   - `docker compose --env-file .env -f docker-compose.yml up -d --build`

## Vérifications utiles
- `docker compose --env-file .env -f docker-compose.yml ps`
- `docker compose --env-file .env -f docker-compose.yml logs -f backend`
- `docker compose --env-file .env -f docker-compose.yml logs -f ia-service`

## Points d’accès
- NGINX : `http://localhost`
- API : `http://localhost:80/api/...`
- Swagger : `http://localhost:80/swagger/`
- IA : `http://localhost:8000` ou via NGINX `http://localhost/ia/`
- n8n : `http://localhost:5678`
- Prometheus : `http://localhost:9090`
- Grafana : `http://localhost:3000`

## Dépannage rapide
- Si `backend` reste en `unhealthy`, vérifier PostgreSQL et les variables `CONNECTION_STRING`, `JWT_SECRET`.
- Si `ia-service` reste en `unhealthy`, vérifier `GEMINI_API_KEY`.
- Si `nginx` ne démarre pas, vérifier que `backend` et `ia-service` sont joignables sur le réseau `mecamanage-net`.
- Sur Windows, `cadvisor` peut demander des droits Docker Desktop élevés ou être limité selon la version du moteur Docker.

