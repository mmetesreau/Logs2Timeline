#r "nuget:System.Diagnostics.EventLog"
#r "nuget:Newtonsoft.Json"
#r "nuget:Fsharp.Data"

open FSharp.Data
open Newtonsoft.Json

open System
open System.IO
open System.Diagnostics
open System.Diagnostics.Eventing.Reader

type Output = {
    Folder : string
    JsFile : string
    HtmlFile : string
}

type EventData = {
    [<JsonProperty(PropertyName = "date")>] Date: DateTime
    [<JsonProperty(PropertyName = "detail")>] Detail: string list
}

type Event = {
    [<JsonProperty(PropertyName = "name")>] Name: string;
    [<JsonProperty(PropertyName = "data")>] Data: EventData list 
}

type Events = {
    [<JsonProperty(PropertyName = "startdate")>] Startdate: DateTime
    [<JsonProperty(PropertyName = "stopdate")>] Stopdate: DateTime
    [<JsonProperty(PropertyName = "events")>] Events: Event list
}

type Entry = {
    TimeCreated: DateTime
    ProviderName: string
    Level: string
    Data: string
}

let output = { Folder = "timeline"; JsFile = "app.js"; HtmlFile = "timeline.html" }

let sanitizeDetail (detail : string) = 
    detail.Replace("\n", "<br>").Replace("'"," ").Trim()

let computeEntries (entries : Entry list) =
    let getStartDate(allEntries : Entry list) = 
        let minDate = (allEntries |> List.minBy (fun x -> x.TimeCreated)).TimeCreated
        new DateTime(minDate.Year,minDate.Month,minDate.Day,00,00,01)

    let getStopdate (allEntries : Entry list) = 
        let maxDate = (allEntries |> List.maxBy (fun x -> x.TimeCreated)).TimeCreated
        new DateTime(maxDate.Year,maxDate.Month,maxDate.Day,23,59,59)

    let getItemData (entriesGroup : Entry list) = entriesGroup 
                                                    |> List.groupBy (fun x -> x.TimeCreated)
                                                    |> List.map (fun (key,values) -> { Date = key; Detail = values |> List.map (fun x -> sanitizeDetail x.Data) })

    let getItems (allEntries : Entry list) = allEntries
                                                    |> List.groupBy (fun x -> sprintf "%s - %s" x.ProviderName x.Level)
                                                    |> List.map (fun (key,values) -> { Name = key; Data = getItemData values}) 
    {
        Startdate = getStartDate entries
        Stopdate = getStopdate entries
        Events = getItems entries
    }

let serialize items = JsonConvert.SerializeObject(items)

let computeJavascriptFile (events : Events) = sprintf "run(%s)" <| serialize events

let getOutputPath file = Path.Combine(__SOURCE_DIRECTORY__, output.Folder, file)

let writeFile path content = 
    use file = File.CreateText(path)
    fprintfn file "%s" content

let writeJavascriptFile = writeFile <| getOutputPath output.JsFile

let openHtmlFile() = 
    let info = ProcessStartInfo(FileName = getOutputPath  output.HtmlFile, UseShellExecute = true)
    Process.Start(info) |> ignore

let processEntries entries =
    entries
        |> computeEntries 
        |> computeJavascriptFile 
        |> writeJavascriptFile

    openHtmlFile()

let loadEventLogEntriesFromFile path =  
    let readEventLogEntriesFromFile() = 
        use reader =  new EventLogReader(path,PathType.FilePath)

        let normalyze (entry : EventRecord) = { 
                        TimeCreated = entry.TimeCreated.Value 
                        Level = entry.LevelDisplayName
                        ProviderName = entry.ProviderName
                        Data = entry.FormatDescription()
                    } 

        let rec read entries = 
            match reader.ReadEvent() with
            | null -> entries
            | entry -> read (entry::entries)

        read [] |> List.map normalyze

    try
        match File.Exists(path) with
        | true -> Some(readEventLogEntriesFromFile())
        | _ -> None
    with
    | _ -> None

let loadLocalEventLogEntries logName = 
    let readLocalEventLogEntries() = 
        use eventLog = new EventLog(logName)
        
        let normalyze (entry : EventLogEntry) = { 
                TimeCreated = entry.TimeGenerated
                ProviderName = entry.Source
                Level = entry.EntryType.ToString()
                Data = entry.Message 
            }

        [for entry in eventLog.Entries -> normalyze entry]

    try
        match EventLog.Exists logName with
        | true -> Some(readLocalEventLogEntries())
        | _ -> None
    with
    | _ -> None

let processEventLogFile path = 
    match loadEventLogEntriesFromFile path with 
    | Some(entries) -> processEntries entries
    | _ -> printfn "Invalid path or file"

let processLocalEventLog logName =
    match loadLocalEventLogEntries logName with
    | Some(entries)  -> processEntries entries
    | _ -> printfn "Invalid log name or no entries"

match fsi.CommandLineArgs with
| [| _ ; "--file" ; path|] -> 
                            processEventLogFile path
| [| _ ; "--logname" ; logName|] ->  
                            processLocalEventLog logName
| _ -> printfn "Invalid command line"
