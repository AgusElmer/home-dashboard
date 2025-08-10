# ─────────────────────────────────────────────
# Stage 1	Build	backend
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-backend
WORKDIR /src
COPY backend/HomeDashboard.Api.csproj backend/
RUN dotnet restore backend/HomeDashboard.Api.csproj
COPY backend/ backend/
WORKDIR /src/backend
RUN dotnet publish -c Release -o /app/build

# ─────────────────────────────────────────────
# Stage 2	Build	frontend
# ─────────────────────────────────────────────
FROM node:20 AS build-frontend
WORKDIR /src
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ .
RUN npm run build      # outputs to /src/dist

# ─────────────────────────────────────────────
# Stage 3	Runtime (	production	)
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build-backend /app/build .
COPY --from=build-frontend /src/dist wwwroot
USER 1000    # non	root
EXPOSE 8080
ENTRYPOINT ["dotnet", "HomeDashboard.Api.dll"]

# ─────────────────────────────────────────────
# Stage 4	Runtime (	debug for VS Code	)
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS debug
WORKDIR /app
COPY --from=build-backend /app/build .
COPY --from=build-frontend /src/dist wwwroot

# install vsdbg
RUN apt-get update && apt-get install -y --no-install-recommends curl unzip \
 && rm -rf /var/lib/apt/lists/* \
 && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

# Labels 	VS Code Docker ext. shows “Attach Visual Studio Code”
LABEL com.microsoft.visualstudio.debug="true"
LABEL com.microsoft.visualstudio.debug.1="vsdbg;transport=dt_socket;address=0.0.0.0:4024"

EXPOSE 8080 4024
ENTRYPOINT ["dotnet","HomeDashboard.Api.dll"]
