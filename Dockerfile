FROM node:20-alpine AS frontend-build
WORKDIR /web
COPY web/package*.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY Marmitas.sln ./
COPY src/Marmitas.Domain/Marmitas.Domain.csproj src/Marmitas.Domain/
COPY src/Marmitas.Application/Marmitas.Application.csproj src/Marmitas.Application/
COPY src/Marmitas.Infrastructure/Marmitas.Infrastructure.csproj src/Marmitas.Infrastructure/
COPY src/Marmitas.Api/Marmitas.Api.csproj src/Marmitas.Api/
RUN dotnet restore src/Marmitas.Api/Marmitas.Api.csproj

COPY src/ src/
COPY --from=frontend-build /web/dist src/Marmitas.Api/wwwroot
RUN dotnet publish src/Marmitas.Api/Marmitas.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=backend-build /app/publish .
ENTRYPOINT ["dotnet", "Marmitas.Api.dll"]
