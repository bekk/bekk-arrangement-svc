module Seq

/// <summary>Same as `Seq.skip` except it returns `Seq.empty` if
///  you try to skip more elements than the sequence is long
///  instead of throwing</summary>
let safeSkip n list =
    list
    |> Seq.mapi (fun i e -> (e, i))
    |> Seq.skipWhile (fun (_, i) -> i < n)
    |> Seq.map fst


let fromDict dictionary =
    dictionary
    |> Seq.map (|KeyValue|)

