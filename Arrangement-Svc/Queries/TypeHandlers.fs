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

type EventTypeHandler() =
    inherit SqlMapper.TypeHandler<EventType>()

    override __.SetValue(param, value) =
        let valueOrNull = value.ToString()
        param.Value <- valueOrNull

    override __.Parse value =
        match value.ToString() with
        | "Faglig" -> Faglig
        | "Sosialt" -> Sosialt
        | s -> failwith $"'{s}' er ikke en gyldig EventType"

module DapperConfig =

    let RegisterTypeHandlers () =
        SqlMapper.AddTypeHandler(EventTypeHandler())
        SqlMapper.AddTypeHandler(OptionHandler<string>())
        SqlMapper.AddTypeHandler(OptionHandler<int>())
