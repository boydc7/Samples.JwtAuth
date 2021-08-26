FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env

WORKDIR /usr/src/authapi

COPY Samples.JwtAuth.Api/ ./Samples.JwtAuth.Api
COPY Samples.JwtAuth.sln .

RUN dotnet restore && dotnet publish Samples.JwtAuth.Api/Samples.JwtAuth.Api.csproj -c Release -o publish

################################################################
# Deployment
################################################################
FROM mcr.microsoft.com/dotnet/aspnet:5.0

WORKDIR /opt/authapi

COPY --from=build-env /usr/src/authapi/publish /opt/authapi

ENV ASPNETCORE_URLS=http://+:8084
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0

EXPOSE 8084

ENTRYPOINT ["/opt/authapi/authapi"]
