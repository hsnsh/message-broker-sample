FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["NetCoreEventBus/NetCoreEventBus.Web.Order/NetCoreEventBus.Web.Order.csproj", "NetCoreEventBus/NetCoreEventBus.Web.Order/"]
RUN dotnet restore "NetCoreEventBus/NetCoreEventBus.Web.Order/NetCoreEventBus.Web.Order.csproj"
COPY . .
WORKDIR "/src/NetCoreEventBus/NetCoreEventBus.Web.Order"
RUN dotnet build "NetCoreEventBus.Web.Order.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetCoreEventBus.Web.Order.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetCoreEventBus.Web.Order.dll"]
