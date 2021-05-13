#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["RpcClientSample/RpcClientSample.csproj", "RpcClientSample/"]
RUN dotnet restore "RpcClientSample/RpcClientSample.csproj"
COPY . .
WORKDIR "/src/RpcClientSample"
RUN dotnet build "RpcClientSample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RpcClientSample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RpcClientSample.dll"]