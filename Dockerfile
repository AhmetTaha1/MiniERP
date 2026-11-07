# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY src/MiniERP/MiniERP.csproj src/MiniERP/
RUN dotnet restore src/MiniERP/MiniERP.csproj

COPY src/MiniERP/ src/MiniERP/
RUN dotnet publish src/MiniERP/MiniERP.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "MiniERP.dll"]
