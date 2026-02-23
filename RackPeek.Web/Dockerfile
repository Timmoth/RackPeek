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

# Build Web
WORKDIR "/src/RackPeek.Web"
RUN dotnet publish "./RackPeek.Web.csproj" -c $BUILD_CONFIGURATION -o /app/web-publish /p:UseAppHost=false

# Build CLI (framework dependent, small)
WORKDIR "/src/RackPeek"
RUN dotnet publish "./RackPeek.csproj" -c $BUILD_CONFIGURATION -o /app/cli-publish /p:UseAppHost=false
FROM base AS final

WORKDIR /app

USER root

# Create shared config dir
RUN mkdir -p /app/config && chown -R $APP_UID /app/config
VOLUME ["/app/config"]

# Copy Web app
COPY --from=build /app/web-publish .

# Copy CLI publish output
COPY --from=build /app/cli-publish /usr/local/bin/rpk-dir

# If AppHost exists, use it. Otherwise create wrapper.
RUN if [ -f /usr/local/bin/rpk-dir/RackPeek ]; then \
        mv /usr/local/bin/rpk-dir/RackPeek /usr/local/bin/rpk; \
    else \
        echo '#!/bin/sh\nexec dotnet /usr/local/bin/rpk-dir/RackPeek.dll "$@"' > /usr/local/bin/rpk && \
        chmod +x /usr/local/bin/rpk; \
    fi

# Set environment variable so CLI uses shared config
ENV RPK_YAML_DIR=/app/config

USER $APP_UID

ENTRYPOINT ["dotnet", "RackPeek.Web.dll"]