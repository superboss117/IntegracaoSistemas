FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/AnalyticsWorker/AnalyticsWorker.csproj", "AnalyticsWorker/"]
COPY ["src/Shared.Events/Shared.Events.csproj", "Shared.Events/"]
COPY ["src/Shared.Messaging/Shared.Messaging.csproj", "Shared.Messaging/"]
RUN dotnet restore "AnalyticsWorker/AnalyticsWorker.csproj"
COPY src/AnalyticsWorker/ AnalyticsWorker/
COPY src/Shared.Events/ Shared.Events/
COPY src/Shared.Messaging/ Shared.Messaging/
WORKDIR "/src/AnalyticsWorker"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AnalyticsWorker.dll"]
