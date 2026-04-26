FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PlataformaCreditos.csproj", "./"]
RUN dotnet restore "PlataformaCreditos.csproj"

COPY . .
# La bandera /p:UseAppHost=false evita crear binarios de Windows en Linux
RUN dotnet publish "PlataformaCreditos.csproj" -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# Forzamos el puerto estándar de .NET 8 en contenedores
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PlataformaCreditos.dll"]