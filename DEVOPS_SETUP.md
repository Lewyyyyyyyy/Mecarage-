# 🔧 MecaManage - Configuration DevOps Complète

## 📚 Documentation Guide

Cette section décrit comment configurer le système DevOps complet de MecaManage, y compris les tests, Docker, et la CI/CD.

### 📖 Documents Importants

1. **[DOCKER.md](DOCKER.md)** - Guide complet de déploiement Docker
   - Configuration locale
   - Commandes utiles
   - Troubleshooting

2. **[SETUP_GITHUB_DOCKERHUB.md](SETUP_GITHUB_DOCKERHUB.md)** - Configuration GitHub & DockerHub
   - Créer les secrets GitHub
   - Configurer DockerHub
   - Pousser le code
   - Monitorig de la pipeline

---

## ⚡ Démarrage Rapide (5 minutes)

### Prérequis
```bash
✓ .NET 9 SDK
✓ Node.js 18+
✓ Docker & Docker Compose
✓ Git
```

### 1. Cloner et configurer
```bash
git clone https://github.com/votre-username/mecarage.git
cd mecarage
cp .env.example .env
```

### 2. Configurer les variables (`.env`)
```env
MYSQL_ROOT_PASSWORD=iheb
JWT_SECRET=your-super-secret-key-32-chars-minimum
BACKEND_IMAGE=yourusername/mecamanage-backend:latest
FRONTEND_IMAGE=yourusername/mecamanage-frontend:latest
```

### 3. Tester localement
```bash
# Tests Backend
dotnet test backend/MecaManage.Tests/

# Tests Frontend
npm test --prefix frontend/

# Lancer l'application
docker-compose up -d
```

### 4. Vérifier
```bash
docker-compose ps          # Voir le status
docker-compose logs -f     # Voir les logs
```

---

## 🧪 Tests

### Backend (C# / xUnit)
```bash
cd backend
dotnet test MecaManage.Tests/
```

**Tests inclus** :
- ✅ 5 tests GarageService
- ✅ 7 tests UserService
- ✅ 5 tests divers
- **Total** : 17 tests

### Frontend (Angular / Jasmine)
```bash
npm test --prefix frontend/
```

**Tests** :
- Admin Dashboard
- Components
- Services
- etc.

---

## 🐳 Docker

### Build local
```bash
# Backend
docker build -f backend/Dockerfile -t mecamanage-backend:local .

# Frontend
docker build -f frontend/Dockerfile -t mecamanage-frontend:local ./frontend
```

### Run avec docker-compose
```bash
docker-compose up -d
docker-compose ps
docker-compose logs -f
```

### Services disponibles
- **Frontend** : http://localhost (port 3000)
- **Backend API** : http://localhost:5000
- **Swagger** : http://localhost:5000/swagger/ui
- **MySQL** : localhost:3306

---

## 🚀 CI/CD Pipeline (GitHub Actions)

### Workflow automatique
Quand vous poussez sur `main` :

```
1️⃣ test-backend (∼5 min)
   └─ dotnet test
   └─ dotnet build Release
   
2️⃣ test-frontend (∼3 min)
   └─ npm test
   └─ npm run build
   
3️⃣ docker-build (∼10 min)
   └─ Build & push Backend image
   └─ Build & push Frontend image
   
✅ Total : ∼18-20 minutes par push
```

### Voir la pipeline
```
https://github.com/votre-username/mecarage/actions
```

---

## 📋 Checklist Configuration

### GitHub
- [ ] Créer repository sur GitHub
- [ ] Configurer 3 secrets (DOCKERHUB_USERNAME, DOCKERHUB_TOKEN, GITHUB_TOKEN)
- [ ] Vérifier que `.github/workflows/ci.yml` existe

### DockerHub
- [ ] Créer compte DockerHub
- [ ] Créer 2 repositories : `mecamanage-backend`, `mecamanage-frontend`
- [ ] Générer Access Token

### Local Development
- [ ] `.env.example` copié en `.env`
- [ ] Variables d'environnement configurées
- [ ] `dotnet test` passe ✓
- [ ] `npm test` passe ✓
- [ ] `docker-compose up` fonctionne ✓

### Git
- [ ] Ajouter tous les fichiers
- [ ] Commit : `git commit -m "feat: devops setup"`
- [ ] Push : `git push origin main`

---

## 🔐 Sécurité

### Secrets GitHub
```bash
# JAMAIS commiter les secrets!
echo ".env" >> .gitignore
git add .gitignore
git commit -m "fix: ignore .env file"
```

### Secrets à protéger
```
MYSQL_ROOT_PASSWORD
JWT_SECRET
DOCKERHUB_TOKEN
```

### Bonnes pratiques
```bash
# Générer une clé sécurisée
openssl rand -base64 32

# Utiliser des secrets GitHub pour les valeurs sensibles
# Ne pas stocker en clair
```

---

## 📊 Architecture

```
                    ┌─────────────────┐
                    │   GitHub Repo   │
                    └────────┬────────┘
                             │ git push
                             ▼
                    ┌─────────────────┐
                    │ GitHub Actions  │
                    │   CI/CD Pipeline│
                    └────────┬────────┘
                             │
                 ┌───────────┼───────────┐
                 │           │           │
            ┌────▼──┐  ┌────▼──┐  ┌────▼──┐
            │ Test  │  │ Test  │  │ Build │
            │Backend│  │Frontend│  │Images │
            └───────┘  └───────┘  └────┬──┘
                                        │
                         ┌──────────────┼──────────────┐
                         │              │              │
                    ┌────▼──────┐ ┌────▼──────┐ ┌────▼──────┐
                    │ DockerHub  │ │   GHCR    │ │ Server    │
                    │ Registry   │ │ Registry  │ │ Deployment│
                    └────────────┘ └───────────┘ └───────────┘
```

---

## 🛠️ Troubleshooting

### Tests échouent
```bash
# Vérifier les logs
dotnet test backend/MecaManage.Tests/ --verbosity detailed

# Nettoyer et relancer
dotnet clean backend/
dotnet build backend/
dotnet test backend/MecaManage.Tests/
```

### Docker build échoue
```bash
# Vérifier le Dockerfile
docker build -f backend/Dockerfile -t test:latest .

# Vérifier les permissions
chmod +x backend/Dockerfile
```

### Images ne poussent pas
```bash
# Vérifier la connexion DockerHub
docker login

# Vérifier les secrets GitHub
# Settings > Secrets and variables > Actions
```

---

## 📞 Support

### Ressources
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [DockerHub Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Testing Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/)

### Logs & Debugging
```bash
# Logs locaux
docker-compose logs -f service-name

# GitHub Actions logs
# https://github.com/votre-username/mecarage/actions

# Build logs
docker build --progress=plain -f backend/Dockerfile .
```

---

## ✨ Prochaines étapes

1. ✅ Tests configurés et passants
2. ✅ Docker setup complet
3. ✅ CI/CD pipeline prête
4. 📋 **À faire** : Configurer les secrets GitHub
5. 📋 **À faire** : Pousser le code
6. 📋 **À faire** : Vérifier la pipeline
7. 📋 **À faire** : Configurer SSL/HTTPS
8. 📋 **À faire** : Ajouter monitoring et logs

---

## 📝 Fichiers modifiés/créés

```
✓ backend/Dockerfile
✓ backend/MecaManage.Tests/Features/GarageServiceTests.cs
✓ backend/MecaManage.Tests/Features/UserServiceTests.cs
✓ backend/MecaManage.Tests/Helpers/DatabaseHelper.cs
✓ .github/workflows/ci.yml (amélioré)
✓ docker-compose.yml (root)
✓ .env.example
✓ .gitignore (amélioré)
✓ DOCKER.md
✓ SETUP_GITHUB_DOCKERHUB.md
✓ verify-setup.sh
✓ verify-setup.bat
```

---

## 🎯 Résumé

Vous avez maintenant :

✅ **17 tests** pour le backend
✅ **Tests Angular** pour le frontend
✅ **Docker Compose** complet (production-ready)
✅ **CI/CD pipeline** avec GitHub Actions
✅ **Push automatique** des images Docker
✅ **Documentation** complète

**Prochaine étape** : Configurez les secrets GitHub et poussez le code!

```bash
git add .
git commit -m "feat: complete devops setup with tests and CI/CD"
git push origin main
```

---

**Créé le** : 2026-05-15
**Par** : GitHub Copilot
**Version** : 1.0


