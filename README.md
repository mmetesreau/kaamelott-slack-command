# Kaamelott slack command

Slack command to send kaamelott soundtracks.

![example](/screenshot.gif "example")

## Technical Instructions

### Requirements
- F# 

### Run

```
$env:ACCESSTOKEN="yourslackaccesstoken"
$env:VERIFICATIONTOKEN="yourslackverificationtoken"
.\paket\paket.bootstrapper.exe
.\paket\paket.exe restore
fsi.exe .\src\app.fsx
```