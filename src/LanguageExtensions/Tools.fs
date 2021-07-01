namespace ArrangementService

module Tools =

    /// <summary>
    /// f takes its (first) two arguments in the reverse order of f.
    /// Nice to have when used with partial application/currying. 
    /// </summary>
    ///<param name="f"> The function </param>
    ///<param name="a"> Argument 1 </param>
    ///<param name="b"> Argument 2 </param>
    let flip f a b = f b a
