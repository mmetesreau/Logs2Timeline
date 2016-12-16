#r "./packages/Newtonsoft.Json/lib/net40/Newtonsoft.Json.dll"
#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "System.Xml.Linq.dll"

open FSharp.Data
open Newtonsoft.Json

open System
open System.IO
open System.Diagnostics

type OutputConfig = {
    Folder : string
    JsFile : string
    HtmlFile : string
}

type EventData = {
    [<JsonProperty(PropertyName = "date")>]
    Date: DateTime
    [<JsonProperty(PropertyName = "detail")>]
    Detail: string
}

type Event = {
    [<JsonProperty(PropertyName = "name")>]
    Name: string;
    [<JsonProperty(PropertyName = "data")>]
    Data: EventData [] 
}

type Events = {
    [<JsonProperty(PropertyName = "startdate")>]
    Startdate: DateTime
    [<JsonProperty(PropertyName = "stopdate")>]
    Stopdate: DateTime
    [<JsonProperty(PropertyName = "events")>]
    Events: Event []
}

type Logs = XmlProvider<"logs.xml">

let output = { Folder = "timeline"; JsFile = "data.js"; HtmlFile = "timeline.js" }

let sanitizeDetail (detail : string) = 
    detail.Replace("\n", "<br>").Replace("'"," ")

let parseEvents (events :  Logs.Event[]) =
    let getStartDate(allEvents : Logs.Event[]) = 
        let minDate = (allEvents |> Seq.minBy (fun x -> x.System.TimeCreated.SystemTime)).System.TimeCreated.SystemTime
        new DateTime(minDate.Year,minDate.Month,minDate.Day,00,00,01)

    let getStopdate (allEvents : Logs.Event[]) = 
        let maxDate = (allEvents |> Seq.maxBy (fun x -> x.System.TimeCreated.SystemTime)).System.TimeCreated.SystemTime
        new DateTime(maxDate.Year,maxDate.Month,maxDate.Day,23,59,59)

    let getItemData (eventsGroup : Logs.Event[]) = eventsGroup |> Array.map (fun x -> { Date = x.System.TimeCreated.SystemTime; Detail = sanitizeDetail x.EventData.Data})

    let getItems (allEvents : Logs.Event[]) = allEvents
                                                    |> Array.groupBy (fun x -> sprintf "%s - %s" x.System.Provider.Name x.RenderingInfo.Level)
                                                    |> Array.map (fun (key,values) -> { Name = key; Data = getItemData values}) 
    {
        Startdate = getStartDate events
        Stopdate = getStopdate events
        Events = getItems events
    }

let writeInFile path content = 
    File.Create(path).Dispose()
    File.WriteAllText(path, content)

let computeOutputPath file = Path.Combine(__SOURCE_DIRECTORY__, output.Folder, file)

let writeInDataFile = writeInFile <| computeOutputPath  output.JsFile

let serialize items = 
     JsonConvert.SerializeObject(items)

let computeJavascriptConfig (items : Events) = 
    sprintf "var config = %s" <| serialize items

let readFile path = 
    try
        match File.Exists(path) with
        | true -> Some(File.ReadAllText(path))
        | _ -> None
    with
    | _ -> None

let loadLogsFile text =  
    use stringReader =  new StringReader(text)
    let logs = Logs.Load stringReader
    logs.Events

let processFile path = 
    match readFile path with 
    | Some(text) -> loadLogsFile text 
                        |> parseEvents 
                        |> computeJavascriptConfig 
                        |> writeInDataFile 
    | _ -> printfn "Invalid path or file"

let openResult = Process.Start(computeOutputPath  output.HtmlFile) |> ignore

match fsi.CommandLineArgs with
| [| _ ; "--file" ; path|] -> 
                        processFile path
                        openResult
| _ -> printfn "Invalid command line"
