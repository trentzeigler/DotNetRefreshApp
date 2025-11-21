# Azure SQL Database Setup Guide

## Step 1: Create Your `.env` File

Copy `.env.example` to `.env` and fill in your Azure SQL Database details:

```bash
cp .env.example .env
```

Edit `.env` and replace with your actual Azure connection string:

```env
AZURE_SQL_CONNECTION_STRING=Server=tcp:YOUR_SERVER_NAME.database.windows.net,1433;Initial Catalog=MyDB;Persist Security Info=False;User ID=YOUR_USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### How to Get Your Connection String from Azure Portal:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your SQL Database
3. Click **"Connection strings"** in the left menu
4. Copy the **ADO.NET** connection string
5. Replace `{your_password}` with your actual password

---

## Step 2: Apply Migrations to Azure SQL Database

Before running your app, create the database schema in Azure:

```bash
# Set the connection string temporarily for migration
export ConnectionStrings__DefaultConnection="YOUR_AZURE_CONNECTION_STRING"

# Apply migrations
dotnet ef database update
```

Or use the connection string directly:

```bash
dotnet ef database update --connection "YOUR_AZURE_CONNECTION_STRING"
```

---

## Step 3: Run Your App with Docker Compose

### Option A: Run with Azure SQL Database (Recommended)

```bash
# Build and start the app (uses Azure SQL from .env file)
docker-compose up --build app
```

### Option B: Run with Local SQL Server

```bash
# Start both local SQL Server and app
docker-compose --profile local-db up --build
```

---

## Step 4: Test Your API

Once running, your API will be available at:
- **Swagger UI:** http://localhost:5173/swagger
- **API Endpoint:** http://localhost:5173/api/users

Test with curl:
```bash
# Get all users
curl http://localhost:5173/api/users

# Create a user
curl -X POST http://localhost:5173/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Jane Doe","email":"jane@example.com","role":"User"}'
```

---

## Environment Variable Injection Explained

Docker Compose reads the `.env` file and injects `AZURE_SQL_CONNECTION_STRING` into the container.

Inside the container, .NET reads it as:
```
ConnectionStrings__DefaultConnection
```

The double underscore `__` represents a nested JSON structure in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

---

## Security Best Practices

✅ **DO:**
- Keep `.env` in `.gitignore` (already configured)
- Use Azure Key Vault for production secrets
- Rotate passwords regularly
- Use Managed Identity in Azure (no passwords needed!)

❌ **DON'T:**
- Commit `.env` to Git
- Hardcode passwords in `appsettings.json`
- Share connection strings in chat/email

---

## Troubleshooting

### "Cannot open database"
- Make sure you ran `dotnet ef database update` against Azure SQL
- Check firewall rules in Azure Portal (allow your IP)

### "Login failed for user"
- Verify username/password in connection string
- Check if user has proper permissions

### "Server was not found"
- Verify server name in connection string
- Check network connectivity to Azure

---

## Multiple Containers → Same Database

Once configured, you can deploy this same Docker image to:
- **Azure Container Instances**
- **Azure Kubernetes Service (AKS)**
- **AWS ECS/EKS**
- **Any Docker host**

All containers will share the same Azure SQL Database! Just inject the same `AZURE_SQL_CONNECTION_STRING` environment variable.
