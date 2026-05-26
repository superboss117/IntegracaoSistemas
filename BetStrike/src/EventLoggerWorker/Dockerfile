FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/EventLoggerWorker/EventLoggerWorker.csproj", "EventLoggerWorker/"]
COPY ["src/Shared.Events/Shared.Events.csproj", "Shared.Events/"]
RUN dotnet restore "EventLoggerWorker/EventLoggerWorker.csproj"
COPY src/EventLoggerWorker/ EventLoggerWorker/
COPY src/Shared.Events/ Shared.Events/
WORKDIR "/src/EventLoggerWorker"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventLoggerWorker.dll"]
