namespace ArrangementService

open Giraffe

module Health =
    let healthCheck: HttpHandler =
        route "/health" >=> Successful.OK "Health check: dette gikk fint"
