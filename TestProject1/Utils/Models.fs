module Models

open Thoth.Json.Net

type InnerEvent = {
    id: string
    title: string
}

type CreatedEvent = {
    editToken: string
    event: InnerEvent
}

let innerEventDecoder: Decoder<InnerEvent> =
        Decode.object (fun get ->
        {
            id = get.Required.Field "id" Decode.string
            title = get.Required.Field "title" Decode.string
        })

let createdEventDecoder: Decoder<CreatedEvent> =
        Decode.object (fun get ->
        {
            editToken = get.Required.Field "editToken" Decode.string
            event = get.Required.Field "event" innerEventDecoder
        })
