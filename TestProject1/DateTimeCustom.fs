namespace Tests.DateTimeCustom

open System
open Xunit

[<AutoOpen>]
module Helpers =
    let addTimeSpan<'a> (tm: TimeSpan) (o: 'a) : 'a =
        match o :> obj with
        | :? DateTime as x ->
            x.Add(tm) :> obj :?> 'a
        | :? TimeSpan as x ->
            x.Add(tm) :> obj :?> 'a
        | _ -> failwith "Expected DateTime or TimeSpan"
    let addMinutes (m: int) = addTimeSpan (TimeSpan.FromMinutes(m))
    let addHours (h: int) = addTimeSpan (TimeSpan.FromHours(h))
    let addDays (d: int) = addTimeSpan (TimeSpan.FromDays(d))

type UpdateEvent() =
    let always x = fun _ -> x
    let makeTime (h: int) (m: int) = let now = DateTime.Now in DateTime(now.Year, now.Month, now.Day, h, m, 00)
    let mapStart f ((dt, tm): DateTime*TimeSpan) : DateTime*TimeSpan = (f dt, tm)
    let mapDuration f ((dt, tm): DateTime*TimeSpan) : DateTime*TimeSpan = (dt, f tm)
    let alwaysStart dt = mapStart (always dt)
    let alwaysDuration tm = mapDuration (always tm)

    let run (mapOldValue: DateTime*TimeSpan -> DateTime*TimeSpan) (mapNewValue: DateTime*TimeSpan -> DateTime*TimeSpan) =
      let now = DateTime.Now.Date
      let duration = TimeSpan.Zero
      let oldStart, oldDuration = mapOldValue (now, duration)
      let oldEnd = oldStart + oldDuration
      let newStart, newDuration = mapNewValue (oldStart, oldDuration)
      let newEnd = newStart + newDuration
      let actual =
        DateTimeCustom.toReadableDiff
          (DateTimeCustom.toCustomDateTime oldStart oldStart.TimeOfDay)
          (DateTimeCustom.toCustomDateTime oldEnd oldEnd.TimeOfDay)
          (DateTimeCustom.toCustomDateTime newStart newStart.TimeOfDay)
          (DateTimeCustom.toCustomDateTime newEnd newEnd.TimeOfDay)
      actual

    [<Fact>]
    member _.``No changes gives empty result``() =
        let actual = run id id
        Assert.Equal(None, actual)

    [<Fact>]
    member _.``Change start time, zero duration``() =
        let actual = run (alwaysStart (makeTime 15 05)) (mapStart ((addHours 1) >> (addMinutes 1)))
        let expected = Some ("kl 15:05", "kl 16:06")
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Change start time, non-zero duration``() =
        let actual = run ((alwaysStart (makeTime 15 05)) >> (mapDuration (addHours 1))) (mapStart (addHours 1))
        let expected = Some ("kl 15:05-16:05", "kl 16:05-17:05")
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Change duration from zero to non-zero``() =
        let actual = run (alwaysStart (makeTime 15 05)) (mapDuration (addHours 1))
        let expected = Some ("kl 15:05", "kl 15:05-16:05")
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Change duration in days``() =
        let actual = run (always (DateTime(2020, 01, 02, 03, 04, 05), TimeSpan.Zero)) (mapDuration (addDays 1))
        let expected = Some ("02.01.2020", "02.01.2020-03.01.2020")
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Change duration from non-zero to zero``() =
        let actual = run ((alwaysStart (makeTime 10 42)) >> mapDuration (addHours 1)) (alwaysDuration TimeSpan.Zero)
        let expected = Some ("kl 10:42-11:42", "kl 10:42")
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Change date``() =
        let actual = run (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05))) (mapStart (addDays 1))
        let expected = Some ("02.01.2020", "03.01.2020")
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Change date and start``() =
        let actual = run (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05))) (mapStart ((addDays 1) >> (addHours 1)))
        let expected = Some ("02.01.2020 kl 03:04", "03.01.2020 kl 04:04")
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Change date and duration from zero to non-zero``() =
        let actual = run (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05))) ((mapStart (addDays 1)) >> (mapDuration (addHours 1)))
        let expected = Some ("02.01.2020 kl 03:04", "03.01.2020 kl 03:04-04:04")
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Change date and duration from non-zero to zero``() =
        let actual = run (always (DateTime(2020, 01, 02, 03, 04, 05), TimeSpan.FromHours(1))) ((mapStart (addDays 1)) >> (alwaysDuration TimeSpan.Zero))
        let expected = Some ("02.01.2020 kl 03:04-04:04", "03.01.2020 kl 03:04")
        Assert.Equal (actual, expected)

    [<Fact>]
    member _.``Change date, start, and duration from zero to non-zero``() =
        let actual = run (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05))) ((mapStart ((addDays 1) >> (addHours 1))) >> (mapDuration (addHours 1)))
        let expected = Some ("02.01.2020 kl 03:04", "03.01.2020 kl 04:04-05:04")
        Assert.Equal (actual, expected)

    [<Fact>]
    member _.``Change date, start, and duration from non-zero to zero``() =
        let actual = run (always (DateTime(2020, 01, 02, 03, 04, 05), TimeSpan.FromHours(1))) ((mapStart ((addDays 1) >> (addHours 1))) >> (alwaysDuration TimeSpan.Zero))
        let expected = Some ("02.01.2020 kl 03:04-04:04", "03.01.2020 kl 04:04")
        Assert.Equal (actual, expected)

    [<Fact>]
    member _.``Change everything``() =
        let actual = run (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05)) >> (mapDuration ((addDays 1) >> (addHours 1)))) ((mapStart ((addDays 1) >> (addHours 1))) >> (mapDuration ((addDays 1) >> (addHours 1))))
        let expected = Some ("02.01.2020 kl 03:04-03.01.2020 kl 04:04", "03.01.2020 kl 04:04-05.01.2020 kl 06:04")
        Assert.Equal (actual, expected)
