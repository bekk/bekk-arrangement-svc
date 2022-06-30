FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build-env
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o out ./Arrangement-Svc/Arrangement-Svc.fsproj

FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY --from=build-env /app/out .

ENV VIRTUAL_PATH="/arrangement-svc"
ENV PORT=80
CMD dotnet arrangementSvc.dll
