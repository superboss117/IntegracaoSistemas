FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/AnalyticsApi/AnalyticsApi.csproj", "AnalyticsApi/"]
RUN dotnet restore "AnalyticsApi/AnalyticsApi.csproj"
COPY src/AnalyticsApi/ AnalyticsApi/
WORKDIR "/src/AnalyticsApi"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AnalyticsApi.dll"]
