FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
# Remove the USER directive temporarily to debug permissions
# USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Server/Server.csproj", "Server/"]
RUN dotnet restore "Server/Server.csproj"
COPY . .
WORKDIR "/src/Server"
RUN dotnet build "./Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY Server/ConfigurationManagement/AllGatewayConfigs.json /app/ConfigurationManagement
COPY Server/appsettings.json .

ENTRYPOINT ["sh", "-c", "dotnet Server.dll"]