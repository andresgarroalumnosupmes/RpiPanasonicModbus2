# Usa la imagen oficial de .NET 6.0 para ejecutar aplicaciones
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Establece el directorio de trabajo del contenedor
WORKDIR /app

# Copia el archivo de proyecto y restaura las dependencias
COPY *.csproj ./
RUN dotnet restore

# Copia el resto del código fuente y compila la aplicación
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Usa una imagen más ligera solo con el runtime para ejecutar la app
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS runtime

# Establece el directorio de trabajo del contenedor
WORKDIR /app
COPY --from=build /app/publish .

# Define environment variable
ENV NAME PanasonicModbus2

# Comando de inicio del contenedor
CMD ["dotnet", "PanasonicModbus2.dll"]