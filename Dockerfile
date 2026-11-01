# Imagen del frontend (Blazor WebAssembly servido por nginx sin privilegios).
#   docker build -t fitnessfrontend .
#   docker run -p 8081:8080 -e URL_BASE_API=https://api.ejemplo.edu.co fitnessfrontend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/FitnessFrontend/FitnessFrontend.csproj src/FitnessFrontend/
RUN dotnet restore src/FitnessFrontend/FitnessFrontend.csproj
COPY src/ src/
RUN dotnet publish src/FitnessFrontend/FitnessFrontend.csproj -c Release -o /out --no-restore \
    # appsettings.json queda como plantilla: el script de arranque la regenera con URL_BASE_API
    && mv /out/wwwroot/appsettings.json /out/wwwroot/appsettings.template.json \
    && rm -f /out/wwwroot/appsettings.json.br /out/wwwroot/appsettings.json.gz

FROM nginxinc/nginx-unprivileged:alpine
# El directorio debe ser escribible por el usuario sin privilegios (101) para generar appsettings.json al arrancar
USER root
RUN chown 101:101 /usr/share/nginx/html
USER 101
COPY --from=build --chown=101:101 /out/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --chmod=755 docker/40-url-api.sh /docker-entrypoint.d/40-url-api.sh
EXPOSE 8080
