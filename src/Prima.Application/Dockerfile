FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Prima.Application/Prima.Application.csproj", "Prima.Application/"]
RUN dotnet restore "Prima.Application/Prima.Application.csproj"
COPY . .
WORKDIR "/src/Prima.Application"
RUN dotnet build "Prima.Application.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Prima.Application.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Prima.Application.dll"]
