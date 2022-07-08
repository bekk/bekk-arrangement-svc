FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

RUN apk add --update nodejs npm netcat-openbsd

WORKDIR /app
COPY . .

WORKDIR /app/Frontend
RUN npm ci
RUN npm run build
WORKDIR /app/Arrangement-Svc
RUN dotnet publish -c release -o out

# RUN
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine
WORKDIR /app/
COPY --from=build /app/Arrangement-Svc/out .
COPY --from=build /app/Arrangement-Svc/wwwroot wwwroot/.
COPY --from=build /app/Frontend/build wwwroot/.
CMD dotnet arrangementSvc.dll
