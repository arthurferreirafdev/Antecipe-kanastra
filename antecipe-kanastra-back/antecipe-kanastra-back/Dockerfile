FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY antecipe-kanastra-back/antecipe-kanastra-back/antecipe-kanastra-back.csproj antecipe-kanastra-back/antecipe-kanastra-back/
RUN dotnet restore antecipe-kanastra-back/antecipe-kanastra-back/antecipe-kanastra-back.csproj
COPY antecipe-kanastra-back/antecipe-kanastra-back/ antecipe-kanastra-back/antecipe-kanastra-back/
# [Aa]: a pasta aparece como "antecipe-kanastra" no git e "Antecipe-kanastra" em alguns checkouts.
COPY [Aa]ntecipe-kanastra/antecipe-kanastra/validators.json antecipe-kanastra/antecipe-kanastra/
RUN dotnet publish antecipe-kanastra-back/antecipe-kanastra-back/antecipe-kanastra-back.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
# O publish não inclui o CSV (o csproj usa Content Update, e .csv não é Content por padrão).
COPY antecipe-kanastra-back/antecipe-kanastra-back/estoque_antecipe.csv .
EXPOSE 8080
ENTRYPOINT ["dotnet", "antecipe-kanastra-back.dll"]
