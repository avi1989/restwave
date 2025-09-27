# RestWave 🌊

A modern, cross-platform REST API client built with Avalonia UI and .NET 9. RestWave provides an intuitive interface for testing and managing REST API requests with collection organization and advanced features.

![RestWave Logo](RestWave/Assets/logo.ico)

## Features

- **Native Cross-Platform Performance**: Built with .NET and Avalonia UI - no Electron, no browser overhead, just fast native performance on Windows, macOS, and Linux
- **Fully Offline with Local Storage**: Works completely offline with collections stored as files on your local file system - own your data, sync with Git, backup however you want
- **Server-Sent Events (SSE) Support**: Real-time streaming response handling with automatic parsing, grouping, and live updates
- **Hierarchical Organization**: Organize requests in nested folders that map directly to your file system structure
- **Developer-Friendly**: Keyboard shortcuts, request cloning, syntax highlighting, and JSON formatting for efficient workflow
- **Privacy-First**: No telemetry, no cloud dependencies, no data collection - your API tests stay on your machine

## Screenshots

*Coming Soon*

## Requirements

- .NET 9.0 or later
- Windows, macOS, or Linux

## Installation

### From Source

1. Clone the repository:
```bash
git clone https://github.com/avi1989/restwave.git
cd restwave
```

2. Build the application:
```bash
dotnet build RestWave.sln
```

3. Run the application:
```bash
dotnet run --project RestWave
```

### Binary Releases

*Binary releases will be available soon*

## Usage

### Making Your First Request

1. Launch RestWave
2. Enter your API endpoint URL
3. Select the HTTP method (GET, POST, PUT, DELETE)
4. Add headers if needed
5. For POST/PUT requests, add your JSON body in the editor
6. Click the "Invoke" button or press F5 to send the request
7. View the response in the response panel

### Organizing Requests with Collections

1. Create a new collection from the File menu
2. Add requests to collections for better organization
3. Use nested folders to create hierarchical structures
4. Collections are automatically saved and persist between sessions

### Keyboard Shortcuts

- `Ctrl+S`: Save current request
- `F5`: Send/Submit current request
- `Ctrl+N`: Create new request

### Settings

Access settings to:
- Change application theme (Light/Dark/System)
- Configure requests directory path
- Manage other preferences

## Architecture

RestWave is built using:

- **Framework**: .NET 9.0
- **UI Framework**: Avalonia UI 11.3.2
- **Architecture Pattern**: MVVM with CommunityToolkit.Mvvm
- **Text Editor**: AvaloniaEdit with TextMate support for syntax highlighting
- **Styling**: Fluent Design with Inter font family

### Project Structure

```
RestWave/
├── Models/           # Data models and configuration
├── ViewModels/       # MVVM view models
├── Views/            # UI views and controls
├── Services/         # Business logic and data services
├── Assets/           # Images, icons, and resources
└── Styles/           # Application styling and themes
```

## Contributing

We welcome contributions! Here's how to get started:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and test them
4. Commit your changes: `git commit -m 'Add amazing feature'`
5. Push to the branch: `git push origin feature/amazing-feature`
6. Open a Pull Request

### Development Setup

1. Ensure you have .NET 9.0 SDK installed
2. Clone the repository
3. Open `RestWave.sln` in your favorite IDE (Visual Studio, Rider, VS Code)
4. Build and run the project

### Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Ensure proper error handling

## Recent Updates

- ✨ **Collection Management**: Refactored collection list for better organization
- 🔧 **Configuration**: Fixed ConfigManager loading issues
- 📋 **Request Cloning**: Added option to clone existing requests
- 🎨 **JSON Formatting**: Implemented format button for JSON responses
- 📱 **Menu Integration**: Added new request/collection options in menubar
- ⚙️ **Settings Panel**: Implemented comprehensive settings management
- 🎯 **Nested Collections**: Support for hierarchical request organization
- ⌨️ **Hotkeys**: Save and submit request keyboard shortcuts

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- [ ] Import/Export collections (Postman, Insomnia formats)
- [ ] Environment variables support
- [ ] Tabbed Interface
- [ ] GraphQL support
- [ ] WebSocket testing

## Support

If you encounter any issues or have questions:

1. Check the [Issues](https://github.com/avi1989/restwave/issues) page
2. Create a new issue if your problem isn't already reported
3. Provide detailed information about your environment and the issue

## Acknowledgments

- Built with [Avalonia UI](https://avaloniaui.net/) - Cross-platform .NET UI framework
- Powered by [.NET 9](https://dotnet.microsoft.com/) - Modern development platform
- Icons and design inspired by modern REST client applications

---
