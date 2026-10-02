# API (ASP.NET Core 8) — compila e roda dentro do container
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/TarotSE.Api/TarotSE.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
ENTRYPOINT ["dotnet", "TarotSE.Api.dll"]
