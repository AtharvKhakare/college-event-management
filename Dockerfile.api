# Multi-stage Docker build for CollegeEventManagement.API (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching layer
COPY ["CollegeEventManagement.Core/CollegeEventManagement.Core.csproj", "CollegeEventManagement.Core/"]
COPY ["CollegeEventManagement.Infrastructure/CollegeEventManagement.Infrastructure.csproj", "CollegeEventManagement.Infrastructure/"]
COPY ["CollegeEventManagement.API/CollegeEventManagement.API.csproj", "CollegeEventManagement.API/"]

RUN dotnet restore "CollegeEventManagement.API/CollegeEventManagement.API.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/CollegeEventManagement.API"
RUN dotnet publish "CollegeEventManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render exposes the PORT environment variable (default 8080)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CollegeEventManagement.API.dll"]
