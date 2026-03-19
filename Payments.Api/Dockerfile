FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Payments.sln .
COPY Payments.Api/Payments.Api.csproj Payments.Api/
COPY Payments.Application/Payments.Application.csproj Payments.Application/
COPY Payments.Domain/Payments.Domain.csproj Payments.Domain/
COPY Payments.Infra/Payments.Infra.csproj Payments.Infra/
RUN dotnet restore Payments.Api/Payments.Api.csproj

COPY . .
RUN dotnet publish Payments.Api/Payments.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5004
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
EXPOSE 5004
ENTRYPOINT ["dotnet", "Payments.Api.dll"]
