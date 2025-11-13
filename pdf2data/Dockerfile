# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore --runtime linux-x64

# Copy source code and build
COPY . ./
RUN dotnet publish --configuration Release --framework net8.0 --runtime linux-x64 --output out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install dependencies for PDF processing
# RUN apt-get update && \
#     apt-get install -y \
#     libgdiplus \
#     libc6-dev \
#     libx11-dev \
#     && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=build /app/out .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
# Expose port (for lambda container)
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "pdf2data.dll"]