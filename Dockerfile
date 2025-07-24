# Stage 1: Build the ASP.NET Core backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-backend
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY backend/HomeDashboard.Api.csproj backend/
RUN dotnet restore backend/HomeDashboard.Api.csproj

# Copy everything else and build
COPY backend/ backend/
WORKDIR /src/backend
RUN dotnet publish -c Release -o /app/build

# Stage 2: Build the React frontend
FROM node:20 AS build-frontend
WORKDIR /src

# Copy package.json and install dependencies
COPY frontend/package.json frontend/package-lock.json ./
RUN npm install

# Copy the rest of the frontend code and build
COPY frontend/ .
RUN npm run build

# Stage 3: Create the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the built backend
COPY --from=build-backend /app/build .

# Create a non-root user and group
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Create the data directory and set permissions
# This ensures the directory exists and the appuser has write permissions
RUN mkdir -p /app/data && chown appuser:appgroup /app/data && chmod 770 /app/data

# Switch to the non-root user
USER appuser

# Copy the built frontend into the wwwroot folder of the backend
COPY --from=build-frontend /src/dist wwwroot

# Expose the port the app will run on
EXPOSE 8080

# Set the entrypoint
ENTRYPOINT ["dotnet", "HomeDashboard.Api.dll"]
