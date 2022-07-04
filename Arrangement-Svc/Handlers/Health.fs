module Health

open Giraffe

let healthCheck: HttpHandler =
    route "/api/health" >=> Successful.OK "Health check: dette gikk fint"
