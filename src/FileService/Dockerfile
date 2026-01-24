FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["FileService/SkynetReview.FileService.csproj", "FileService/"]
COPY ["Shared/SkynetReview.Shared.csproj", "Shared/"]
RUN dotnet restore "FileService/SkynetReview.FileService.csproj"
COPY . .
WORKDIR "/src/FileService"
RUN dotnet build "SkynetReview.FileService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SkynetReview.FileService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create volume mount point for file storage
VOLUME /app/temp-files

ENTRYPOINT ["dotnet", "SkynetReview.FileService.dll"]