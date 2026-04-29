# n8n setup - MecaManage

## 1) Import workflows in n8n
1. Open `http://127.0.0.1:5678`.
2. Go to **Workflows** > **Import from File**.
3. Import these files one by one:
   - `n8n/workflows/workflow1-notification-client.json`
   - `n8n/workflows/workflow2-notification-mecanicien.json`
   - `n8n/workflows/workflow3-rapport-quotidien.json`
4. Save each workflow and keep it inactive until credentials are configured.

## 2) Configure credentials

### SMTP credential (for Email nodes)
1. In n8n, open **Credentials** > **Create Credential** > **SMTP**.
2. Set name to `SMTP MecaManage`.
3. Example SMTP config:
   - Host: `smtp.gmail.com` (or your SMTP server)
   - Port: `587`
   - Secure: `false` (use `true` for port `465`)
   - User: your sender email
   - Password/App password: your SMTP secret
   - From Email: `no-reply@mecamanage.tn`
4. Save and test the credential.

### PostgreSQL credential (for Daily report)
1. In n8n, open **Credentials** > **Create Credential** > **Postgres**.
2. Set name to `PostgreSQL MecaManage`.
3. Use:
   - Host: `localhost`
   - Port: `5432`
   - Database: `mecamanage`
   - User: `mecamanage_user`
   - Password: `pass123`
4. Save and test the credential.

## 3) Test each workflow

### Workflow 1: Notification client (intervention acceptee)
1. Activate workflow.
2. Send a POST request to:
   - `http://127.0.0.1:5678/webhook/intervention-accepted`
3. Example JSON body:
```json
{
  "interventionId": 101,
  "clientName": "Ahmed",
  "clientEmail": "client@example.com",
  "vehicle": "Peugeot 208",
  "garage": "Garage Centre",
  "scheduledAt": "2026-03-12 15:30"
}
```
4. Confirm email received by client.

### Workflow 2: Notification mecanicien (assignation)
1. Activate workflow.
2. Send a POST request to:
   - `http://127.0.0.1:5678/webhook/mecanicien-assigned`
3. Example JSON body:
```json
{
  "interventionId": 102,
  "mecanicienName": "Karim",
  "mecanicienEmail": "meca@example.com",
  "clientName": "Sami",
  "vehicle": "Renault Clio",
  "location": "Tunis Centre",
  "urgency": "High"
}
```
4. Confirm email received by mecanicien.

### Workflow 3: Rapport quotidien
1. Activate workflow.
2. Run it manually once from n8n editor to validate DB access and email sending.
3. Confirm report email sent to `iheb@mecamanage.tn`.
4. Keep it active for automatic run every day at 18:00.

## 4) Notes
- The workflow SQL expects an `intervention_requests` table with `id`, `status`, `urgency`, `created_at`.
- If your schema differs, update the SQL query in workflow 3.
