FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore
    
# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install wkhtmltopdf and its dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    wkhtmltopdf \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    fontconfig \
    libxext6 \
    libxrender1 \
    xfonts-75dpi \
    xfonts-base \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create directory for wkhtmltopdf
RUN mkdir -p /app/Rotativa/Linux

# Create symbolic links to wkhtmltopdf binaries
RUN ln -s /usr/bin/wkhtmltopdf /app/Rotativa/Linux/wkhtmltopdf \
    && ln -s /usr/bin/wkhtmltoimage /app/Rotativa/Linux/wkhtmltoimage

# Copy app files
COPY --from=build-env /app/out .

ENV APP_NET_CORE urbanx.dll 

CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE