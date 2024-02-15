FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
    
FROM  --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RinhaApi/RinhaApi.csproj", "RinhaApi/"]
RUN dotnet restore "RinhaApi/RinhaApi.csproj" --arch $TARGETARCH
COPY . .
WORKDIR "/src/RinhaApi"
RUN dotnet build "RinhaApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RinhaApi.csproj" -c $BUILD_CONFIGURATION --arch $TARGETARCH -o /app/publish  /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RinhaApi.dll"]
