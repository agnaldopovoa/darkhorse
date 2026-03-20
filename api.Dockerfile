FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Darkhorse.sln", "./"]
COPY ["backend/Api/Darkhorse.Api.csproj", "backend/Api/"]
COPY ["backend/Worker/Darkhorse.Worker.csproj", "backend/Worker/"]
COPY ["backend/Application/Darkhorse.Application.csproj", "backend/Application/"]
COPY ["backend/Domain/Darkhorse.Domain.csproj", "backend/Domain/"]
COPY ["backend/Infrastructure/Darkhorse.Infrastructure.csproj", "backend/Infrastructure/"]
RUN dotnet restore "backend/Api/Darkhorse.Api.csproj"

# Copy full source code
COPY . .
WORKDIR "/src/backend/Api"
RUN dotnet build "Darkhorse.Api.csproj" -c Release -o /app/build
RUN dotnet publish "Darkhorse.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Darkhorse.Api.dll"]
