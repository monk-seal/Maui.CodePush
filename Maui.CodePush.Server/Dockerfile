FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Maui.CodePush.Server/Maui.CodePush.Server.csproj", "Maui.CodePush.Server/"]
RUN dotnet restore "Maui.CodePush.Server/Maui.CodePush.Server.csproj"
COPY Maui.CodePush.Server/ Maui.CodePush.Server/
WORKDIR /src/Maui.CodePush.Server
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /app/uploads

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Maui.CodePush.Server.dll"]
