# Build e publicação da API (.NET 10). Contexto do build: a pasta api/.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Antecipe.Api/Antecipe.Api.csproj Antecipe.Api/
RUN dotnet restore Antecipe.Api/Antecipe.Api.csproj
COPY Antecipe.Api/ Antecipe.Api/
RUN dotnet publish Antecipe.Api/Antecipe.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# A pasta Dados/ (validators.json, estoque e situações cadastrais) vai junto no publish.
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Antecipe.Api.dll"]
