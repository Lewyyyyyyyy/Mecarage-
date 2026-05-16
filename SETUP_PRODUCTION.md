# 🚀 Setup Production MecaRage

Deployment guide to production servers.

---

## 📋 Prérequis Serveur

**Recommandé**: Ubuntu 22.04 LTS

```bash
# Check Ubuntu version
lsb_release -a

# Install required tools
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl wget git openssl
```

### Install Docker & Docker Compose

```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
docker-compose --version
```

---

## 1️⃣ Clone & Configuration

```bash
# Clone repository
git clone https://github.com/your-username/mecarage.git
cd mecarage

# Copy environment template
cp .env.example .env.prod

# Edit with your values
nano .env.prod
```

### Configuration essentielles dans `.env.prod`

```env
# Database
MYSQL_ROOT_PASSWORD=your_secure_password_here
MYSQL_DATABASE=mecarage

# JWT
Jwt__Secret=your_very_secure_32_char_secret_key_here

# AI
GEMINI_API_KEY=your_google_api_key

# Docker Images (from your DockerHub)
BACKEND_IMAGE=yourusername/mecamanage-backend:latest
FRONTEND_IMAGE=yourusername/mecamanage-frontend:latest
```

---

## 2️⃣ Configure SSL/HTTPS

### Option A: Let's Encrypt (Recommended)

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Get certificate
sudo certbot certonly --standalone -d your-domain.com -d www.your-domain.com

# Certificates stored in:
# /etc/letsencrypt/live/your-domain.com/

# Create copy for Docker
sudo cp /etc/letsencrypt/live/your-domain.com/fullchain.pem ./nginx/ssl/cert.crt
sudo cp /etc/letsencrypt/live/your-domain.com/privkey.pem ./nginx/ssl/private.key
sudo chown $USER:$USER ./nginx/ssl/*
```

### Option B: Auto-renew (Certbot)

```bash
# Setup auto-renewal
sudo systemctl enable certbot.timer
sudo systemctl start certbot.timer
```

---

## 3️⃣ Configure Nginx

Update `nginx/nginx.conf`:

```nginx
server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/nginx/ssl/cert.crt;
    ssl_certificate_key /etc/nginx/ssl/private.key;
    
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    
    location / {
        proxy_pass http://frontend:3000;
    }
    
    location /api/ {
        proxy_pass http://backend:5000/api/;
    }
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}
```

---

## 4️⃣ Deploy with Docker Compose

```bash
# From project root
docker compose -f docker-compose.yml --env-file .env.prod up -d

# Check services
docker compose -f docker-compose.yml ps

# View logs
docker compose -f docker-compose.yml logs -f

# Check health
curl -I https://your-domain.com
curl -I https://your-domain.com:5073/health
```

---

## 5️⃣ Access Your Application

| Service | URL |
|---------|-----|
| Frontend | `https://your-domain.com` |
| Backend API | `https://your-domain.com:5073` |
| Swagger Docs | `https://your-domain.com:5073/swagger` |
| Prometheus | `http://127.0.0.1:9090` (local only) |
| Grafana | `http://127.0.0.1:3000` (local only) |

---

## 6️⃣ Monitoring & Logs

```bash
# View all logs
docker compose -f docker-compose.yml logs -f

# View specific service
docker compose -f docker-compose.yml logs -f backend
docker compose -f docker-compose.yml logs -f frontend

# Container stats
docker stats

# Health check
docker compose -f docker-compose.yml ps
```

---

## 7️⃣ Backup & Maintenance

### Backup Database

```bash
# Backup MySQL
docker exec mecamanage-mysql mysqldump -u root -p$MYSQL_ROOT_PASSWORD mecarage > backup.sql

# Restore
docker exec -i mecamanage-mysql mysql -u root -p$MYSQL_ROOT_PASSWORD mecarage < backup.sql
```

### Updates

```bash
# Pull latest images
docker compose -f docker-compose.yml pull

# Apply updates (zero-downtime)
docker compose -f docker-compose.yml up -d --no-build --remove-orphans

# Cleanup
docker image prune -f
```

---

## 8️⃣ Troubleshooting

### SSL Certificate Issues

```bash
# Verify certificate
openssl s_client -connect your-domain.com:443

# Renew before expiration
sudo certbot renew --dry-run
```

### Database Connection Issues

```bash
# Check MySQL
docker compose -f docker-compose.yml exec mysql mysql -u root -p -e "SELECT 1"

# Check logs
docker compose -f docker-compose.yml logs mysql
```

### Memory Issues

```bash
# Check running processes
docker stats

# Increase Docker memory limit in docker-compose.yml
services:
  backend:
    deploy:
      resources:
        limits:
          memory: 1G
```

---

## 🔐 Security Checklist

- [ ] Change default MySQL password
- [ ] Generate new JWT secret
- [ ] Enable HTTPS/SSL
- [ ] Setup firewall rules
- [ ] Configure rate limiting
- [ ] Enable Docker registry authentication
- [ ] Regular backups configured
- [ ] Monitoring alerts setup

---

## 📞 Support

- Logs: `docker compose logs -f`
- Health: `curl https://your-domain.com/health`
- Docs: See `MONITORING.md`

