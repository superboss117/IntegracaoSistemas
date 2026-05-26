FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/WorkersRabbitMq/WorkersRabbitMq.csproj", "WorkersRabbitMq/"]
COPY ["src/Shared.Messaging/Shared.Messaging.csproj", "Shared.Messaging/"]
RUN dotnet restore "WorkersRabbitMq/WorkersRabbitMq.csproj"
COPY src/WorkersRabbitMq/ WorkersRabbitMq/
COPY src/Shared.Messaging/ Shared.Messaging/
WORKDIR "/src/WorkersRabbitMq"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkersRabbitMq.dll"]
