module Tests.DateTimeCustom

open System
open Expecto
open TestUtils

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

let tests =
  testList "DateTimeCustom period diff" [
    let makeTime (h: int) (m: int) = let now = DateTime.Now in DateTime(now.Year, now.Month, now.Day, h, m, 00)
    let mapStart f ((dt, tm): DateTime*TimeSpan) : DateTime*TimeSpan = (f dt, tm)
    let mapDuration f ((dt, tm): DateTime*TimeSpan) : DateTime*TimeSpan = (dt, f tm)
    let alwaysStart dt = mapStart (always dt)
    let alwaysDuration tm = mapDuration (always tm)

    let run (mapOldValue: DateTime*TimeSpan -> DateTime*TimeSpan) (mapNewValue: DateTime*TimeSpan -> DateTime*TimeSpan) =
      let now = DateTime.Now.Date
      let duration = TimeSpan.Zero
      let (oldStart, oldDuration) = mapOldValue (now, duration)
      let oldEnd = oldStart + oldDuration
      let (newStart, newDuration) = mapNewValue (oldStart, oldDuration)
      let newEnd = newStart + newDuration
      let actual =
        DateTimeCustom.toReadableDiff
          (DateTimeCustom.toCustomDateTime oldStart oldStart.TimeOfDay)
          (DateTimeCustom.toCustomDateTime oldEnd oldEnd.TimeOfDay)
          (DateTimeCustom.toCustomDateTime newStart newStart.TimeOfDay)
          (DateTimeCustom.toCustomDateTime newEnd newEnd.TimeOfDay)
      actual

    let expectDiff mapOld mapNew expected =
      let actual = run mapOld mapNew
      Expect.equal actual expected "Unexpected diff"

    test "No changes gives empty result" {
      expectDiff
        id
        id
        None
    }

    test "Change start time, zero duration" {
      expectDiff
        (alwaysStart (makeTime 15 05))
        (mapStart ((addHours 1) >> (addMinutes 1)))
        (Some ("kl 15:05", "kl 16:06"))
    }

    test "Change start time, non-zero duration" {
      expectDiff
        ((alwaysStart (makeTime 15 05)) >> (mapDuration (addHours 1)))
        (mapStart (addHours 1))
        (Some ("kl 15:05-16:05", "kl 16:05-17:05"))
    }

    test "Change duration from zero to non-zero" {
      expectDiff
        (alwaysStart (makeTime 15 05))
        (mapDuration (addHours 1))
        (Some ("kl 15:05", "kl 15:05-16:05"))
    }

    test "Change duration in days" {
      expectDiff
        (always (DateTime(2020, 01, 02, 03, 04, 05), TimeSpan.Zero))
        (mapDuration (addDays 1))
        (Some ("02.01.2020", "02.01.2020-03.01.2020"))
    }

    test "Change duration from non-zero to zero" {
      expectDiff
        ((alwaysStart (makeTime 10 42)) >> mapDuration (addHours 1))
        (alwaysDuration TimeSpan.Zero)
        (Some ("kl 10:42-11:42", "kl 10:42"))
    }

    test "Change date" {
      expectDiff
        (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05)))
        (mapStart (addDays 1))
        (Some ("02.01.2020", "03.01.2020"))
    }

    test "Change date and start" {
      expectDiff
        (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05)))
        ((mapStart ((addDays 1) >> (addHours 1))))
        (Some ("02.01.2020 kl 03:04", "03.01.2020 kl 04:04"))
    }

    test "Change date and duration from zero to non-zero" {
      expectDiff
        (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05)))
        ((mapStart ((addDays 1))) >> (mapDuration (addHours 1)))
        (Some ("02.01.2020 kl 03:04", "03.01.2020 kl 03:04-04:04"))
    }

    test "Change date and duration from non-zero to zero" {
      expectDiff
        (always (DateTime(2020, 01, 02, 03, 04, 05), TimeSpan.FromHours(1)))
        ((mapStart (addDays 1)) >> (alwaysDuration TimeSpan.Zero))
        (Some ("02.01.2020 kl 03:04-04:04", "03.01.2020 kl 03:04"))
    }

    test "Change date, start, and duration from zero to non-zero" {
      expectDiff
        (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05)))
        ((mapStart ((addDays 1) >> (addHours 1))) >> (mapDuration (addHours 1)))
        (Some ("02.01.2020 kl 03:04", "03.01.2020 kl 04:04-05:04"))
    }

    test "Change date, start, and duration from non-zero to zero" {
      expectDiff
        (always (DateTime(2020, 01, 02, 03, 04, 05), TimeSpan.FromHours(1)))
        ((mapStart ((addDays 1) >> (addHours 1))) >> (alwaysDuration TimeSpan.Zero))
        (Some ("02.01.2020 kl 03:04-04:04", "03.01.2020 kl 04:04"))
    }

    test "Change everything" {
      expectDiff
        (alwaysStart (DateTime(2020, 01, 02, 03, 04, 05)) >> (mapDuration ((addDays 1) >> (addHours 1))))
        ((mapStart ((addDays 1) >> (addHours 1))) >> (mapDuration ((addDays 1) >> (addHours 1))))
        (Some ("02.01.2020 kl 03:04-03.01.2020 kl 04:04", "03.01.2020 kl 04:04-05.01.2020 kl 06:04"))
    }
  ]
