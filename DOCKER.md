# MecaManage - Déploiement Docker

## Configuration

### 1. Cloner le repository
```bash
git clone https://github.com/yourusername/mecarage.git
cd mecarage
```

### 2. Configurer les variables d'environnement
```bash
cp .env.example .env
```

Modifiez `.env` avec vos valeurs :
```env
# MySQL
MYSQL_ROOT_PASSWORD=votre_mdp_securise
CONNECTION_STRING="Server=mysql;Port=3306;Database=mecarage;Uid=root;Pwd=votre_mdp_securise;"

# JWT (Générez une clé sécurisée)
JWT_SECRET=$(openssl rand -base64 32)

# Images Docker
BACKEND_IMAGE=yourdockerhub/mecamanage-backend:latest
FRONTEND_IMAGE=yourdockerhub/mecamanage-frontend:latest
```

### 3. Lancer l'application

**Mode développement :**
```bash
docker-compose -f docker/docker-compose.yml up -d
```

**Mode production (from repository root) :**
```bash
docker-compose up -d
```

## Accès aux services

- **Frontend** : http://localhost
- **Backend API** : http://localhost:5000
- **Swagger UI** : http://localhost:5000/swagger/ui

## Commandes utiles

```bash
# Voir les logs
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f mysql

# Arrêter l'application
docker-compose down

# Supprimer les données
docker-compose down -v

# Reconstruire les images
docker-compose build --no-cache

# Vérifier la santé des services
docker-compose ps
```

## Problèmes courants

### MySQL refuse la connexion
- Vérifiez le password dans `.env`
- Attendez 30 secondes pour que MySQL démarre
- Vérifiez les logs : `docker-compose logs mysql`

### Backend ne démarre pas
- Vérifiez la CONNECTION_STRING dans `.env`
- Vérifiez que MySQL est sain : `docker-compose ps`
- Consultez les logs : `docker-compose logs backend`

### Frontend affiche une page blanche
- Vérifiez que le backend est en cours d'exécution
- Vérifiez la console du navigateur pour les erreurs
- Consultez les logs nginx : `docker-compose logs nginx`

## CI/CD avec GitHub Actions

Le workflow automatique :
1. ✅ Teste le backend (.NET)
2. ✅ Teste le frontend (Angular)
3. 🐳 Construit et pousse les images Docker
4. 🚀 Déploie sur le serveur (optionnel)

### Secrets GitHub requis
- `DOCKERHUB_USERNAME` : Votre username DockerHub
- `DOCKERHUB_TOKEN` : Votre token DockerHub
- `GHCR_TOKEN` : Votre token GitHub
- `SERVER_HOST` : IP du serveur (optionnel)
- `SERVER_USER` : Utilisateur SSH (optionnel)
- `SERVER_SSH_KEY` : Clé SSH (optionnel)

## Sécurité

⚠️ **NE JAMAIS** commiter le fichier `.env` - utiliser `.env.example`

Bonnes pratiques :
```bash
# Générer des clés sécurisées
openssl rand -base64 32

# Utiliser des secrets GitHub pour les valeurs sensibles
# Ne pas stocker les mots de passe en clair
```

## Maintenance

```bash
# Nettoyer les images inutilisées
docker image prune -a

# Nettoyer les volumes inutilisés
docker volume prune

# Mettre à jour les images
docker-compose pull
docker-compose up -d
```

