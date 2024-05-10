FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["NetCoreEventBus/NetCoreEventBus.Web.Shipment/NetCoreEventBus.Web.Shipment.csproj", "NetCoreEventBus/NetCoreEventBus.Web.Shipment/"]
RUN dotnet restore "NetCoreEventBus/NetCoreEventBus.Web.Shipment/NetCoreEventBus.Web.Shipment.csproj"
COPY . .
WORKDIR "/src/NetCoreEventBus/NetCoreEventBus.Web.Shipment"
RUN dotnet build "NetCoreEventBus.Web.Shipment.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetCoreEventBus.Web.Shipment.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetCoreEventBus.Web.Shipment.dll"]
