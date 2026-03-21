📘 Darkhorse Deployment & Implementation Guide
1. Local Environment (Developer Setup)
1.1 Install Dependencies (Ubuntu)
sudo apt update

# Base tools
sudo apt install -y git curl build-essential

# .NET SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

export PATH=$PATH:$HOME/.dotnet

# Node.js (LTS)
curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
sudo apt install -y nodejs

# Python
sudo apt install -y python3 python3-pip python3-venv

# Docker
sudo apt install -y docker.io docker-compose
sudo usermod -aG docker $USER
1.2 Clone Repository
git clone https://github.com/agnaldopovoa/darkhorse.git
cd darkhorse
1.3 Environment Variables

Create .env:

DB_CONNECTION=Host=localhost;Port=5432;Database=darkhorse;Username=postgres;Password=postgres
REDIS_HOST=localhost
JWT_SECRET=dev_secret
1.4 Database (Docker)
docker run -d \
  --name postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=darkhorse \
  -p 5432:5432 \
  postgres:15
1.5 Run Backend (API)
cd src
dotnet restore
dotnet build
dotnet run --project API
1.6 Run Worker
dotnet run --project Worker
1.7 Run Frontend
cd frontend
npm install
npm run dev
1.8 Run Python Strategy Runner
cd strategy-runner
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt

python main.py
1.9 Debugging
API
dotnet watch run
Common Issues
Problem	Solution
DB connection fails	Check port 5432
SignalR not connecting	Verify CORS
Python errors	Activate venv
2. QA Environment (Dockerized Ubuntu Server)
2.1 Install Docker
sudo apt update
sudo apt install -y docker.io docker-compose
2.2 docker-compose.yml
version: "3.9"

services:
  api:
    build: .
    ports:
      - "5000:5000"
    env_file:
      - .env
    depends_on:
      - db
      - redis

  worker:
    build: .
    command: ["dotnet", "Worker.dll"]
    env_file:
      - .env
    depends_on:
      - db

  db:
    image: postgres:15
    environment:
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: darkhorse
    ports:
      - "5432:5432"

  redis:
    image: redis:7
    ports:
      - "6379:6379"
2.3 Commands
docker-compose up -d --build
docker-compose logs -f
docker-compose down
2.4 Update Deployment
git pull
docker-compose down
docker-compose up -d --build
3. Production Environment
Architecture
Frontend → Vercel
Backend + Worker → Fly.io
Database → Supabase
3.1 Frontend (Vercel)
Setup
Import GitHub repo
Framework: Vite
Build Settings
npm run build

Output:

dist/
Environment Variables
VITE_API_URL=https://your-api.fly.dev
3.2 Backend + Worker (Fly.io)
Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish src/API -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

RUN apt-get update && apt-get install -y python3 python3-pip

CMD ["dotnet", "API.dll"]
fly.toml
app = "darkhorse-api"

[build]
  dockerfile = "Dockerfile"

[env]
  ASPNETCORE_URLS = "http://0.0.0.0:8080"

[[services]]
  internal_port = 8080
  protocol = "tcp"

  [[services.ports]]
    port = 80
Deploy
fly launch
fly deploy
Resource Limits (safe)
fly scale vm shared-cpu-1x --memory 512
3.3 Supabase (Database)
Setup
Create project
Get connection string
DB_CONNECTION=Host=xyz.supabase.co;Database=postgres;Username=postgres;Password=xxx
Apply Migrations
dotnet ef database update
3.4 CI/CD (GitHub Actions)
Backend Deploy

.github/workflows/backend.yml

name: Deploy Backend

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - uses: superfly/flyctl-actions/setup-flyctl@master

      - run: flyctl deploy --remote-only
        env:
          FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
Frontend (optional)

Vercel auto-deploy via GitHub

4. Environment Strategy
Env	DB	Hosting
Local	Docker	Local
QA	Docker	Ubuntu
Prod	Supabase	Cloud
Naming Convention
APP_ENV=local
APP_ENV=qa
APP_ENV=prod
5. Data & Performance
OHLC Storage
Type	Size (10y)
1-min	~300 MB
5-min	~60 MB
Indexing
CREATE INDEX idx_symbol_time ON candles(symbol, timestamp);
Optimization
Cache frequent queries
Limit backtest duration
Use pagination
6. Troubleshooting
Docker fails
sudo systemctl restart docker
Fly deploy fails
fly logs
CORS error

Check:

builder.Services.AddCors(...)
Supabase connection
Check SSL requirement
Verify port 5432
✅ Final Notes
Start simple (single instance)
Measure CPU/memory during backtests
Scale only when needed
