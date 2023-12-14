FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LogConsumer/LogConsumer.csproj", "LogConsumer/"]
RUN dotnet restore "LogConsumer/LogConsumer.csproj"
COPY . .
WORKDIR "/src/LogConsumer"
RUN dotnet build "LogConsumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LogConsumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LogConsumer.dll"]
