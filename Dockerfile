# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SiteAttendance/SiteAttendance.csproj", "SiteAttendance/"]
RUN dotnet restore "SiteAttendance/SiteAttendance.csproj"

COPY SiteAttendance/ SiteAttendance/
WORKDIR "/src/SiteAttendance"
RUN dotnet build "SiteAttendance.csproj" -c Release -o /app/build
RUN dotnet publish "SiteAttendance.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SiteAttendance.dll"]

