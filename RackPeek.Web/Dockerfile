FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RackPeek.Web/RackPeek.Web.csproj", "RackPeek.Web/"]
COPY ["RackPeek.Domain/RackPeek.Domain.csproj", "RackPeek.Domain/"]
COPY ["Shared.Rcl/Shared.Rcl.csproj", "Shared.Rcl/"]
COPY ["RackPeek/RackPeek.csproj", "RackPeek/"]
RUN dotnet restore "RackPeek.Web/RackPeek.Web.csproj"
COPY . .
WORKDIR "/src/RackPeek.Web"
RUN dotnet build "./RackPeek.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RackPeek.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

RUN mkdir -p /app/config && chown -R $APP_UID /app/config

VOLUME ["/app/config"]

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RackPeek.Web.dll"]