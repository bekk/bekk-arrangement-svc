FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

RUN apk add --update nodejs npm netcat-openbsd

COPY . .

WORKDIR /Frontend
RUN npm ci
RUN npm run build
WORKDIR /Arrangement-Svc
RUN dotnet restore
RUN dotnet publish -c release -o out

# RUN
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine
WORKDIR /Arrangement-Svc/
COPY --from=build /Arrangement-Svc/out .
COPY --from=build /Frontend/build wwwroot/.
CMD dotnet arrangementSvc.dll
