FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TSQR.Gateway.slnx .
COPY src/TSQR.Gateway.Api/ ./src/TSQR.Gateway.Api/

RUN dotnet restore ./src/TSQR.Gateway.Api/TSQR.Gateway.Api.csproj
RUN dotnet publish ./src/TSQR.Gateway.Api/TSQR.Gateway.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TSQR.Gateway.Api.dll"]
