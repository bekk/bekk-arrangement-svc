[<RequireQualifiedAccess>]
module DateTimeCustom

open System
open Thoth.Json.Net

[<CustomComparison; CustomEquality>]
type Date =
    { Day: int
      Month: int
      Year: int
    }

    member this.ToTuple = (this.Year, this.Month, this.Day)

    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | :? Date as other ->
                let thisDate = this.ToTuple
                let otherDate = other.ToTuple
                if thisDate > otherDate then 1
                else if thisDate < otherDate then -1
                else 0
            | _ -> 0

    override this.Equals obj =
        match obj with
        | :? Date as other ->
            this <= other && other <= this
        | _ -> false

    override this.GetHashCode() = this.ToTuple.GetHashCode()

module Date =
    let decoder: Decoder<Date> =
        Decode.object (fun get ->
              {
                  Day = get.Required.Field "day" Decode.int
                  Month = get.Required.Field "month" Decode.int
                  Year = get.Required.Field "year" Decode.int
              })

    let encoder date =
        Encode.object [
            "day", Encode.int date.Day
            "month", Encode.int date.Month
            "year", Encode.int date.Year
        ]

[<CustomComparison; CustomEquality>]
type Time =
    { Hour: int
      Minute: int
    }

    member this.ToTuple = (this.Hour, this.Minute)

    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | :? Time as other ->
                let thisTime = this.ToTuple
                let otherTime = other.ToTuple
                if thisTime > otherTime then 1
                else if thisTime < otherTime then -1
                else 0
            | _ -> 0

    override this.Equals obj =
        match obj with
        | :? Time as other ->
            this <= other && other <= this
        | _ -> false

    override this.GetHashCode() = this.ToTuple.GetHashCode()

module Time =
    let decoder: Decoder<Time> =
        Decode.object (fun get ->
              {
                Hour = get.Required.Field "hour" Decode.int
                Minute = get.Required.Field "minute" Decode.int
              })

    let encoder time =
        Encode.object [
            "hour", Encode.int time.Hour
            "minute", Encode.int time.Minute
        ]


[<CustomComparison; CustomEquality>]
type DateTimeCustom =
    { Date: Date
      Time: Time
    }

    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | :? DateTimeCustom as other ->
                if this.Date > other.Date then 1
                else if this.Date < other.Date then -1
                else if this.Time > other.Time then 1
                else if this.Time < other.Time then -1
                else 0
            | _ -> 0

    override this.Equals obj =
        match obj with
        | :? DateTimeCustom as other ->
            this <= other && other <= this
        | _ -> false

    override this.GetHashCode() =
        this.Date.GetHashCode() + this.Time.GetHashCode()

module DateTimeCustom =
    let decoder: Decoder<DateTimeCustom> =
        Decode.object (fun get ->
              {
                Date = get.Required.Field "date" Date.decoder
                Time = get.Required.Field "time" Time.decoder
              })

    let encoder dateTimeCustom =
        Encode.object [
            "date", Date.encoder dateTimeCustom.Date
            "time", Time.encoder dateTimeCustom.Time
        ]


let customToDateTime (date: Date): DateTime =
    DateTime(date.Year, date.Month, date.Day, 0, 0, 0)

let customToTimeSpan (time: Time): TimeSpan =
    TimeSpan(time.Hour, time.Minute, 0)

let toCustomDateTime (date: DateTime) (time: TimeSpan): DateTimeCustom =
    { Date =
          { Day = date.Day
            Month = date.Month
            Year = date.Year }
      Time =
          { Hour = time.Hours
            Minute = time.Minutes } }

let now(): DateTimeCustom =
    toCustomDateTime DateTime.Now (TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0))

let toUtcString (dt: DateTimeCustom) =
    sprintf "%s%s%sT%s%s%sZ" (dt.Date.Year.ToString())
        (dt.Date.Month.ToString().PadLeft(2, '0'))
        (dt.Date.Day.ToString().PadLeft(2, '0'))
        (dt.Time.Hour.ToString().PadLeft(2, '0'))
        (dt.Time.Minute.ToString().PadLeft(2, '0'))
        "00" // Format: "20200101T192209Z"

let toDateString (dt: DateTimeCustom) =
    sprintf "%s%s%sT%s%s%s" (dt.Date.Year.ToString())
        (dt.Date.Month.ToString().PadLeft(2, '0'))
        (dt.Date.Day.ToString().PadLeft(2, '0'))
        (dt.Time.Hour.ToString().PadLeft(2, '0'))
        (dt.Time.Minute.ToString().PadLeft(2, '0'))
        "00" // Format: "20200101T192209"

let toReadableString (dt: DateTimeCustom) =
    sprintf "%s.%s.%s kl %s:%s" (dt.Date.Day.ToString().PadLeft(2, '0'))
        (dt.Date.Month.ToString().PadLeft(2, '0'))
        (dt.Date.Year.ToString()) (dt.Time.Hour.ToString().PadLeft(2, '0'))
        (dt.Time.Minute.ToString().PadLeft(2, '0'))
