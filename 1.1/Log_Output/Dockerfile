FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Log_Output.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
# Disable .NET Core diagnostics to reduce noise in logs
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "Log_Output.dll"]
