module TypeHandlers

open Dapper
open Models

type OptionHandler<'T>() =
    inherit SqlMapper.TypeHandler<option<'T>>()

    override __.SetValue(param, value) = 
        let valueOrNull = 
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull    

    override __.Parse value =
        if isNull value then
            None
        else
            Some (value :?> 'T)


type OfficeOptionHandler() =
    inherit SqlMapper.TypeHandler<option<Office>>()

    override __.SetValue(param, value) = 
        let valueOrNull =
            value
            |> Option.map (Office.toString >> box)
            |> Option.defaultValue null
        
        param.Value <- valueOrNull

    override __.Parse value =
        if isNull value then
            None
        else
            value :?> string |> Office.fromString

module DapperConfig =

    let RegisterTypeHandlers () =
        SqlMapper.AddTypeHandler(new OptionHandler<string>())
        SqlMapper.AddTypeHandler(new OptionHandler<int>())
        SqlMapper.AddTypeHandler(new OfficeOptionHandler())
