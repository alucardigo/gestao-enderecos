# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura usando só os manifestos primeiro (melhor cache de camadas).
COPY global.json Directory.Build.props ./
COPY src/GestaoEnderecos/GestaoEnderecos.csproj src/GestaoEnderecos/
RUN dotnet restore src/GestaoEnderecos/GestaoEnderecos.csproj

# Copia o restante e publica.
COPY src/ src/
RUN dotnet publish src/GestaoEnderecos/GestaoEnderecos.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GestaoEnderecos.dll"]
