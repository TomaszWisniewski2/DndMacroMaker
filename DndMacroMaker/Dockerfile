FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DndMacroMaker.csproj", "./"]
RUN dotnet restore "DndMacroMaker.csproj"
COPY . .
RUN dotnet build "DndMacroMaker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DndMacroMaker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY wwwroot ./wwwroot
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENTRYPOINT ["dotnet", "DndMacroMaker.dll"]
