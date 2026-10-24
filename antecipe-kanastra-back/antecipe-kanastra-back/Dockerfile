FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY antecipe-kanastra-back/antecipe-kanastra-back/antecipe-kanastra-back.csproj antecipe-kanastra-back/antecipe-kanastra-back/
RUN dotnet restore antecipe-kanastra-back/antecipe-kanastra-back/antecipe-kanastra-back.csproj
COPY antecipe-kanastra-back/antecipe-kanastra-back/ antecipe-kanastra-back/antecipe-kanastra-back/
COPY antecipe-kanastra/antecipe-kanastra/validators.json antecipe-kanastra/antecipe-kanastra/validators.json
RUN dotnet publish antecipe-kanastra-back/antecipe-kanastra-back/antecipe-kanastra-back.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "antecipe-kanastra-back.dll"]
