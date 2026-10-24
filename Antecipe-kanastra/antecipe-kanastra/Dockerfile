FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["antecipe-kanastra/antecipe-kanastra.csproj", "antecipe-kanastra/"]
RUN dotnet restore "antecipe-kanastra/antecipe-kanastra.csproj"
COPY . .
WORKDIR "/src/antecipe-kanastra"
RUN dotnet build "./antecipe-kanastra.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./antecipe-kanastra.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "antecipe-kanastra.dll"]
