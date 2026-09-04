FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["nuget.config", "./"]
COPY ["src/API/Aggregator.Api/Aggregator.Api.csproj", "src/API/Aggregator.Api/"]
COPY ["src/Common/Aggregator.Common.Application/Aggregator.Common.Application.csproj", "src/Common/Aggregator.Common.Application/"]
COPY ["src/Common/Aggregator.Common.Domain/Aggregator.Common.Domain.csproj", "src/Common/Aggregator.Common.Domain/"]
COPY ["src/Common/Aggregator.Common.Infrastructure/Aggregator.Common.Infrastructure.csproj", "src/Common/Aggregator.Common.Infrastructure/"]
COPY ["src/Common/Aggregator.Common.Presentation/Aggregator.Common.Presentation.csproj", "src/Common/Aggregator.Common.Presentation/"]
COPY ["src/Modules/Aggregation/Aggregator.Aggregation.Application/Aggregator.Aggregation.Application.csproj", "src/Modules/Aggregation/Aggregator.Aggregation.Application/"]
COPY ["src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/Aggregator.Aggregation.Infrastructure.csproj", "src/Modules/Aggregation/Aggregator.Aggregation.Infrastructure/"]
COPY ["src/Modules/Aggregation/Aggregator.Aggregation.Presentation/Aggregator.Aggregation.Presentation.csproj", "src/Modules/Aggregation/Aggregator.Aggregation.Presentation/"]
RUN dotnet restore "./src/API/Aggregator.Api/Aggregator.Api.csproj"
COPY . .
WORKDIR "/src/src/API/Aggregator.Api"
RUN dotnet build "./Aggregator.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Aggregator.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aggregator.Api.dll"]
