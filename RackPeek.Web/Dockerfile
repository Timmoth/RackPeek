# -----------------------------
# Runtime base
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

ARG APP_UID=1000
ENV APP_UID=$APP_UID

WORKDIR /app
EXPOSE 8080

# -----------------------------
# Build stage
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["RackPeek.Web/RackPeek.Web.csproj", "RackPeek.Web/"]
COPY ["RackPeek.Domain/RackPeek.Domain.csproj", "RackPeek.Domain/"]
COPY ["Shared.Rcl/Shared.Rcl.csproj", "Shared.Rcl/"]
COPY ["RackPeek/RackPeek.csproj", "RackPeek/"]

RUN dotnet restore "RackPeek.Web/RackPeek.Web.csproj"

COPY . .

# Publish Web
WORKDIR "/src/RackPeek.Web"
RUN dotnet publish "./RackPeek.Web.csproj" -c $BUILD_CONFIGURATION -o /app/web-publish /p:UseAppHost=false

# Publish CLI
WORKDIR "/src/RackPeek"
RUN dotnet publish "./RackPeek.csproj" -c $BUILD_CONFIGURATION -o /app/cli-publish /p:UseAppHost=false


# -----------------------------
# Final runtime image
# -----------------------------
FROM base AS final

WORKDIR /app

USER root

# Create shared config directory safely
RUN mkdir -p /app/config \
    && chown -R ${APP_UID}:0 /app/config \
    && chmod -R g=u /app/config

VOLUME ["/app/config"]

# Copy published outputs
COPY --from=build /app/web-publish .
COPY --from=build /app/cli-publish /usr/local/bin/rpk-dir

# Create CLI wrapper
RUN if [ -f /usr/local/bin/rpk-dir/RackPeek ]; then \
        mv /usr/local/bin/rpk-dir/RackPeek /usr/local/bin/rpk; \
    else \
        echo '#!/bin/sh\nexec dotnet /usr/local/bin/rpk-dir/RackPeek.dll "$@"' > /usr/local/bin/rpk && \
        chmod +x /usr/local/bin/rpk; \
    fi

# Make sure ASP.NET binds correctly in containers
ENV ASPNETCORE_URLS=http://+:8080
ENV RPK_YAML_DIR=/app/config

# Drop privileges
USER ${APP_UID}

ENTRYPOINT ["dotnet", "RackPeek.Web.dll"]