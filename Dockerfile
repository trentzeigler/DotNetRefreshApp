# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["DotNetRefreshApp.csproj", "./"]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/publish .

# Configure app to listen on port 5000
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

# Run the app
ENTRYPOINT ["dotnet", "DotNetRefreshApp.dll"]
