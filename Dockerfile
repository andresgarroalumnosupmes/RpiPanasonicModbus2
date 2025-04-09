# Usa la imagen oficial de .NET 6.0 para ejecutar aplicaciones
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Establece el directorio de trabajo del contenedor
WORKDIR /app

# Socket port from the Listener (IoTEdge,) will listen
EXPOSE 8888

# Copia el archivo de proyecto y restaura las dependencias. Evita que se ejecuten varias dependencias en paralelo
COPY *.csproj ./
RUN dotnet restore --disable-parallel

# Copia el resto del código fuente, .net compila la aplicación, crea un ejecutable para arquitecturas arm64 como la Raspberry Pi5, se le indica q el run time ARM no esta embebido
COPY . ./
RUN dotnet publish -c Release -o /app/publish -r linux-arm64 --self-contained false

# Usa una imagen más ligera solo con el runtime para ejecutar la app (versión ARM64 y so bookworn que corre en la Raspberry Pi5)
FROM mcr.microsoft.com/dotnet/runtime:6.0-bookworm-slim-arm64v8 AS runtime


# Establece el directorio de trabajo del contenedor
WORKDIR /app
COPY --from=build /app/publish .

# Define environment variable
ENV NAME PanasonicModbus2

# Comando de inicio del contenedor
CMD ["dotnet", "PanasonicModbus2.dll"]