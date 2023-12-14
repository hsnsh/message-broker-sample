FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["NotificationProducer/NotificationProducer.csproj", "NotificationProducer/"]
RUN dotnet restore "NotificationProducer/NotificationProducer.csproj"
COPY . .
WORKDIR "/src/NotificationProducer"
RUN dotnet build "NotificationProducer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationProducer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationProducer.dll"]
