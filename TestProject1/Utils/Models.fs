module Models

type CreatedEvent = {
    id: string
    shortName: string option
    isCancelled: bool
    editToken: string
    event: Models.EventWriteModel
}
