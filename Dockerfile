# Usa imagen ARM64 específica para SDK
FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Establece el directorio de trabajo del contenedor
WORKDIR /app

# Copia el archivo de proyecto y restaura las dependencias
COPY *.csproj ./
RUN dotnet restore

# Copia el resto del código fuente y compila la aplicación
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Usa imagen ARM64 específica para runtime
FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/runtime:6.0 AS runtime

# Establece el directorio de trabajo del contenedor
WORKDIR /app
COPY --from=build /app/publish .

# Define environment variable
ENV NAME PanasonicModbus2

# Comando de inicio del contenedor
CMD ["dotnet", "PanasonicModbus2.dll"]