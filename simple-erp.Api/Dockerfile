# Dockerfile multi-stage da API do Simple ERP.
# O contexto de build é a RAIZ da solução (veja o docker-compose), porque a API
# depende dos projetos Core e Infraestrutura — todos precisam estar visíveis.

# ---------- Estágio 1: restore + build + publish ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos primeiro só os .csproj para aproveitar o cache de camadas do Docker:
# enquanto as dependências não mudarem, o restore não é refeito a cada alteração de código.
COPY simple-erp.Core/simple-erp.Core.csproj                     simple-erp.Core/
COPY simple-erp.Infraestrutura/simple-erp.Infraestrutura.csproj simple-erp.Infraestrutura/
COPY simple-erp.Api/simple-erp.Api.csproj                       simple-erp.Api/

RUN dotnet restore simple-erp.Api/simple-erp.Api.csproj

# Agora sim o código-fonte completo.
COPY simple-erp.Core/          simple-erp.Core/
COPY simple-erp.Infraestrutura/ simple-erp.Infraestrutura/
COPY simple-erp.Api/           simple-erp.Api/

RUN dotnet publish simple-erp.Api/simple-erp.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- Estágio 2: imagem final (só runtime) ----------
# A imagem final não carrega o SDK — apenas o runtime do ASP.NET, ficando bem menor.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Kestrel escuta na 8080 dentro do container (porta não privilegiada, padrão das
# imagens .NET 8+). O mapeamento para a porta do host é feito no docker-compose.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "simple-erp.Api.dll"]
