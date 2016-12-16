#Logs2Timeline: Data visualization for event logs

FSX script file that display on a timeline the xml logs exported from windows event viewer.

##Technical Instructions

###Requirements
- F# 

###Run

```
.\paket\paket.bootstrapper.exe
.\paket\paket.exe restore
fsi.exe .\logsToTimeline.fsx --file ..\exportedlogs.xml
```

### Example

![example](/screenshot.png "example")

##Thanks to

[EventDrops](https://github.com/marmelab/EventDrops)