# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8.0 ASP.NET Core Web API project for JWT authentication. The project is a minimal API setup with Swagger documentation enabled for development.

## Architecture

- **Framework**: ASP.NET Core 8.0 Web API
- **Project Structure**: Single project solution (`JwtAuth.sln` -> `JwtAuth/JwtAuth.csproj`)
- **Configuration**: Standard ASP.NET Core configuration with `appsettings.json` and development settings
- **Documentation**: Swagger/OpenAPI integration for API documentation

## Development Commands

### Build and Run
- Build: `dotnet build`
- Run: `dotnet run` (runs on http://localhost:5174, https://localhost:7053)
- Clean: `dotnet clean`

### Development URLs
- HTTP: http://localhost:5174
- HTTPS: https://localhost:7053
- Swagger UI: Available at `/swagger` when running in Development environment

### Testing
- Run tests: `dotnet test` (when test projects are added)

## Key Files

- `Program.cs`: Application entry point and service configuration
- `appsettings.json`: Main configuration file
- `appsettings.Development.json`: Development-specific settings
- `JwtAuth.http`: HTTP request examples for testing endpoints
- `Properties/launchSettings.json`: Launch profiles for different environments

## Notes

- Project currently has minimal setup - no controllers, models, or JWT implementation yet
- Swagger is enabled for development environment only
- Uses standard ASP.NET Core middleware pipeline with HTTPS redirection and authorization