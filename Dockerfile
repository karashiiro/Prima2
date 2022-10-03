FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "/src/src/Prima.Application/Prima.Application.csproj"
RUN dotnet build "/src/src/Prima.Application/Prima.Application.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "/src/src/Prima.Application/Prima.Application.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Prima.Application.dll"]
