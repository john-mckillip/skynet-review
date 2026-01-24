FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Gateway/SkynetReview.Gateway.csproj", "Gateway/"]
COPY ["Shared/SkynetReview.Shared.csproj", "Shared/"]
RUN dotnet restore "Gateway/SkynetReview.Gateway.csproj"
COPY . .
WORKDIR "/src/Gateway"
RUN dotnet build "SkynetReview.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SkynetReview.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SkynetReview.Gateway.dll"]