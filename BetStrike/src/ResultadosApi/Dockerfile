FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ResultadosApi/*.csproj src/ResultadosApi/
RUN dotnet restore src/ResultadosApi/ResultadosApi.csproj

COPY . .
RUN dotnet publish src/ResultadosApi/ResultadosApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ResultadosApi.dll"]