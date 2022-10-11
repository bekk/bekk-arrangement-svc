module Health

open Giraffe
open GiraffeHelpers

let healthCheck: HttpHandler =
    choose [
        route "/health" >=> Successful.OK "Health check: dette gikk fint"
        route "/api/health" >=> Successful.OK "Health check: dette gikk fint"
    ]
