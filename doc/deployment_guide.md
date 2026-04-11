# Darkhorse - Implementation and Deployment Guide

## System Context (MANDATORY)

* **Backend:** ASP.NET Core Web API (.NET 9)
* **Worker:** .NET Background Service (Runs inside API process via `IHostedService` for Hangfire)
* **Frontend:** React + Vite + TypeScript
* **Strategy Runner:** Python 3 (Invoked by background worker)
* **Database:** PostgreSQL (Supabase in production)
* **Real-time:** SignalR (TradingHub) — used for strategy execution lifecycle events only; dashboard uses 60-second HTTP polling
* **Cache:** Redis (Upstash in production, Local Docker in QA/Local)
* **Supported Exchanges:** Binance, Bitfinex, Bybit, Coinbase Advanced, Kraken, KuCoin, OKX

---

## 1. Local Environment (Developer Setup)

### Objectives
Allow a developer to run the full Darkhorse system locally, including API, Worker, Frontend, Python runner, and Database.

### Required Tools Setup (Ubuntu)
Run the following commands in your Linux terminal to install the requisite tools from scratch.

```bash
# Update base repositories
sudo apt update && sudo apt upgrade -y

# 1. Install .NET 8 SDK
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0

# 2. Install Node.js (v20 LTS) & npm
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs

# 3. Install Python 3 (For strategy runner)
sudo apt-get install -y python3 python3-pip

# 4. Install Docker & Docker Compose
sudo apt-get install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER 
# (You must logout and log back in to apply the docker group permissions)
```

### Setup Steps

**1. Clone Configuration Maps**
```bash
# Assuming you cloned the repository into ~/projects/darkhorse
cd ~/projects/darkhorse
```

**2. Configure Environment Variables**
Generate a `.env` map for the local API and frontend to hook into our Local Docker dependencies.
```bash
# Create local .env for Backend (reads at runtime)
cat << 'EOF' > backend/.env
DB_CONNECTION="Host=localhost;Port=5431;Database=darkhorse_dev;Username=darkhorse;Password=darkhorse"
DB_PASSWORD=darkhorse
REDIS_URL="localhost:6378,password=redispass"
REDIS_PASSWORD=redispass
JWT_SECRET="super_secret_local_jwt_key_that_is_at_least_32_bytes_long"
JWT_EXPIRATION_MINUTES="15"
REFRESHTOKEN_EXPIRATION_HOURS="24"
JTI_BLACKLIST_EXPIRATION_DAYS="7"
REFRESHTOKEN_BLACKLIST_EXPIRATION_DAYS="30"
MASTER_ENCRYPTION_KEY="4461726B686F7273652063727970746F63757272656E63792074726164696E67"
ALLOWED_ORIGINS="https://localhost:5173"
SSL_CERT_PATH="/etc/ssl/localcerts/nvr.pem"
SSL_KEY_PATH="/etc/ssl/localcerts/nvr-key.pem"
EOF

# Create local .env for Frontend
cat << 'EOF' > frontend/.env.local
DARKHORSE_API_URL="https://localhost:7000"
SSL_CERT_PATH="/etc/ssl/localcerts/nvr.pem"
SSL_KEY_PATH="/etc/ssl/localcerts/nvr-key.pem"
EOF
```

**3. Start Local Dependencies (PostgreSQL & Redis)**
Using a small compose file exclusively for our backing services.
```bash
cat <<EOF > docker-compose.local.yml
services:
  db_dev:
    image: postgres:16-alpine
    container_name: darkhorse_db_dev
    restart: always
    environment:
      POSTGRES_USER: darkhorse
      POSTGRES_PASSWORD: ${DB_PASSWORD:-darkhorse}
      POSTGRES_DB: darkhorse_dev
    ports:
      - "5431:5432"
    volumes:
      - pgdata_dev:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U darkhorse -d darkhorse_dev"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis_dev:
    image: redis:7-alpine
    container_name: darkhorse_redis_dev
    restart: always
    command: redis-server --requirepass ${REDIS_PASSWORD:-redispass}
    ports:
      - "6378:6379"
    volumes:
      - redisdata_dev:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  pgdata_dev:
  redisdata_dev:
EOF

docker compose -f docker-compose.local.yml up -d
```

### Run Commands

**1. Apply Database Migrations & Run Backend API + Worker (Terminal 1)**
The worker `IHostedService` lifecycle runs automatically inside the ASP.NET Core process.
```bash
# Restore dependencies
dotnet restore
# Apply migrations (ensure dotnet-ef is installed globally: dotnet tool install --global dotnet-ef)
cd backend
dotnet ef database update --project Infrastructure/Darkhorse.Infrastructure.csproj --startup-project Api/Darkhorse.Api.csproj
```


**2. Trust the HTTPS development certificate (run once)**
```bash
dotnet dev-certs https --trust
```


**3. Start API and Worker**
```bash
cd backend/Api
dotnet run --launch-profile "https"
# Usually binds to https://localhost:7000
```


**4. Run Frontend (Terminal 2)**
```bash
cd frontend
npm install
npm run dev
# Vite runs usually at https://localhost:5173
```

### Debugging Local
* **SignalR WS 401/CORS:** In the .NET `Program.cs`, verify CORS `WithOrigins("https://localhost:5173")` and `AllowCredentials()` is set up.
* **Python runtime fails:** If Hangfire throws "python3: command not found", check `which python3` and ensure the application runner references the correct executable path.

---

## 2. QA Environment (Local Ubuntu Server)

### Objectives
Provide a fully isolated, containerized environment that accurately mimics the production state, perfect for an internal network or VM. 

### Server Preparation
```bash
# Ensure firewall allows standard web traffic and API/UI access
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 8080/tcp # API Container
sudo ufw allow 3000/tcp # React Container
```

### Dockerized Setup
Create a master `docker-compose.yml` that builds all containers autonomously.

```yaml
# docker-compose.yml
version: '3.8'

services:
  db:
    image: postgres:15-alpine
    restart: unless-stopped
    environment:
      POSTGRES_USER: qa_user
      POSTGRES_PASSWORD: ${DB_PASS}
      POSTGRES_DB: darkhorse_qa
    volumes:
      - pgdata:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    restart: unless-stopped
    command: redis-server --requirepass ${REDIS_PASS}

  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
    restart: unless-stopped
    environment:
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=darkhorse_qa;Username=qa_user;Password=${DB_PASS}"
      Redis__ConnectionString: "redis:6379,password=${REDIS_PASS}"
      JWT_SECRET: ${JWT_SECRET}
      JWT_EXPIRATION_MINUTES: ${JWT_EXPIRATION_MINUTES}
      REFRESHTOKEN_EXPIRATION_HOURS: ${REFRESHTOKEN_EXPIRATION_HOURS}
      JTI_BLACKLIST_EXPIRATION_DAYS: ${JTI_BLACKLIST_EXPIRATION_DAYS}
      REFRESHTOKEN_BLACKLIST_EXPIRATION_DAYS: ${REFRESHTOKEN_BLACKLIST_EXPIRATION_DAYS}
      MASTER_ENCRYPTION_KEY: ${MASTER_ENCRYPTION_KEY}
      ASPNETCORE_ENVIRONMENT: "QA"
    ports:
      - "8080:8080"
    depends_on:
      - db
      - redis

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile  # Standard Nginx layer serving statically
    restart: unless-stopped
    ports:
      - "3000:80"
    environment:
      DARKHORSE_API_URL: "http://<SERVER_IP>:8080"

volumes:
  pgdata:
```

### `.env` File handling
Keep a `.env` in the root that Docker Compose will automatically read. Do not commit this.
```bash
DB_PASS=Sup3rS3cr3tQAPass
REDIS_PASS=QARedisAuth123
JWT_SECRET=bVy2N4o0l5nF1k4F6G2hX8c1D4f8B2k7
JWT_EXPIRATION_MINUTES=15
REFRESHTOKEN_EXPIRATION_HOURS="24"
JTI_BLACKLIST_EXPIRATION_DAYS="7"
REFRESHTOKEN_BLACKLIST_EXPIRATION_DAYS="30"
MASTER_ENCRYPTION_KEY=9a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p
```

### Commands
```bash
# Build and start all services in detached mode
docker compose up -d --build

# View unified logs for troubleshooting python or API errors
docker compose logs -f api

# Stop and restart API specifically
docker compose restart api
```

### Update Process (Minimal Downtime)
```bash
git pull origin main
# Rebuild only the API container and recreate it gracefully
docker compose up -d --build --no-deps api
```

---

## 3. Production Environment

### Architecture
* **Frontend:** Vercel (CD/CDN)
* **Backend + Worker:** Fly.io (Shared single app, 1GB RAM VM)
* **Database:** Supabase (PostgreSQL serverless)

### 3.1 Frontend (Vercel)
1. Go to Vercel.com and select **Add New Project**.
2. Connect your GitHub repository and select the target branch (`main`).
3. **Framework Preset:** Vite
4. **Build Command:** `npm run build`
5. **Output Directory:** `dist`
6. **Environment Variables:**
   - `DARKHORSE_API_URL`: `https://api.yourdomain.com`
7. Click **Deploy**. Vercel will auto-deploy pushes to `main`.

### 3.2 Backend + Worker (Fly.io)

**Execution Model:** 
We will implement "API + Worker in same container (HostedService)". Since Fly.io requires us to install Python for the strategy runner manually, we'll build a custom `Dockerfile`.

**1. Create the `backend/Dockerfile`:**
```dockerfile
# backend/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and restore
COPY ["*.sln", "./"]
COPY ["Api/*.csproj", "Api/"]
COPY ["Application/*.csproj", "Application/"]
COPY ["Domain/*.csproj", "Domain/"]
COPY ["Infrastructure/*.csproj", "Infrastructure/"]
COPY ["Worker/*.csproj", "Worker/"]
RUN dotnet restore

# Copy all files, build and publish
COPY . .
WORKDIR "/src/Api"
RUN dotnet publish -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Install Python 3 for the Hangfire Strategy Runner
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Darkhorse.Api.dll"]
```

**2. Fly.io Configuration (`fly.toml`):**
Place this at the root of `backend/`.
```toml
# backend/fly.toml
app = "darkhorse-api"
primary_region = "iad"

[build]
  dockerfile = "Dockerfile"

[env]
  ASPNETCORE_ENVIRONMENT = "Production"
  PORT = "8080"

[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = true
  auto_start_machines = true
  min_machines_running = 1
  processes = ["app"]

[[vm]]
  memory = "1gb"  # Important: Worker + DB connections require at least 1GB
  cpu_kind = "shared"
  cpus = 1
```

**3. Deployment Commands:**
```bash
curl -L https://fly.io/install.sh | sh
fly auth login
cd backend

# Provision the app structure (say NO to setting up Postgres via Fly, we use Supabase)
fly launch --no-deploy

# Set production secrets
fly secrets set JWT_SECRET="prod-jwt-secret-xyz"
fly secrets set JWT_EXPIRATION_MINUTES="15"
fly secrets set REFRESHTOKEN_EXPIRATION_HOURS="24"
fly secrets set JTI_BLACKLIST_EXPIRATION_DAYS="7"
fly secrets set REFRESHTOKEN_BLACKLIST_EXPIRATION_DAYS="30"
fly secrets set MASTER_ENCRYPTION_KEY="prod-enc-key-32-chars-xyz"
fly secrets set ConnectionStrings__DefaultConnection="Host=aws-0-xx.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xxx;Password=your_supabase_pass;"

# Deploy the image container
fly deploy
```

### 3.3 Database (Supabase)
1. In Supabase, create a new Project.
2. Note your **Database Password** carefully.
3. Access **Project Settings > Database** to get the Connection String.
   * Supabase provides a connection pooler string (Port 6543) and a direct string (Port 5432). Use the **Direct String (5432)** for Entity Framework Migrations, and **Pooler String (6543)** for the Fly.io app `ConnectionStrings__DefaultConnection`.
4. Apply production migrations exclusively from your local machine:
   ```bash
   export ConnectionStrings__DefaultConnection="<Supabase_Direct_Connection_String>"
   dotnet ef database update
   ```

---

## 3.4 CI/CD with GitHub Actions (MANDATORY)

Provide these exact files in `.github/workflows/`.

**1. Backend Deploy (Fly.io)**
Create `.github/workflows/deploy-backend.yml`:
```yaml
name: Deploy Backend to Fly.io
on:
  push:
    branches:
      - main
    paths:
      - 'backend/**'
jobs:
  deploy:
    name: Build & Deploy Backend
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: superfly/flyctl-actions/setup-flyctl@master
      - name: Deploy to Fly Configuration
        run: cd backend && flyctl deploy --remote-only
        env:
          FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
```

**2. Frontend Auto-Deploy (Vercel)**
Normally deployed via Vercel's Github plugin immediately on push. If a manual manual override is required via Actions:
Create `.github/workflows/deploy-frontend.yml`:
```yaml
name: Vercel Production Deployment
on:
  push:
    branches:
      - main
    paths:
      - 'frontend/**'
jobs:
  Deploy-Production:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Install Vercel CLI
        run: npm install --global vercel@latest
      - name: Pull Vercel Environment Information
        run: vercel pull --yes --environment=production --token=${{ secrets.VERCEL_TOKEN }}
      - name: Build Project Artifacts
        run: vercel build --prod --token=${{ secrets.VERCEL_TOKEN }}
      - name: Deploy Project Artifacts to Vercel
        run: vercel deploy --prebuilt --prod --token=${{ secrets.VERCEL_TOKEN }}
        env:
          VERCEL_ORG_ID: ${{ secrets.VERCEL_ORG_ID }}
          VERCEL_PROJECT_ID: ${{ secrets.VERCEL_PROJECT_ID }}
```

**Required GitHub Secrets:**
- `FLY_API_TOKEN` (Generated via `fly tokens create`)
- `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID`

---

## 4. Environment Configuration Strategy

| Level | Env Type | Env Var Naming Example | Management Tool |
|---|---|---|---|
| Local | Development | `ASPNETCORE_ENVIRONMENT=Development` | Local `.env` files / `launchSettings.json`. Ignored in Git. |
| QA | Staging | `ASPNETCORE_ENVIRONMENT=QA` | Standard `.env` injected directly via Docker Compose variables array. |
| Prod | Production | `ASPNETCORE_ENVIRONMENT=Production` | Injected via `fly secrets set`. Never stored in flat files. |

Secrets Approach: **NEVER** commit `.env` files. Ensure `.env` is inside `.gitignore`. Master encryption logs are maintained via a strictly isolated Key Manager internally at the company, completely abstracted from developer laptops.

---

## 5. Data & Performance Considerations

* **Handling OHLC Data:** Historical data grows rapidly. Use Supabase indexing on `(exchange, symbol, timeframe, ts)` on the `data_history` tables. Use EF `.AsNoTracking()` for all chart pulling to prevent catastrophic RAM bottlenecks in .NET.
* **Database Connections:** Supabase caps DB connections. Using the pooler port (`6543`) with `PgBouncer` is essential on Fly.io to avoid connection exhaustion from aggressive .NET thread pooling.
* **Worker Execution Restrictions:** Fly.io 1GB limit will break if Python containers execute non-stop. For the worker, limit the Hangfire max parallel execution threads to match the `cpus` allocated in `fly.toml` (e.g. 1-2).
* **Caching:** Caching Ticker API data from external brokers locally in Redis drops average strategy round-trip-time from 350ms to ~12ms.

---

## 6. Troubleshooting & Common Issues

| Issue | Cause | Solution |
|---|---|---|
| **API won't connect to Docker DB** | `.env` string contains `localhost`. | Inside Docker Compose, containers communicate via service names. Change `localhost` to `db`. |
| **Vite Frontend gives CORS errors** | Backend doesn't recognize frontend port. | Update `Program.cs` CORS policy to explicitly allow `https://localhost:5173` and allow credentials. |
| **Fly.io Deployment Timeouts** | Remote Docker Build running out of RAM. | Add `--remote-only` to `fly deploy` command. |
| **Supabase `too many connections`** | Direct connection limit hit by EF Core. | Change connection string port from `5432` to the connection pooler `6543`. |
| **SignalR 401 Unauthorized** | Token missing in WS Handshake. | In React config, ensure `withUrl("...", { accessTokenFactory: () => <your-memory-statet-token> })` is bound. |
| **"python3: command not found" log** | Worker container is missing OS deps. | You forgot to include `apt-get install python3` in the API Dockerfile runtime layer. Re-deploy. |
