FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/Ipkiss.Api/Ipkiss.Api.csproj src/Ipkiss.Api/
RUN dotnet restore src/Ipkiss.Api/Ipkiss.Api.csproj
COPY src/ src/
RUN dotnet publish src/Ipkiss.Api/Ipkiss.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ipkiss.Api.dll"]
