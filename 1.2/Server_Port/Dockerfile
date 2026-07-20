FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Server_Port.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Quiet logs
ENV DOTNET_EnableDiagnostics=0
# Default port if Deployment does not set PORT
ENV PORT=3000
ENTRYPOINT ["dotnet", "Server_Port.dll"]
