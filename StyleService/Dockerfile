FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Установим libgdiplus для System.Drawing на Linux
RUN apt-get update && apt-get install -y libgdiplus libc6-dev && rm -rf /var/lib/apt/lists/*
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
# Восстановим зависимости и соберём
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://*:80
ENTRYPOINT ["dotnet", "StyleService.dll"]