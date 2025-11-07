# ASPNETCORE_URLS Configuration Guide

The `ASPNETCORE_URLS` environment variable configures the URLs that your ASP.NET Core application listens on. Here are the different ways to set it:

## 1. Using launchSettings.json (Development)

The `ASPNETCORE_URLS` variable is already configured in `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "environmentVariables": {
        "ASPNETCORE_URLS": "http://localhost:5000;https://localhost:5001"
      }
    }
  }
}
```

## 2. Using Environment Variables

### Command Line:
```bash
# Linux/Mac
export ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001"
dotnet run

# Windows PowerShell
$env:ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001"
dotnet run

# Windows Command Prompt
set ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001
dotnet run
```

### Terminal (one-time):
```bash
ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001" dotnet run
```

## 3. Using .env File (Local Development)

Create a `.env` file in the project root:
```
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001
```

The application is configured to automatically load this file in development.

## 4. Using Command Line Arguments

```bash
dotnet run --urls "http://localhost:5000;https://localhost:5001"
```

## 5. Using appsettings.json

Add to `appsettings.json` or `appsettings.Development.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001"
      }
    }
  }
}
```

## Common URL Patterns

### Local Development:
- `http://localhost:5000` - HTTP only
- `https://localhost:5001` - HTTPS only  
- `http://localhost:5000;https://localhost:5001` - Both HTTP and HTTPS

### Production/Docker:
- `http://*:80` - Listen on all interfaces, port 80
- `http://0.0.0.0:8080` - Listen on all interfaces, port 8080
- `http://+:5000;https://+:5001` - Listen on all interfaces

### AWS Lambda/Serverless:
- Not typically needed as the runtime handles URL binding

## Testing the Configuration

Run the application and check the console output:
```bash
dotnet run
```

Look for output like:
```
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
```

## Priority Order

ASP.NET Core uses this priority order for URL configuration:
1. Command line arguments (`--urls`)
2. `ASPNETCORE_URLS` environment variable
3. `applicationUrl` in launchSettings.json
4. Kestrel configuration in appsettings.json
5. Default URLs