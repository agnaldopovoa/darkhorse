FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Darkhorse.sln", "./"]
COPY ["backend/Api/Darkhorse.Api.csproj", "backend/Api/"]
COPY ["backend/Worker/Darkhorse.Worker.csproj", "backend/Worker/"]
COPY ["backend/Application/Darkhorse.Application.csproj", "backend/Application/"]
COPY ["backend/Domain/Darkhorse.Domain.csproj", "backend/Domain/"]
COPY ["backend/Infrastructure/Darkhorse.Infrastructure.csproj", "backend/Infrastructure/"]
RUN dotnet restore "backend/Worker/Darkhorse.Worker.csproj"

# Copy full source code
COPY . .
WORKDIR "/src/backend/Worker"
RUN dotnet build "Darkhorse.Worker.csproj" -c Release -o /app/build
RUN dotnet publish "Darkhorse.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install Docker CLI so the Worker can spawn Python runner containers
RUN apt-get update && \
    apt-get install -y apt-transport-https ca-certificates curl gnupg lsb-release && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null && \
    apt-get update && \
    apt-get install -y docker-ce-cli && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Darkhorse.Worker.dll"]
