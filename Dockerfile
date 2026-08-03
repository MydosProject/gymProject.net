FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/NO23.Web/*.csproj ./src/NO23.Web/
RUN dotnet restore src/NO23.Web/NO23.Web.csproj

COPY . .
WORKDIR /app/src/NO23.Web
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "NO23.Web.dll"]
