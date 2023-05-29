FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview AS build-env

WORKDIR /src

COPY SubtickBot/*.csproj .
RUN dotnet restore --use-current-runtime

COPY SubtickBot SubtickBot

WORKDIR /publish
COPY /Resources Resources

WORKDIR /src
RUN dotnet publish -c Release --use-current-runtime --self-contained false --no-restore -o /publish

FROM mcr.microsoft.com/dotnet/nightly/runtime:8.0-preview AS runtime
WORKDIR /publish
COPY --from=build-env /publish .
ENTRYPOINT ["dotnet", "SubtickBot.dll"]