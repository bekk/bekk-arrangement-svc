module Tests.OfficeEvents

open OfficeEvents.Parser

open Expecto


let n    = $"\r\n"
let n'   = $"{n}{n}"
let n''  = $"{n}{n}{n}"
let n''' = $"{n}{n}{n}{n}"

let tests =
  let parseBody body = parse $"<body>{body}</body>"
  let desc desc = { ParseResult.Default with description = desc }
  let types types = { ParseResult.Default with types = types }
  let themes themes = { ParseResult.Default with themes = themes }

  let expectParsed body expected =
    Expect.equal
      (parseBody body)
      expected
      body

  testList "EventBodyParser" [
    testList "default instances" [
      testCase "Default result isn't empty" <| fun () ->
        Expect.notEqual
          ParseResult.Empty
          ParseResult.Default
          "Default parse result shouldn't be empty"

      testCase "empty body gives default result" <| fun () ->
        Expect.equal
          (parse "")
          ParseResult.Default
          "Empty body not yielding default parse result"
    ]

    testList "body tag" [
      testCase "body can appear top-level" <| fun () ->
        Expect.equal
          (parse "<body>title</body>")
          { ParseResult.Empty with description = "title" }
          "Top-level <body> doesn't work"

      testCase "body can appear as inner tag" <| fun () ->
        Expect.equal
          (parse "<html><body>title</body></html>")
          { ParseResult.Empty with description = "title" }
          "<body> in <html> doesn't work"
    ]

    testList "Description parsing" [
      testCase "simple description" <| fun () ->
        expectParsed
          "a b"
          (desc "a b")

      testCase "single linebreak is replaced with space" <| fun () ->
        expectParsed
          $"a{n}b"
          (desc "a b")

      testCase "double linebreaks is interpreted as double linebreak" <| fun () ->
        expectParsed
          $"a{n'}b"
          (desc $"a{n'}b")

      testCase "empty lines is skipped" <| fun () ->
        expectParsed
          $"a{n'}{n'}b"
          (desc $"a{n'}b")

      testCase "Single line html comment is skipped" <| fun () ->
        expectParsed
          $"a{n'}<!-- whatever -->{n'}b"
          (desc $"a{n'}b")

      testCase "Type line html comment is skipped" <| fun () ->
        expectParsed
          $"a{n'}type: t{n'}b"
          { ParseResult.Empty with description = $"a{n'}b"; types = [ "t" ] }

      testCase "Theme line html comment is skipped" <| fun () ->
        expectParsed
          $"a{n'}tema: t{n'}b"
          { ParseResult.Empty with description = $"a{n'}b"; themes = [ "t" ] }
    ]

    testList "type/theme parsing" [
      testCase "single type is parsed correctly" <| fun () ->
        expectParsed
          "type: type1"
          (types ["type1"])

      testCase "type can contain multiple values" <| fun () ->
        expectParsed
          "type: t1, t2, , t3, "
          (types ["t1"; "t2"; "t3"])

      testCase "type can be capitalized" <| fun () ->
        expectParsed
          "Type: type1"
          (types ["type1"])

      testCase "text on type/theme line is ignored" <| fun () ->
        expectParsed
          "this is ignored type: part of type"
          { ParseResult.Default with types = [ "part of type" ] }

      testCase "type can appear anywhere" <| fun () ->
        expectParsed
          "whatever type: part of type"
          { ParseResult.Default with types = [ "part of type" ] }

      testCase "multiple types on a single line is interpreted as single type" <| fun () ->
        expectParsed
          "type: type1 type: type2"
          { ParseResult.Default with types = [ "type1 type: type2" ] }

      testCase "type and theme can appear on the same line" <| fun () ->
        expectParsed
          "type: type1 tema: theme1"
          { ParseResult.Default with types = [ "type1" ]; themes = [ "theme1" ] }

      testCase "Type and theme can appear multiple times" <| fun () ->
        expectParsed
          $"type: type1{n'}tema: theme1{n'}type: type2{n'}tema: theme2"
          { ParseResult.Default with types = [ "type1"; "type2" ]; themes = [ "theme1"; "theme2" ] }
    ]

    testList "Url to link replacement" [
      testCase "Url without scheme is replaced" <| fun () ->
        expectParsed
          $"www.nrk.no"
          { ParseResult.Empty with description = """<a target="_blank" href ="//www.nrk.no">www.nrk.no</a>""" }

      testCase "Url with http scheme is replaced" <| fun () ->
        expectParsed
          $"http://www.nrk.no"
          { ParseResult.Empty with description = """<a target="_blank" href ="http://www.nrk.no">http://www.nrk.no</a>""" }

      testCase "Url with https scheme is replaced" <| fun () ->
        expectParsed
          $"https://www.nrk.no"
          { ParseResult.Empty with description = """<a target="_blank" href ="https://www.nrk.no">https://www.nrk.no</a>""" }
    ]
  ]
