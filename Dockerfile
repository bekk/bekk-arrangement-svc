FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /app

RUN apk add --update nodejs npm netcat-openbsd

COPY Frontend/. frontend/.
COPY Arrangement-Svc/. backend/Arrangement-Svc/.
COPY Migration/. backend/migration/.

WORKDIR /app/frontend
RUN npm ci
RUN npm run build
WORKDIR /app/backend
RUN ls
WORKDIR /app/backend/Arrangement-Svc
RUN dotnet restore
RUN dotnet publish -c release -o out

# RUN
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine
WORKDIR /app/backend/Arrangement-Svc/
COPY --from=build /app/backend/Arrangement-Svc/out .
COPY --from=build /app/frontend/build wwwroot/.
ENV VIRTUAL_PATH="/arrangement-svc"
ENV PORT=80
CMD dotnet backend.dll
