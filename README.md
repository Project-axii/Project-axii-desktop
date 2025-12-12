# Axii Desktop - Remote PC Control System

<div align="center">
  <img src="img/white-logo.ico" alt="Axii Logo" width="120"/>
  
  **A powerful and modern remote PC control system built with .NET 9 and SignalR**
  
  [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
  [![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
  [![SignalR](https://img.shields.io/badge/SignalR-Real--time-green.svg)](https://dotnet.microsoft.com/apps/aspnet/signalr)
</div>

---

## 📋 Overview

Axii Desktop is a client-server application that enables remote control and command execution on multiple Windows PCs simultaneously through an intuitive web interface. It uses real-time communication via SignalR to execute commands remotely, making it perfect for labs, classrooms, or managing multiple computers.

### ✨ Key Features

- 🌐 **Web-based Control Panel** - Modern React interface with beautiful gradients and animations
- 🚀 **Real-time Communication** - Instant command execution using SignalR WebSockets
- 💻 **Multi-PC Support** - Control multiple computers simultaneously
- 🔄 **Auto-reconnect** - Automatic reconnection with exponential backoff
- 📊 **Live Status Monitoring** - Track connected clients and command execution in real-time
- 🎨 **Beautiful UI** - Modern design with Tailwind CSS and smooth animations
- ♿ **Accessibility** - VLibras integration for Brazilian Sign Language

### 🎯 Supported Actions

- **Notepad** - Open Windows Notepad
- **Date/Time** - Display system date and time information
- **File Creation** - Create a timestamped text file
- **VS Code** - Launch Visual Studio Code
- **Visual Studio** - Launch Visual Studio IDE
- **Laragon** - Start Laragon development environment
- **Packet Tracer** - Launch Cisco Packet Tracer
- **Ngrok** - Start ngrok tunnel on port 5000

---

## 🏗️ Architecture

The system consists of two main components:

### 1. AxiiServer (ASP.NET Core Web Application)
- Hosts the web-based control panel
- Manages SignalR hub for real-time communication
- Tracks connected clients
- Broadcasts commands to all connected clients
- Runs on port 5000

### 2. AxiiClient (Console Application)
- Connects to the AxiiServer via SignalR
- Listens for commands from the server
- Executes commands locally using batch scripts
- Reports execution status back to the server
- Auto-reconnects on connection loss

```
┌─────────────────┐
│  Web Browser    │
│  (Control UI)   │
└────────┬────────┘
         │ HTTP
         ▼
┌─────────────────┐      SignalR      ┌─────────────────┐
│  AxiiServer     │◄─────────────────►│  AxiiClient     │
│  (Hub/Web API)  │                    │  (PC 1)         │
└─────────────────┘                    └─────────────────┘
         │                              ┌─────────────────┐
         └─────────────────────────────►│  AxiiClient     │
                  SignalR                │  (PC 2)         │
                                        └─────────────────┘
                                        ┌─────────────────┐
                                        │  AxiiClient     │
                                        │  (PC N)         │
                                        └─────────────────┘
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Windows OS (for client execution)
- Visual Studio 2022 or VS Code (optional, for development)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Project-axii/Project-axii-desktop.git
   cd Project-axii-desktop
   ```

2. **Build the solution**
   ```bash
   dotnet build Project-axii-desktop.sln
   ```

### Running the System

#### Step 1: Start the Server

Navigate to the server directory and run:

```bash
cd AxiiServer
dotnet run
```

The server will start on `http://0.0.0.0:5000`. You should see output similar to:

```
AXII DESKTOP
Acesse: http://192.168.1.100:5000
Interface: http://localhost:5000
```

Open your browser and navigate to `http://localhost:5000` to access the control panel.

#### Step 2: Start Clients on Target PCs

On each PC you want to control, navigate to the client directory and run:

```bash
cd AxiiClient
dotnet run
```

The client will prompt you for the server configuration URL. You can:
- Press Enter to use the default configuration from GitHub
- Enter a custom JSON configuration URL

Once connected, you'll see:
```
CONECTADO COM SUCESSO!
Status: Aguardando comandos...
```

---

## 📖 Usage

### Web Control Panel

1. Open the web interface at `http://localhost:5000`
2. Check the status bar to see connected clients
3. Click on any application card to execute it on all connected PCs
4. Monitor execution logs in the log panel
5. Use "Clear Logs" to clean the log view
6. Use "Update Status" to refresh connection information

### Configuration via JSON

The client can fetch server configuration from a JSON file (default: GitHub repository). The JSON should have this format:

```json
{
  "status": "success",
  "link": "http://your-server-url:5000"
}
```

### Using with ngrok

To expose the server over the internet:

1. Run the server: `dotnet run` (in AxiiServer)
2. In another terminal: `ngrok http 5000`
3. Update your configuration JSON with the ngrok URL
4. Clients can now connect from anywhere

---

## 🛠️ Development

### Project Structure

```
Project-axii-desktop/
├── AxiiServer/              # Server application
│   ├── Program.cs           # Server entry point and SignalR hub
│   ├── index.html           # Web control panel UI
│   ├── appsettings.json     # Server configuration
│   └── AxiiServer.csproj    # Server project file
├── AxiiClient/              # Client application
│   ├── Program.cs           # Client entry point
│   └── AxiiClient.csproj    # Client project file
├── img/                     # Images and logos
├── Project-axii-desktop.sln # Visual Studio solution
├── LICENSE                  # MIT License
└── README.md               # This file
```

### Technologies Used

- **.NET 9.0** - Core framework
- **ASP.NET Core** - Web server framework
- **SignalR** - Real-time communication
- **React 18** - UI framework
- **Tailwind CSS** - Styling framework
- **VLibras** - Brazilian Sign Language accessibility

### Building from Source

```bash
# Build entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Run tests (if available)
dotnet test
```

---

## 🔧 Configuration

### Server Configuration

Edit `AxiiServer/appsettings.json` to customize server settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Client Configuration

The client automatically connects to the server URL specified in the configuration JSON. No additional configuration is required.

---

## 🐛 Troubleshooting

### Server won't start
- **Problem**: Port 5000 already in use
- **Solution**: Stop other applications using port 5000 or change the port in `Program.cs`

### Client can't connect
- **Problem**: Connection refused or timeout
- **Solution**: 
  - Verify the server is running
  - Check firewall settings allow connections on port 5000
  - Ensure the configuration JSON URL is correct and accessible
  - Verify network connectivity between client and server

### Commands not executing
- **Problem**: No clients connected
- **Solution**: Start at least one client and verify it connects successfully

### Application not launching
- **Problem**: Application not found on client PC
- **Solution**: Install the required application (VS Code, Visual Studio, etc.) or customize the batch script in `AxiiClient/Program.cs`

---

## 🔒 Security Considerations

- This system is designed for trusted networks (labs, classrooms)
- No authentication is implemented by default
- Commands are executed with the privileges of the user running the client
- Be cautious when exposing the server to the internet
- Consider implementing authentication and encryption for production use

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2025 AXII

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction...
```

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📧 Support

For support, questions, or feedback:
- Open an issue on [GitHub Issues](https://github.com/Project-axii/Project-axii-desktop/issues)
- Check existing issues for solutions
- Review the troubleshooting section above

---

## 🎓 Educational Purpose

This project was created for educational purposes to demonstrate:
- Real-time client-server communication with SignalR
- Cross-platform .NET development
- Modern web UI development with React
- Remote system administration concepts

---

<div align="center">
  <p>Made with ❤️ by the AXII Team</p>
  <p>© 2025 AXII. All rights reserved.</p>
</div>