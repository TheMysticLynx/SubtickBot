FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

WORKDIR /src

COPY SubtickBot.sln SubtickBot.sln
COPY SubtickBot SubtickBot
COPY SubtickBot.Data SubtickBot.Data
COPY SubtickBot.Tests SubtickBot.Tests

RUN dotnet restore --use-current-runtime

WORKDIR /publish
COPY /Resources Resources

WORKDIR /src
RUN dotnet publish -c Release --use-current-runtime --self-contained false --no-restore -o /publish

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS runtime
WORKDIR /publish
COPY --from=build-env /publish .
ENTRYPOINT ["dotnet", "SubtickBot.dll"]