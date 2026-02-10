# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SecureFileStatementDelivery.sln ./
COPY src/ ./src/

RUN dotnet restore ./SecureFileStatementDelivery.sln
RUN dotnet publish ./src/SecureFileStatementDelivery.Api/SecureFileStatementDelivery.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DATA_DIR=/data

EXPOSE 8080

VOLUME ["/data"]

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "SecureFileStatementDelivery.Api.dll"]
