#!/usr/bin/env pwsh
# Script de push automatisé pour MecaManage

Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  MecaManage - Push Automatisé vers GitHub" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Vérifier que nous sommes dans le bon dossier
if (!(Test-Path ".git")) {
    Write-Host "❌ Erreur: Pas dans un repository Git!" -ForegroundColor Red
    Write-Host "Allez dans: C:\Users\saafi\projetdev sem 2\Mecarage-" -ForegroundColor Yellow
    exit 1
}

# Demander les informations
Write-Host "📋 Configuration GitHub" -ForegroundColor Yellow
Write-Host ""

$username = Read-Host "Entrez votre username GitHub"
$repo = Read-Host "Entrez le nom du repository (ex: mecarage)"

# Vérifier si le remote existe
$remote = git remote -v 2>&1
if ($remote -match "origin") {
    Write-Host "✓ Remote 'origin' existe déjà" -ForegroundColor Green
} else {
    Write-Host "➕ Ajout du remote origin..." -ForegroundColor Cyan
    git remote add origin "https://github.com/$username/$repo.git"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Remote ajouté" -ForegroundColor Green
    } else {
        Write-Host "❌ Erreur lors de l'ajout du remote" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "🔧 Configuration Git globale" -ForegroundColor Yellow
Write-Host ""

$name = Read-Host "Entrez votre nom complet (ex: Ihebeddine Saafi)"
$email = Read-Host "Entrez votre email (ex: iheb@mecamanage.tn)"

git config --global user.name "$name"
git config --global user.email "$email"
git config --global credential.helper store

Write-Host "✓ Configuration Git complétée" -ForegroundColor Green
Write-Host ""

Write-Host "📦 Vérification des changements" -ForegroundColor Yellow
Write-Host ""

# Montrer le statut
git status --short | Select-Object -First 20

Write-Host ""
Write-Host "⏸️  Appuyez sur ENTER pour continuer ou Ctrl+C pour annuler..."
Read-Host

Write-Host ""
Write-Host "📝 Ajout des fichiers" -ForegroundColor Yellow
Write-Host ""

$files = @(
    ".github/workflows/ci.yml",
    "backend/MecaManage.Tests/Features/",
    "backend/MecaManage.Tests/Helpers/DatabaseHelper.cs",
    "backend/Dockerfile",
    "docker-compose.yml",
    ".env.example",
    ".gitignore",
    "DOCKER.md",
    "SETUP_GITHUB_DOCKERHUB.md",
    "DEVOPS_SETUP.md",
    "COMMIT_MESSAGE.md",
    "verify-setup.sh",
    "verify-setup.bat",
    "PUSH_AND_GITHUB_SETUP.md"
)

foreach ($file in $files) {
    Write-Host "  ➕ $file" -ForegroundColor Cyan
    git add $file
}

Write-Host "✓ Fichiers en staging" -ForegroundColor Green
Write-Host ""

Write-Host "💬 Message de commit" -ForegroundColor Yellow
$message = @"
feat: complete devops setup with comprehensive tests, docker orchestration, and CI/CD pipeline

- Added 17 unit tests for backend (GarageService, UserService)
- Configured Docker Compose with MySQL 8.0, ASP.NET 9, Angular, and Nginx
- Implemented GitHub Actions CI/CD pipeline with automated testing
- Added automatic Docker image build and push to DockerHub and GHCR
- Created complete documentation for deployment and configuration
- Updated .gitignore to exclude build artifacts

All tests passing ✓
Ready for production deployment ✓
"@

Write-Host ""
Write-Host "Message:" -ForegroundColor Cyan
Write-Host $message
Write-Host ""

Write-Host "⏸️  Appuyez sur ENTER pour créer le commit ou Ctrl+C pour annuler..."
Read-Host

Write-Host ""
Write-Host "🔨 Création du commit" -ForegroundColor Yellow
Write-Host ""

git commit -m $message

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Commit créé avec succès" -ForegroundColor Green
} else {
    Write-Host "❌ Erreur lors du commit" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🚀 Push vers GitHub" -ForegroundColor Yellow
Write-Host ""

Write-Host "Destination: https://github.com/$username/$repo.git" -ForegroundColor Cyan
Write-Host ""

Write-Host "⏸️  Appuyez sur ENTER pour pousser ou Ctrl+C pour annuler..."
Read-Host

Write-Host ""
Write-Host "📤 Envoi du code vers GitHub..." -ForegroundColor Cyan
Write-Host ""

git push -u origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ PUSH RÉUSSI!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Prochaines étapes:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1️⃣  Configurez les secrets GitHub:" -ForegroundColor Cyan
    Write-Host "   URL: https://github.com/$username/$repo/settings/secrets/actions" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Secrets à ajouter:" -ForegroundColor Yellow
    Write-Host "   • DOCKERHUB_USERNAME = votre-username-dockerhub" -ForegroundColor Yellow
    Write-Host "   • DOCKERHUB_TOKEN = votre-token-dockerhub" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "2️⃣  Vérifiez la pipeline GitHub Actions:" -ForegroundColor Cyan
    Write-Host "   URL: https://github.com/$username/$repo/actions" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "3️⃣  Créez les repositories DockerHub:" -ForegroundColor Cyan
    Write-Host "   • https://hub.docker.com/r/create" -ForegroundColor Cyan
    Write-Host "   • mecamanage-backend (public)" -ForegroundColor Cyan
    Write-Host "   • mecamanage-frontend (public)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📚 Documentation complète dans PUSH_AND_GITHUB_SETUP.md" -ForegroundColor Magenta
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ Erreur lors du push" -ForegroundColor Red
    Write-Host ""
    Write-Host "Vérifiez:" -ForegroundColor Yellow
    Write-Host "  • Votre connexion Internet" -ForegroundColor Yellow
    Write-Host "  • Vos identifiants GitHub" -ForegroundColor Yellow
    Write-Host "  • L'URL du repository" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Fin du script" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan

