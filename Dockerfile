FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy only the solution file first
COPY *.sln .

# Copy all project files first
COPY src/*/*.csproj ./
RUN for file in *.csproj; do mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; done

# Restore as distinct layers
RUN dotnet restore

# Copy everything else
COPY src ./src

# Build and publish
RUN dotnet publish src/JetPay.TonWatcher/JetPay.TonWatcher.csproj -c Release -o /app/publish /p:CopyLocalLockFileAssemblies=true

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

# Expose port for Railway
EXPOSE 8080

# Set environment variables for Railway
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "JetPay.TonWatcher.dll"] 