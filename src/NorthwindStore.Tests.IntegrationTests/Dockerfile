FROM mcr.microsoft.com/dotnet/sdk:6.0

WORKDIR /src
COPY ["packages/", "packages/"]
COPY ["nuget.config", "."]
COPY ["NorthwindStore.Tests.IntegrationTests/NorthwindStore.Tests.IntegrationTests.csproj", "NorthwindStore.Tests.IntegrationTests/"]
COPY ["NorthwindStore.App/NorthwindStore.App.csproj", "NorthwindStore.App/"]
COPY ["NorthwindStore.DAL/NorthwindStore.DAL.csproj", "NorthwindStore.DAL/"]
COPY ["NorthwindStore.BL/NorthwindStore.BL.csproj", "NorthwindStore.BL/"]
RUN dotnet restore "NorthwindStore.Tests.IntegrationTests/NorthwindStore.Tests.IntegrationTests.csproj"

COPY . .
WORKDIR "/src/NorthwindStore.Tests.IntegrationTests"
RUN dotnet build "NorthwindStore.Tests.IntegrationTests.csproj"

ENTRYPOINT ["dotnet", "test", "NorthwindStore.Tests.IntegrationTests.csproj", "--no-build"]