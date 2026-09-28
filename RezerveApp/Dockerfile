# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["RezerveApp.csproj", "./"]
RUN dotnet restore "RezerveApp.csproj"

# Copy everything else and build the app
COPY . .
WORKDIR "/app"
RUN dotnet build "RezerveApp.csproj" -c Release -o /app/build

# Publish the app
RUN dotnet publish "RezerveApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80 and set the environment variables
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "RezerveApp.dll"]
