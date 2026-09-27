# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Lab.Api/Lab.Api.csproj src/Lab.Api/
RUN dotnet restore src/Lab.Api/Lab.Api.csproj
COPY src/Lab.Api/ src/Lab.Api/
RUN dotnet publish src/Lab.Api/Lab.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
ARG APP_VERSION=dev
ENV APP_VERSION=${APP_VERSION}
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lab.Api.dll"]
