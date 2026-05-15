#!/bin/bash
# Script de vérification pré-déploiement

echo "════════════════════════════════════════════════════════════"
echo "  MecaManage - Vérification pré-déploiement"
echo "════════════════════════════════════════════════════════════"
echo ""

# Couleurs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

check_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓ $2${NC}"
    else
        echo -e "${RED}✗ $2${NC}"
    fi
}

# Vérifications
echo "📋 Vérification des fichiers..."
echo ""

# Fichiers essentiels
files=(
    ".github/workflows/ci.yml"
    "backend/Dockerfile"
    "frontend/Dockerfile"
    "docker-compose.yml"
    ".env.example"
    "DOCKER.md"
    "SETUP_GITHUB_DOCKERHUB.md"
    "backend/MecaManage.Tests/Features/GarageServiceTests.cs"
    "backend/MecaManage.Tests/Features/UserServiceTests.cs"
    "backend/MecaManage.Tests/Helpers/DatabaseHelper.cs"
)

for file in "${files[@]}"; do
    if [ -f "$file" ]; then
        check_result 0 "$file"
    else
        check_result 1 "$file MANQUANT"
    fi
done

echo ""
echo "🧪 Vérification des tests..."
echo ""

# Vérifier que .NET est installé
if command -v dotnet &> /dev/null; then
    check_result 0 ".NET SDK installé: $(dotnet --version)"
else
    check_result 1 ".NET SDK non installé"
fi

# Vérifier que Docker est installé
if command -v docker &> /dev/null; then
    check_result 0 "Docker installé: $(docker --version)"
else
    check_result 1 "Docker non installé"
fi

# Vérifier que Docker Compose est installé
if command -v docker-compose &> /dev/null; then
    check_result 0 "Docker Compose installé: $(docker-compose --version)"
else
    check_result 1 "Docker Compose non installé"
fi

echo ""
echo "🔐 Vérification des secrets GitHub..."
echo ""

echo "⚠️  Les secrets suivants doivent être configurés sur GitHub:"
echo "   1. DOCKERHUB_USERNAME"
echo "   2. DOCKERHUB_TOKEN"
echo "   3. GITHUB_TOKEN (auto-généré)"
echo ""

echo "🏗️  Vérification du build..."
echo ""

if dotnet build backend/MecaManage.sln --configuration Release --no-restore 2>&1 | grep -q "a réussi"; then
    check_result 0 "Build .NET réussi"
else
    check_result 1 "Build .NET échoué"
fi

echo ""
echo "✅ Vérification complète!"
echo ""
echo "Prochaines étapes:"
echo "1. Configurez les secrets GitHub"
echo "2. Poussez les changements : git push origin main"
echo "3. Vérifiez la pipeline sur GitHub Actions"
echo "4. Déployez avec docker-compose"
echo ""

