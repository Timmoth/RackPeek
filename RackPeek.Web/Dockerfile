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

# Directory expected to be mounted by the user
VOLUME ["/app/config"]

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RackPeek.Web.dll"]
