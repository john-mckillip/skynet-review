FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Install Node.js and npm for Copilot CLI
RUN apt-get update && apt-get install -y \
    curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g @github/copilot \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SecurityAgent/SkynetReview.SecurityAgent.csproj", "SecurityAgent/"]
COPY ["Shared/SkynetReview.Shared.csproj", "Shared/"]
RUN dotnet restore "SecurityAgent/SkynetReview.SecurityAgent.csproj"
COPY . .
WORKDIR "/src/SecurityAgent"
RUN dotnet build "SkynetReview.SecurityAgent.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SkynetReview.SecurityAgent.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy the security rules config file
COPY SecurityAgent/security-rules.yml .

ENTRYPOINT ["dotnet", "SkynetReview.SecurityAgent.dll"]