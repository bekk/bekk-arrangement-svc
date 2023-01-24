module OfficeEvents

open System
open UserMessage
open FsToolkit.ErrorHandling

module String =
    let startsWith (prefix : string) (str : string) =
        str.StartsWith(prefix)

module Seq =
    let defaultValue (whenEmpty : 'a seq) (xs : 'a seq) : 'a seq =
        if Seq.isEmpty xs
        then whenEmpty
        else xs


module Parser =
    open System.Text.RegularExpressions
    open System.Net
    open HtmlAgilityPack

    type ParseResult = {
        description: string
        types: string list
        themes: string list
    } with
        static member Empty = {
            description = ""
            types = []
            themes = []
        }
        static member Default = {
            description = "Ingen beskrivelse"
            types = []
            themes = []
        }

        static member Append (a : ParseResult) (b : ParseResult) : ParseResult =
            { ParseResult.Empty with
                description =
                    if String.IsNullOrWhiteSpace(a.description) then b.description
                    elif String.IsNullOrWhiteSpace(b.description) then a.description
                    else String.Join("\r\n\r\n", [a.description; b.description])
                types = List.append a.types b.types
                themes = List.append a.themes b.themes
            }

    let private extractBody (html: string) : string =
        begin
            let document = HtmlDocument()
            document.LoadHtml(WebUtility.HtmlDecode(html))
            document.DocumentNode.Descendants()
        end
        |> Seq.tryFind (fun x -> x.Name = "body")
        |> Option.bind (fun x -> x.InnerText |> Option.ofObj)
        |> Option.defaultValue html

    let private convertUrlsToLinks (body: string) : string =
        // regEx expression stolen from http://blog.mattheworiordan.com/post/13174566389/url-regular-expression-for-links-with-or-without and modified slightly afterwards
        Regex(@"(?<url>((([A-Za-z]{3,9}:(?:\/\/)?)[A-Za-z0-9.-]+|(?:www\.)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\/\w]*))?))", RegexOptions.IgnoreCase)
        |> fun re -> re.Matches(body)
        |> Seq.map (fun m ->
            let url = m.Groups["url"].Value
            let link =
                if url.StartsWith("http://") || url.StartsWith("https://")
                then $"""<a target="_blank" href ="{url}">{url}</a>"""
                else $"""<a target="_blank" href ="//{url}">{url}</a>"""
            (url, link))
        |> Seq.fold (fun body -> body.Replace) body

    let private bodyLines (body : string) : string seq =
        let rgx = Regex(@"\s*[\r\n]\s*")
        body.Split([| "\r\n\r\n" |], StringSplitOptions.None)
        |> Seq.map (fun l -> rgx.Replace(l, " ").Trim())
        |> Seq.filter (not << String.IsNullOrWhiteSpace)
        // TODO: Is the following necessary? Seems like the HtmlDocument library removes comments. Is this a leftover from before that was introduced..?
        |> Seq.filter (not << String.startsWith "<!--") //hack: filter bodyLines to remove unwanted lines added to the html body when changing an event in outlook
        |> Seq.map convertUrlsToLinks

    let private themesTypesRegex = Regex(@"[T|t]ema:\s*(.+)\s*[T|t]ype:\s*(.+)\s*");
    let private typesThemesRegex = Regex(@"[T|t]ype:\s*(.+)\s*[T|t]ema:\s*(.+)\s*");
    let private typesRegex = Regex(@"[T|t]ype:\s*(.+)\s*");
    let private themesRegex = Regex(@"[T|t]ema:\s*(.+)\s*")

    let private reMatch (re: Regex) (txt: string) : Match option =
        let m = re.Match(txt)
        if m.Success then Some m else None

    let private splitString (value : string) =
        value.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries)
        |> Seq.map (fun x -> x.Trim())
        |> Seq.filter (not << String.IsNullOrWhiteSpace)
        |> List.ofSeq

    let private parseLine (line: string) : ParseResult =
        seq {
            reMatch typesThemesRegex line
            |> Option.map (fun m ->
                { ParseResult.Empty with
                    types = splitString m.Groups[1].Value
                    themes = splitString m.Groups[2].Value })

            reMatch themesTypesRegex line
            |> Option.map (fun m ->
                { ParseResult.Empty with
                    types = splitString m.Groups[2].Value
                    themes = splitString m.Groups[1].Value })

            reMatch typesRegex line
            |> Option.map (fun m ->
                { ParseResult.Empty with
                    types = splitString m.Groups[1].Value })

            reMatch themesRegex line
            |> Option.map (fun m ->
                { ParseResult.Empty with
                    themes = splitString m.Groups[1].Value })

            Some { ParseResult.Empty with description = line }
        }
        |> Seq.pick id

    let parse (html: string) : ParseResult =
        html
        |> extractBody
        |> bodyLines
        |> Seq.map parseLine
        |> Seq.fold ParseResult.Append ParseResult.Empty
        |> fun res ->
            if res.description = ParseResult.Empty.description
            then { res with description = ParseResult.Default.description }
            else res

module CalendarLookup =
    open System.Threading

    open FSharp.Control

    open Azure.Identity
    open Microsoft.Graph

    type Options = {
        TenantId: string
        Mailbox: string
        ClientId: string
        ClientSecret: string
    }

    let getEvents (options : Options) (cancellationToken : CancellationToken) (start : DateTime) (end' : DateTime) : Event AsyncSeq =
        let credential = ClientSecretCredential(
            options.TenantId,
            options.ClientId,
            options.ClientSecret
        )

        GraphServiceClient(credential, [| "https://graph.microsoft.com/.default" |])
            .Users[options.Mailbox].CalendarView
            // Note that the API requires UTC, and no formatting options does this
            // conversion before printing, so we have to force it in case we're storing
            // local time.
            .Request([| QueryOption("startDateTime", $"{start.ToUniversalTime():o}")
                        QueryOption("endDateTime", $"{end'.ToUniversalTime():o}") |])
        |> AsyncSeq.unfoldAsync (fun req ->
            if isNull req
            then async { return None }
            else async {
                    let! page = req.GetAsync(cancellationToken) |> Async.AwaitTask
                    return Some (page :> Event seq, page.NextPageRequest)
                 })
       |> AsyncSeq.concatSeq
       |> AsyncSeq.filter (fun ev ->
           let cancelled = ev.IsCancelled |> Option.ofNullable |> Option.defaultValue false
           not cancelled)

module WebApi =
    open Microsoft.AspNetCore.Http
    open Giraffe
    
    // Used to add timezone marker to the date-string as microsoft chose not to follow the standard here.
    // If its not in UTC then something is terribly wrong, so then we just return the string as we get it
    let generateTimezoneString dateString timeZone =
        let timezone = if timeZone = "UTC" then "Z" else ""
        $"{dateString}{timezone}"

    let get (date: string) (next: HttpFunc) (context: HttpContext) =
        let result =
            taskResult {
                let! date =
                    DateTime.TryParse(date)
                    |> function
                        | false, _ -> Error $"{date} er ikke en gyldig datetime."
                        | true, date -> Ok date
                    |> Result.mapError BadRequest

                let start, end' =
                    let startOfMonth = DateTime(date.Year, date.Month, 1)
                    let endOfMonth = startOfMonth.AddMonths(1)
                    (startOfMonth.AddDays(-7), endOfMonth.AddDays(7))
                return!
                    CalendarLookup.getEvents
                        (context.GetService<CalendarLookup.Options>())
                        context.RequestAborted
                        start
                        end'
                    |> FSharp.Control.AsyncSeq.map (fun e ->
                        let body = Parser.parse e.Body.Content
                        {| Id = e.Id
                           Title =  if String.IsNullOrWhiteSpace(e.Subject) then "Tittel ikke satt" else e.Subject
                           Description = body.description
                           Types = body.types
                           Themes = body.themes
                           StartTime = generateTimezoneString e.Start.DateTime e.Start.TimeZone
                           EndTime = generateTimezoneString e.End.DateTime e.End.TimeZone
                           ContactPerson = e.Organizer.EmailAddress.Name;
                           ModifiedAt = e.LastModifiedDateTime.Value.UtcDateTime;
                           CreatedAt = e.CreatedDateTime.Value.UtcDateTime;
                           Location = if String.IsNullOrWhiteSpace(e.Location.DisplayName) then "Sted ikke satt" else e.Location.DisplayName
                        |})
                    // The json serializer doesn't work with F# or dotnet AsyncEnumerable
                    |> FSharp.Control.AsyncSeq.toListAsync
            }
        jsonResult result next context