# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copiar los archivos del proyecto y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto de los archivos y publicar la aplicación
COPY . ./
RUN dotnet publish -c Release -o out

# Etapa final: Imagen para la aplicación ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copiar los archivos publicados desde la etapa de build
COPY --from=build-env /app/out .

# Copiar wkhtmltopdf.exe a la ruta esperada por Rotativa
COPY Rotativa/Windows/wkhtmltopdf.exe /app/Rotativa/Windows 

COPY Rotativa/Windows/wkhtmltoimage.exe /app/Rotativa/Windows 

# Definir el nombre del ensamblado principal
ENV APP_NET_CORE urbanx.dll

# Exponer el puerto (ajusta según tu entorno)
EXPOSE 5000

# Comando para ejecutar la aplicación
CMD ASPNETCORE_URLS=http://*:$PORT dotnet $APP_NET_CORE
