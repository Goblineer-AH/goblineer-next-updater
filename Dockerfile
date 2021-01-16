FROM mcr.microsoft.com/dotnet/sdk:5.0 as builder

RUN mkdir /app
WORKDIR /app

COPY ./GoblineerNextUpdater/*.csproj ./
RUN dotnet restore

COPY ./GoblineerNextUpdater/* ./

RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/runtime:5.0

WORKDIR /app
COPY --from=builder /app/out .
ENTRYPOINT [ "dotnet", "GoblineerNextUpdater.dll" ]