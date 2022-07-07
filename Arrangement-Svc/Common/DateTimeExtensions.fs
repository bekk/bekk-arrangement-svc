// This exists only because DateTimeCustom has a reeeeeaaally strange design.
// Want to have an extension method on DateTime to convert to DateTimeCustom, but
// the module requires qualified access.
[<AutoOpen>]
module DateTimeCustomExtensions

type System.DateTime with
    member dt.ToDateTimeCustom() = DateTimeCustom.DateTimeCustom.FromDateTime(dt)
