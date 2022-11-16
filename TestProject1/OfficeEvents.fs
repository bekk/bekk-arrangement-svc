namespace Tests.OfficeEvents

open Xunit

open OfficeEvents.Parser

type OfficeEvents() =
    let n    = "\r\n"
    let n'   = $"{n}{n}"
    let n''  = $"{n}{n}{n}"
    let n''' = $"{n}{n}{n}{n}"
    let parseBody body = parse $"<body>{body}</body>"
    let desc desc = { ParseResult.Default with description = desc }
    let types types = { ParseResult.Default with types = types }
    let themes themes = { ParseResult.Default with themes = themes }

    [<Fact>]
    member _.``Default result is not empty``() =
        Assert.NotEqual(ParseResult.Empty, ParseResult.Default)

    [<Fact>]
    member _.``Empty body gives default result``() =
        Assert.Equal(ParseResult.Default, parse "")

    [<Fact>]
    member _.``Body can appear top level``() =
        let actual = parse "<body>title</body>"
        let expected = { ParseResult.Empty with description = "title" }
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Body can appear as inner tag``() =
        let actual = parse "<html><body>title</body></html>"
        let expected = { ParseResult.Empty with description = "title" }
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Simple description``() =
        let actual = parseBody "a b"
        let expected = desc "a b"
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Single line break is replaced with space``() =
        let actual = parseBody $"a{n}b"
        let expected = desc "a b"
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Double line break is interpreted as double linebreak``() =
        let actual = parseBody $"a{n'}b"
        let expected = desc $"a{n'}b"
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Empty lines are skipped``() =
        let actual = parseBody $"a{n'}{n'}b"
        let expected = desc $"a{n'}b"
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Single line HTML comment is skipped``() =
        let actual = parseBody $"a{n'}<!-- whatever -->{n'}b"
        let expected = desc $"a{n'}b"
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Type line html comment is skipped``() =
        let actual = parseBody $"a{n'}type: t{n'}b"
        let expected = { ParseResult.Empty with description = $"a{n'}b"; types = [ "t" ] }
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Theme line html comment is skipped``() =
        let actual = parseBody $"a{n'}tema: t{n'}b"
        let expected = { ParseResult.Empty with description = $"a{n'}b"; themes = [ "t" ] }
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Single type is parsed correctly``() =
        let actual = parseBody "type: type1"
        let expected = types ["type1"]
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Type can contain multiple values``() =
        let actual = parseBody "type: t1, t2, , t3, "
        let expected = types ["t1"; "t2"; "t3"]
        Assert.Equal(expected, actual)
    [<Fact>]
    member _.``Type can be capitalized``() =
        let actual = parseBody "Type: type1"
        let expected = types ["type1"]
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``text on type/theme line is ignored``() =
        let actual = parseBody "this is ignored type: part of type"
        let expected = types ["part of type"]
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``type can appear anywhere``() =
        let actual = parseBody "this is ignored type: part of type"
        let expected = types ["part of type"]
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``multiple types on a single line is interpreted as single type``() =
        let actual = parseBody "type: type1 type: type2"
        let expected = types ["type1 type: type2"]
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``type and theme can appear on the same line``() =
        let actual = parseBody "type: type1 tema: theme1"
        let expected = { ParseResult.Default with types = [ "type1" ]; themes = [ "theme1" ] }
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``type and theme can appear multiple times``() =
        let actual = parseBody $"type: type1{n'}tema: theme1{n'}type: type2{n'}tema: theme2"
        let expected = { ParseResult.Default with types = [ "type1"; "type2" ]; themes = [ "theme1"; "theme2" ] }
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Url without scheme is replaced``() =
        let actual = parseBody "www.nrk.no"
        let expected = { ParseResult.Empty with description = """<a target="_blank" href ="//www.nrk.no">www.nrk.no</a>""" }
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Url with http scheme is replaced``() =
        let actual = parseBody "http://www.nrk.no"
        let expected = { ParseResult.Empty with description = """<a target="_blank" href ="http://www.nrk.no">http://www.nrk.no</a>""" }
        Assert.Equal(expected, actual)

    [<Fact>]
    member _.``Url with https scheme is replaced``() =
        let actual = parseBody "https://www.nrk.no"
        let expected = { ParseResult.Empty with description = """<a target="_blank" href ="https://www.nrk.no">https://www.nrk.no</a>""" }
        Assert.Equal(expected, actual)
