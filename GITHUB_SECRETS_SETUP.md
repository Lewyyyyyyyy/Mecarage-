# 🔐 GitHub Secrets Setup Guide

Complete guide for configuring GitHub Actions secrets.

---

## 📍 Step 1: Go to GitHub Repository Settings

1. Navigate to your repository
2. Click **Settings** (top-right menu)
3. Go to **Secrets and variables** → **Actions**

---

## ✅ Required Secrets (2)

### 1. `DOCKERHUB_USERNAME`

**What**: Your DockerHub username

**Where to get it**:
1. Go to https://hub.docker.com
2. Login with your account
3. Username is in the top-right menu

**Example**: `johndoe`

### 2. `DOCKERHUB_TOKEN`

**What**: DockerHub Personal Access Token (NOT your password!)

**Where to get it**:
1. Go to https://hub.docker.com/settings/security
2. Click **New Access Token**
3. Give it a descriptive name (e.g., "GitHub Actions")
4. Select permissions (select all)
5. Copy the token
6. Add to GitHub Secrets

**Important**: The token will be hidden after creation. Copy it immediately!

---

## 🟣 Optional Secrets (for CI/CD enhancements)

### 3. `SONAR_TOKEN` (Optional - Code Quality)

**What**: SonarCloud token for code analysis

**Setup**:
1. Create account: https://sonarcloud.io
2. Go to Settings → Security
3. Generate token
4. Add to GitHub Secrets

### 4. `SERVER_HOST` (Optional - Production Deploy)

**What**: Your production server IP or domain

**Example**: `1.2.3.4` or `prod.example.com`

### 5. `SERVER_USER` (Optional - Production Deploy)

**What**: SSH username for your server

**Example**: `deployer`

### 6. `SERVER_SSH_KEY` (Optional - Production Deploy)

**What**: Private SSH key (PEM format)

**Where to get it**:

```bash
# Generate new key (run locally)
ssh-keygen -t rsa -b 4096 -f ~/.ssh/mecarage -N ""

# Copy ENTIRE private key content
cat ~/.ssh/mecarage

# Copy the public key to server
ssh-copy-id -i ~/.ssh/mecarage.pub deployer@your-server-ip
```

**Important**: Copy the **entire** private key (from `-----BEGIN RSA PRIVATE KEY-----` to `-----END RSA PRIVATE KEY-----`)

---

## 🔧 How to Add Secrets

### Method 1: GitHub Web UI

1. Go to Settings → Secrets and variables → Actions
2. Click **New repository secret**
3. Name: `DOCKERHUB_USERNAME`
4. Value: Your DockerHub username
5. Click **Add secret**

Repeat for each secret.

### Method 2: GitHub CLI

```bash
# Install GitHub CLI: https://cli.github.com

# Login
gh auth login

# Add secrets
gh secret set DOCKERHUB_USERNAME -b "yourusername"
gh secret set DOCKERHUB_TOKEN -b "your_token_here"
```

---

## ✨ Verify Secrets

### In GitHub Web UI

1. Go to Settings → Secrets and variables → Actions
2. You should see your secrets listed (values hidden)
3. Click the secret to update/delete

### Test the Pipeline

1. Go to Actions tab
2. Fork/commit code to trigger workflow
3. Watch the pipeline execute
4. Check if Docker images are pushed to DockerHub

---

## 🐛 Troubleshooting

### "Permission denied" in Docker Build

- Verify `DOCKERHUB_TOKEN` is correct (not your password)
- Token must have write permissions
- Try regenerating token

### Secrets not working in Actions

- Wait a few minutes after adding secrets
- Refresh GitHub Actions page
- Check secret name matches exactly (case-sensitive)

### Can't find DockerHub Token

1. Go to https://hub.docker.com/settings/security
2. Delete old tokens
3. Create new Personal Access Token
4. Copy immediately and add to GitHub

### SSH Key Issues

- Ensure key is in PEM format (not OpenSSH)
- Include `-----BEGIN-----` and `-----END-----` lines
- Ensure public key is on server: `authorized_keys`

---

## 📋 Secret Names Reference

| Name | Required | Type |
|------|----------|------|
| `DOCKERHUB_USERNAME` | ✅ Yes | Text |
| `DOCKERHUB_TOKEN` | ✅ Yes | Token |
| `GITHUB_TOKEN` | ✅ Auto | (auto-created) |
| `SONAR_TOKEN` | ⭕ Optional | Token |
| `SERVER_HOST` | ⭕ Optional | URL/IP |
| `SERVER_USER` | ⭕ Optional | Username |
| `SERVER_SSH_KEY` | ⭕ Optional | Private Key |

---

## 🚀 After Setup

1. Commit and push code to `main` branch
2. Go to Actions tab
3. Watch pipeline execute
4. Verify images in DockerHub after successful build
5. Check logs for any errors

---

## 🔒 Security Best Practices

- ✅ Use Docker tokens, not passwords
- ✅ Rotate tokens regularly
- ✅ Use SSH keys instead of passwords
- ✅ Never commit secrets to Git
- ✅ Use `.env.example` for templates only
- ✅ Review Actions logs for security issues
- ✅ Keep software updated

---

## 📞 Help

- GitHub Actions Docs: https://docs.github.com/en/actions
- DockerHub Security: https://docs.docker.com/docker-hub/access-tokens/
- GitHub Secrets: https://docs.github.com/en/actions/security-guides/encrypted-secrets

