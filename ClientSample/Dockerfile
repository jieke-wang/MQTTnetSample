#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["ClientSample/ClientSample.csproj", "ClientSample/"]
RUN dotnet restore "ClientSample/ClientSample.csproj"
COPY . .
WORKDIR "/src/ClientSample"
RUN dotnet build "ClientSample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClientSample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClientSample.dll"]