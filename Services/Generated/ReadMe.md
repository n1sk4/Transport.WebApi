# Generate GRFS Realtime service

## Setup Protocol buffer tool
Download the tool from: https://github.com/protocolbuffers/protobuf/releases

### In Windows set to PATH
example path: *C:\Program Files (x86)\protoc-31.0-win64\bin*

Current user:
```bash
[Environment]::SetEnvironmentVariable("Path", [Environment]::GetEnvironmentVariable("Path", "User") + ";C:\Program Files (x86)\protoc-31.0-win64\bin", "User")
```
System (Requires Admin):
```bash
$systemPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
$newPath = "C:\Program Files (x86)\protoc-31.0-win64\bin"
[Environment]::SetEnvironmentVariable("Path", $systemPath + ";" + $newPath, "Machine")
```

## Generate the service
```bash
protoc --csharp_out=./Services/Generated Vendor/transit/gtfs-realtime/proto/gtfs-realtime.proto
```

[Home ReadMe](../../ReadMe.md)