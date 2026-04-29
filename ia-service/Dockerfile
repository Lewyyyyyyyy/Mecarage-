# ── Étape 1 : image de base légère Python ──────────────────────────────────
FROM python:3.11-slim AS base

# Définir le répertoire de travail dans le conteneur
WORKDIR /app

# Copier uniquement requirements.txt d'abord (optimisation cache Docker)
COPY requirements.txt .

# Installer les dépendances Python
RUN pip install --no-cache-dir -r requirements.txt

# ── Étape 2 : copier le code source ────────────────────────────────────────
COPY . .

# Exposer le port 8000 (FastAPI)
EXPOSE 8000

# Lancer l'application avec uvicorn
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]