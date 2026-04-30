FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DevNetControl.Api/DevNetControl.Api.csproj", "DevNetControl.Api/"]
RUN dotnet restore "DevNetControl.Api/DevNetControl.Api.csproj"
COPY ["DevNetControl.Api/", "DevNetControl.Api/"]
WORKDIR "/src/DevNetControl.Api"
RUN dotnet build "DevNetControl.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DevNetControl.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "DevNetControl.Api.dll"]
