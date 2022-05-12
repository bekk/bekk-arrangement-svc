module Tools 

/// <summary>
/// f takes its (first) two arguments in the reverse order of f.
/// Nice to have when used with partial application/currying. 
/// </summary>
///<param name="f"> The function </param>
///<param name="a"> Argument 1 </param>
///<param name="b"> Argument 2 </param>
let flip f a b = f b a

/// <summary>
/// Try to parse an integer with an Option return type instead of exception
/// <code> 
///     tryParseInt "5" = Some 5
///     tryParseInt "hei" = None
/// </code>
/// </summary>
///<param name="s"> String representation of (hopefully) a number </param>
///<returns> Some integer or None </returns>
let tryParseInt (s:string) = 
    try 
        s |> int |> Some
    with :? System.FormatException -> 
        None

let tee f x =
    f x
    x