module Task

open System.Threading.Tasks

let bind (f: 'x -> 'y Task) (t: 'x Task): 'y Task =
    task {
        let! x = t
        return! f x
    }

let map (f: 'x -> 'y) (t: 'x Task): 'y Task =
    task {
        let! x = t
        return f x
    }

let wrap (x: 'x): 'x Task =
    task {
        return x
    }

let sequence (xs : Task<'x> seq) : Task<'x seq> =
    Task.WhenAll(xs) |> map Array.toSeq

let whenAll (xs: Task seq) : Task =
    Task.WhenAll(xs)
