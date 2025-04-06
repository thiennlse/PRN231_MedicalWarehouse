# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MedicalWarehouse_API/MedicalWarehouse_API.csproj", "MedicalWarehouse_API/"]
COPY ["MedicalWarehouse_Services/MedicalWarehouse_Services.csproj", "MedicalWarehouse_Services/"]
COPY ["MedicalWarehouse_Repository/MedicalWarehouse_Repository.csproj", "MedicalWarehouse_Repository/"]
COPY ["MedicalWarehouse_BusinessObject/MedicalWarehouse_BusinessObject.csproj", "MedicalWarehouse_BusinessObject/"]
RUN dotnet restore "./MedicalWarehouse_API/MedicalWarehouse_API.csproj"
COPY . .
WORKDIR "/src/MedicalWarehouse_API"
RUN dotnet build "./MedicalWarehouse_API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MedicalWarehouse_API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MedicalWarehouse_API.dll"]