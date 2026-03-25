# TDP Scan Agent - Windows .NET 3.5 Version

A native Windows system tray application that monitors a folder for new files and automatically uploads them to a configured webhook URL.

## Features

- 🔍 **File Monitoring**: Automatically detects new files in the configured scan folder
- 📤 **Webhook Upload**: Uploads detected files to a configurable webhook URL
- ⚙️ **Settings Management**: Easy-to-use settings dialog for configuration
- 🎯 **System Tray App**: Runs quietly in the system tray
- 🔔 **Notifications**: Get notified when files are uploaded successfully or if errors occur
- 🌐 **WebSocket Server**: Remote control via WebSocket protocol
- 🔗 **Custom URL Scheme**: Support for `scan-agent://` protocol (requires registration)
- 🚀 **Scanner Program Auto-Launch**: Automatically launches configured scanner programs on startup

## Requirements

- Windows XP SP3 or later
- .NET Framework 3.5

## Installation

### Option 1: Build from Source

1. Open `ScanAgent.sln` in Visual Studio 2008 or later
2. Build the solution (F6)
3. Run `ScanAgent.exe` from `bin\Debug` or `bin\Release`

### Option 2: Use MSBuild

```cmd
cd scan-agent-windows
build.bat
```

### Option 3: Install Protocol Handler

To enable `scan-agent://` URL support:

```cmd
cd scan-agent-windows
build.bat
install-protocol.bat
```

**Note:** You must run `install-protocol.bat` as Administrator.

## Configuration

Right-click the system tray icon and select "Settings" to configure:

- **Scan Folder**: The folder to monitor for new files (default: `My Documents\Scans`)
- **Webhook URL**: The HTTP endpoint to upload files to
- **WebSocket Port**: Port for WebSocket server (default: 8124)
- **Auto Start**: Start automatically when Windows starts (requires registry setup)
- **Scanner Programs**: List of scanner programs to launch on startup (one per line)

### Default Scanner Program

If no scanner programs are configured, the app will attempt to launch:
```
C:\Program Files (x86)\PFU\ScanSnap\Driver\PfuSsMon.exe
```

## Custom URL Protocol

The app supports the `scan-agent://` custom URL protocol for remote control.

### Protocol Format

```
scan-agent://scan?token=xxx&dealId=yyy&transactionId=100&formId=200&formName=abc&webHookUrl=https://example.com
```

### Parameters

- `token`: Authentication token
- `dealId`: Deal identifier
- `transactionId`: Transaction ID
- `formId`: Form ID
- `formName`: Form name
- `webHookUrl`: Webhook URL to use for this session

### Installation

Run `install-protocol.bat` as Administrator to register the protocol handler.

### Testing

After installation, you can test the protocol by:

1. Opening a browser
2. Entering: `scan-agent://scan?token=test123&webHookUrl=https://example.com`
3. The app should launch and process the URL

### Uninstallation

Run `uninstall-protocol.bat` as Administrator to remove the protocol handler.

## Project Structure

```
scan-agent-windows/
├── ScanAgent.csproj          # Visual Studio project file
├── Program.cs                # Application entry point
├── MainForm.cs               # Main application logic (system tray)
├── MainForm.Designer.cs      # Form designer code
├── FileWatcher.cs            # File system monitoring
├── WebhookUploader.cs        # HTTP upload functionality
├── SettingsManager.cs        # Configuration management
├── SettingsForm.cs           # Settings UI
├── SettingsForm.Designer.cs  # Settings form designer code
├── WebSocketServer.cs        # WebSocket server implementation
├── ProtocolRegistration.cs   # Protocol handler registration
├── Logger.cs                 # Logging functionality
├── app.config                # Application configuration
├── build.bat                 # Build script
├── install-protocol.bat      # Protocol installer (run as admin)
├── uninstall-protocol.bat    # Protocol uninstaller (run as admin)
├── register-protocol.reg     # Registry template
└── Properties/
    └── AssemblyInfo.cs       # Assembly metadata
```

## Architecture

### MainForm.cs
- Main application controller
- Creates system tray icon and menu
- Coordinates file watching and uploads
- Manages notifications

### FileWatcher.cs
- Uses .NET FileSystemWatcher
- Monitors folder for new files
- Stability check (waits for file to be fully written)
- Prevents duplicate processing

### WebhookUploader.cs
- HTTP client for file uploads
- Multipart form data encoding
- Async upload with callback
- MIME type detection

### SettingsManager.cs
- Configuration wrapper using app.config
- Persistent storage for settings
- Default values handling

### WebSocketServer.cs
- WebSocket server implementation for .NET 3.5
- Manual WebSocket handshake and framing
- Supports remote control commands
- Broadcasts file detection and upload events

### Logger.cs
- File-based logging
- In-memory log storage
- Daily log files

## File Upload Flow

```
1. File appears in scan folder
   ↓
2. FileWatcher detects change
   ↓
3. Wait for file stability (fully written)
   ↓
4. Trigger FileDetected event
   ↓
5. MainForm receives file path
   ↓
6. Check if webhook URL is configured
   ↓
7. WebhookUploader.UploadFile()
   ↓
8. Create multipart form data
   ↓
9. Send HTTP POST request
   ↓
10. Handle response
    ↓
11. Show notification (success/failure)
```

## Webhook Request Format

### HTTP Request
```
POST {webhookUrl}
Content-Type: multipart/form-data; boundary=----Boundary{timestamp}

----Boundary{timestamp}
Content-Disposition: form-data; name="file"; filename="document.pdf"
Content-Type: application/pdf

{binary file data}
----Boundary{timestamp}
Content-Disposition: form-data; name="fileName"

document.pdf
----Boundary{timestamp}
Content-Disposition: form-data; name="fileSize"

1024000
----Boundary{timestamp}
Content-Disposition: form-data; name="timestamp"

2026-03-20T10:30:00.000Z
----Boundary{timestamp}--
```

## WebSocket Protocol

### Connection
```
ws://localhost:8124
```

### Messages

**Connected:**
```json
{
  "type": "connected",
  "data": {
    "status": "connected",
    "timestamp": "2026-03-20T10:30:00.000Z"
  }
}
```

**File Detected:**
```json
{
  "type": "file_detected",
  "data": {
    "filePath": "C:\\Users\\...\\document.pdf",
    "fileName": "document.pdf",
    "timestamp": "2026-03-20T10:30:00.000Z"
  }
}
```

**File Uploaded:**
```json
{
  "type": "file_uploaded",
  "data": {
    "filePath": "C:\\Users\\...\\document.pdf",
    "fileName": "document.pdf",
    "s3BucketUrl": "https://s3.amazonaws.com/bucket/file.pdf",
    "timestamp": "2026-03-20T10:30:00.000Z"
  }
}
```

## Comparison with macOS Version

| Aspect | macOS (Swift) | Windows (.NET 3.5) |
|--------|---------------|-------------------|
| Size | ~1-2MB | ~50KB |
| Memory | ~10-20MB | ~15-30MB |
| Platform | macOS only | Windows XP+ |
| UI Framework | Cocoa/AppKit | Windows Forms |
| File Monitoring | FSEvents | FileSystemWatcher |
| WebSocket | NWProtocolWebSocket | Manual implementation |
| Settings Storage | UserDefaults | app.config |

## Troubleshooting

### App doesn't start
- Ensure .NET Framework 3.5 is installed
- Check Windows Event Viewer for errors

### Files not detected
- Verify scan folder exists and is accessible
- Check file permissions
- Review logs in `%APPDATA%\ScanAgent\Logs`

### Upload fails
- Verify webhook URL is correct and accessible
- Check network connectivity
- Review error messages in notifications

### WebSocket connection fails
- Verify port 8124 is not blocked by firewall
- Check if another application is using the port
- Try changing the port in settings

## Development

### Building
```cmd
# Debug build
msbuild ScanAgent.csproj /p:Configuration=Debug

# Release build
msbuild ScanAgent.csproj /p:Configuration=Release
```

### Testing
1. Configure webhook URL in settings
2. Add a test file to scan folder:
   ```cmd
   echo test > "%USERPROFILE%\Documents\Scans\test.txt"
   ```
3. Check for notification
4. Verify webhook received the file

## License

Copyright © 2026

