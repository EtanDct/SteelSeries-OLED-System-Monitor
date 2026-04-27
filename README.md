# SteelSeries OLED System Monitor

A lightweight C# application that monitors your PC's hardware statistics (CPU load/temp, GPU load, and RAM usage) and displays them in real-time on your SteelSeries keyboard's OLED screen (Apex 7, Apex Pro, etc.) using the GameSense™ API.

## 🌟 Features
- **Real-time Monitoring**: Displays live CPU, GPU (D3D 3D), and RAM stats.
- **Hardware Agnostic**: Powered by [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), it supports almost all modern Intel, AMD, and NVIDIA components.
- **Optimized for OLED**: Text is carefully formatted to fit the 128x40 pixel screens found on SteelSeries keyboards without cutting off data.
- **Auto-Discovery**: Automatically finds your local SteelSeries Engine API address and securely registers itself.

## 📋 Requirements
- Windows 10 / 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SteelSeries GG / Engine running in the background
- A SteelSeries peripheral with an OLED screen (e.g. Apex 7, Apex 7 TKL, Apex Pro)

## 🚀 How to Run
1. Clone this repository to your local machine.
2. Open a terminal in the project directory.
3. Run the application (**Note:** Administrator privileges are required to read CPU temperatures):
   ```bash
   dotnet run
   ```

## ⚙️ Running in the Background
To run the application silently without keeping a console window open, the best method is to use the **Windows Task Scheduler**:
1. Build the project in release mode: `dotnet publish -c Release`
2. Open **Task Scheduler** and create a new Basic Task.
3. Set the trigger to **"At log on"**.
4. Set the action to **"Start a program"** and select the compiled `.exe`.
5. In the task properties, check the boxes for **"Run with highest privileges"** (for temperature readings) and **"Hidden"** (to keep it invisible).

## 🛠️ Built With
- C# / .NET 8.0
- [LibreHardwareMonitorLib](https://www.nuget.org/packages/LibreHardwareMonitorLib/)
- System.Text.Json
