[![Release build](https://github.com/n1sk4/Transport.WebApi/actions/workflows/release_build.yml/badge.svg)](https://github.com/n1sk4/Transport.WebApi/actions/workflows/release_build.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)


# 🚋 Transport.WebApi

## Description
Web API for getting the GTFS (General Transit Feed Specification) data

### Core Functionality
- **Real-time Vehicle Positions** - Track buses, trams, and other vehicles in real-time
- **Static GTFS Data** - Access routes, stops, schedules, and route shapes

## Example
### Simple UI
[More info in WebClients folder](WebClients/simple-client/ReadMe.md)

![🚋](https://raw.githubusercontent.com/n1sk4/Transport.WebApi/refs/heads/master/WebClients/simple-client/image.png)

## Quick Start

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Protocol Buffers Compiler (protoc)](https://github.com/protocolbuffers/protobuf/releases)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/n1sk4/Transport.WebApi.git
   cd Transport.WebApi
   ```

2. **Initialize submodules**
   ```bash
   git submodule update --init --recursive
   ```

3. **Install Protocol Buffers compiler**
   
   **Windows:**
   ```powershell
   # Download from https://github.com/protocolbuffers/protobuf/releases
   # Add to PATH (example path):
   [Environment]::SetEnvironmentVariable("Path", [Environment]::GetEnvironmentVariable("Path", "User") + ";C:\Program Files (x86)\protoc-31.0-win64\bin", "User")
   ```
   
   **macOS:**
   ```bash
   brew install protobuf
   ```
   
   **Linux:**
   ```bash
   sudo apt-get install protobuf-compiler
   ```

4. **Generate GTFS Realtime classes**
   ```bash
   protoc --csharp_out=Services/Generated Vendor/transit/gtfs-realtime/proto/gtfs-realtime.proto
   ```
   OR if using Visual studio 2022:
   Build with configuration ReleaseWProtoGen | DebugWProtoGen

5. **Restore dependencies and build**
   ```bash
   dotnet restore
   dotnet build --configuration Release
   ```

6. **Run the application**
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:7191` with Swagger documentation at `/swagger`.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments
- [Google Transit](https://github.com/google/transit) - GTFS specification and Protocol Buffers
- [ZET Zagreb](https://www.zet.hr/) - Public transport data provider
- [Leaflet](https://leafletjs.com/) - Interactive map library
- [OpenStreetMap](https://www.openstreetmap.org/) - Map tiles and data