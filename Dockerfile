# The ASP.NET Core runtime image from Microsoft's registry (the .NET images stay on MCR). The pipeline publishes the
# application first (dotnet publish -> publish/) and this image only adds it, so the code that was tested is the code that ships.
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY publish/ .

ARG BUILD_ID=local
ENV APP_BUILD=${BUILD_ID} \
    ASPNETCORE_URLS=http://+:8080
LABEL org.opencontainers.image.title="dotnet-api" \
      org.opencontainers.image.description=".NET REST API with Swagger (examples group)" \
      org.opencontainers.image.revision="${BUILD_ID}"

# The image ships a non-root user named "app"
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "DotnetApi.dll"]
