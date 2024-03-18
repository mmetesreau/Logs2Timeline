# Logs2Timeline: Data visualization for event logs

FSX script file that display on a timeline logs from windows event viewer.

## Technical Instructions

### Requirements
- .NET 8

### Run

```
dotnet fsi .\logs2Timeline.fsx --logname Application
```

### Options

* --file: specify evtx file path
* --logname: specify local event log name

### Example

![example](/screenshot.gif "example")

## Thanks to

[EventDrops](https://github.com/marmelab/EventDrops) a time based / event series interactive visualization using d3.js 
