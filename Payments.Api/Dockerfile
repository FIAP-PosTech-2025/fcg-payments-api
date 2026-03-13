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

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Payments.Api.dll"]
