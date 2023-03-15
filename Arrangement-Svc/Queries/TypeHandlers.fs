module TypeHandlers

open Dapper

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


module DapperConfig =

    let RegisterTypeHandlers () =
        SqlMapper.AddTypeHandler(OptionHandler<string>())
        SqlMapper.AddTypeHandler(OptionHandler<int>())
