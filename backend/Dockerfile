# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia a solução e os arquivos de projeto
COPY ./gestaotcc.sln ./
COPY ./gestaotcc.WebApi/gestaotcc.WebApi.csproj ./gestaotcc.WebApi/
COPY ./gestaotcc.Application/gestaotcc.Application.csproj ./gestaotcc.Application/
COPY ./gestaotcc.Domain/gestaotcc.Domain.csproj ./gestaotcc.Domain/
COPY ./gestaotcc.Infra/gestaotcc.Infra.csproj ./gestaotcc.Infra/
COPY ./gestaotcc.Test/gestaotcc.Test.csproj ./gestaotcc.Test/

# Restaura dependências
RUN dotnet restore

# Copia tudo
COPY . .

# Publica a aplicação
RUN dotnet publish ./gestaotcc.WebApi/gestaotcc.WebApi.csproj -c Release -o /app/out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "gestaotcc.WebApi.dll"]