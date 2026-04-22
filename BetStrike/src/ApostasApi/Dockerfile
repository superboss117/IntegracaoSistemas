FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ApostasApi/*.csproj src/ApostasApi/
RUN dotnet restore src/ApostasApi/ApostasApi.csproj

COPY . .
RUN dotnet publish src/ApostasApi/ApostasApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ApostasApi.dll"]