module Email.SendgridApiModels

type SendGridEmailAddress =
    { Email: string
    }

type Personalization =
    { To: SendGridEmailAddress list
    }

type Content =
    { Type: string
      Value: string 
    }

type Attachment =
    { Content: string
      Type: string
      Filename: string 
    }

type SendGridFormat =
    { Personalizations: Personalization list
      From: SendGridEmailAddress
      Subject: string
      Content: Content list
      Attachments: Attachment list option
    }

type SendgridOptions =
    { ApiKey: string
      SendgridUrl: string 
    }
