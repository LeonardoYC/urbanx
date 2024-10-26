FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore
    
# Copy everything else and build
COPY . ./
# Ensure Rotativa executables are marked as executable
RUN chmod +x Rotativa/Windows/wkhtmltopdf.exe
RUN chmod +x Rotativa/Windows/wkhtmltoimage.exe
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install Wine to run Windows executables
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    wine64 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create Rotativa directory
RUN mkdir -p /app/Rotativa/Windows

# Copy Rotativa executables separately to ensure they're included
COPY --from=build-env /app/Rotativa/Windows/wkhtmltopdf.exe /app/Rotativa/Windows/
COPY --from=build-env /app/Rotativa/Windows/wkhtmltoimage.exe /app/Rotativa/Windows/

# Make sure the executables have proper permissions
RUN chmod +x /app/Rotativa/Windows/wkhtmltopdf.exe
RUN chmod +x /app/Rotativa/Windows/wkhtmltoimage.exe

# Copy the rest of the application
COPY --from=build-env /app/out .

ENV APP_NET_CORE urbanx.dll 
ENV WINEDEBUG=-all
ENV WINEPREFIX=/app/.wine

# Initialize Wine (this might take a moment)
RUN wine wineboot --init

CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE
