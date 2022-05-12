module Health

open Giraffe

let healthCheck: HttpHandler =
    route "/health" >=> Successful.OK "Health check: dette gikk fint"
