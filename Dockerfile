FROM node:17.3.0 AS node_build

WORKDIR /app
COPY . .

WORKDIR /app/Frontend
RUN npm ci
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS dotnet_build

WORKDIR /app
COPY . .

WORKDIR /app/Arrangement-Svc
RUN dotnet publish -c release -o out

# RUN
# NOTE: When using Alpine, createEvent requests mysteriously dies at decoding
# the body. This does not happen locally or using Debian. Before changing back
# to Alpine, make sure the application actually works from Docker
FROM bekkforvaltning/aspnet-datadog-instrumented:6.0
WORKDIR /app/
COPY --from=dotnet_build /app/Arrangement-Svc/out .
COPY --from=dotnet_build /app/Arrangement-Svc/wwwroot wwwroot/.
COPY --from=node_build /app/Frontend/build/. wwwroot/.
ENV ASPNETCORE_URLS="http://+:80"
ENV PORT=80

CMD dotnet Arrangement-Svc.dll
