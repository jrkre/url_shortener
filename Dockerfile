FROM node:18 AS react-build

WORKDIR /app

# Copy React app source files
COPY ClientApp ./ClientApp/

WORKDIR /app/ClientApp
RUN npm install && npm run build

# COPY ClientApp/ ./
# RUN npm run build


# backend/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install EF tools (optional, if running migrations in container)
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /app
COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN mkdir -p /var/dpkeys
RUN dotnet publish -c Release -o out

# Run migrations if needed - it brokey on docker compose build becuase it requires a database connection
# RUN dotnet ef database update

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out ./
COPY --from=react-build /app/ClientApp/build ./ClientApp/build


EXPOSE 5000
ENTRYPOINT ["dotnet", "url_shortener.dll"]
