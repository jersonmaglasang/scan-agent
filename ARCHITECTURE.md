# ScanAgent Windows Architecture

## Overview

ScanAgent for Windows is a .NET 3.5 Windows Forms application that provides file monitoring and webhook upload functionality. It runs as a system tray application and is the Windows equivalent of the macOS Swift version.

## Component Diagram

```
┌─────────────────────────────────────────┐
│      System Tray Application            │
│         (MainForm.cs)                   │
└─────────────┬───────────────────────────┘
              │
              ├─── FileWatcher.cs
              │    └─── Monitors scan folder
              │         └─── Detects new files
              │
              ├─── WebhookUploader.cs
              │    └─── Uploads files via HTTP POST
              │         └─── Multipart form data
              │
              ├─── SettingsManager.cs
              │    └─── app.config storage
              │         ├─── Scan folder path
              │         ├─── Webhook URL
              │         ├─── WebSocket port
              │         └─── Scanner programs
              │
              ├─── WebSocketServer.cs
              │    └─── WebSocket protocol
              │         ├─── Manual handshake
              │         ├─── Frame encoding/decoding
              │         └─── Remote control
              │
              ├─── Logger.cs
              │    └─── File-based logging
              │         └─── Daily log files
              │
              └─── SettingsForm.cs
                   └─── Windows Forms UI
                        └─── Configuration interface
```

## Key Components

### 1. MainForm.cs
- Main application controller
- Creates system tray icon (NotifyIcon)
- Manages file watcher lifecycle
- Handles file upload coordination
- Shows balloon tip notifications
- Launches scanner programs on startup

### 2. FileWatcher.cs
- Uses .NET FileSystemWatcher
- Monitors folder for new files
- Waits for files to be fully written (stability check)
- Tracks processed files to avoid duplicates
- Uses HashSet for efficient lookups

### 3. WebhookUploader.cs
- HTTP client for file uploads using HttpWebRequest
- Multipart/form-data encoding
- Sends file + metadata:
  - file: Binary file data
  - fileName: Original filename
  - fileSize: Size in bytes
  - timestamp: ISO 8601 timestamp
- Async upload using ThreadPool

### 4. SettingsManager.cs
- Singleton pattern
- Uses ConfigurationManager for app.config
- Persistent storage for:
  - Scan folder path
  - Webhook URL
  - Auto-start preference
  - WebSocket port
  - Scanner programs (pipe-separated)

### 5. SettingsForm.cs
- Windows Forms dialog
- Folder browser integration
- Form validation
- Settings persistence

### 6. WebSocketServer.cs
- Manual WebSocket implementation for .NET 3.5
- TCP listener with TcpClient
- WebSocket handshake (Sec-WebSocket-Key)
- Frame encoding/decoding
- Supports text frames
- Broadcasts events to all connected clients

### 7. Logger.cs
- Singleton pattern
- File-based logging to %APPDATA%\ScanAgent\Logs
- In-memory log storage for quick access
- Daily log files (yyyy-MM-dd.log)

## File Upload Flow

```
1. File appears in scan folder
   ↓
2. FileSystemWatcher fires Created/Changed event
   ↓
3. FileWatcher.HandleFileEvent()
   ↓
4. Check if file is already processed or pending
   ↓
5. Add to pending set
   ↓
6. CheckFileStability() - wait 1 second
   ↓
7. Compare file sizes
   ↓
8. If stable → Fire FileDetected event
   ↓
9. MainForm.OnFileDetected()
   ↓
10. Determine webhook URL (session or settings)
    ↓
11. WebhookUploader.UploadFile() on background thread
    ↓
12. Create multipart form data
    ↓
13. Send HTTP POST request
    ↓
14. Parse JSON response for s3BucketUrl
    ↓
15. Broadcast file_uploaded via WebSocket
    ↓
16. Show balloon tip notification
```

## WebSocket Protocol

### Handshake
```
Client → Server:
GET / HTTP/1.1
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Key: {base64-key}

Server → Client:
HTTP/1.1 101 Switching Protocols
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Accept: {computed-accept-key}
```

### Frame Format
```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-------+-+-------------+-------------------------------+
|F|R|R|R| opcode|M| Payload len |    Extended payload length    |
|I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
|N|V|V|V|       |S|             |   (if payload len==126/127)   |
| |1|2|3|       |K|             |                               |
+-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
|     Extended payload length continued, if payload len == 127  |
+ - - - - - - - - - - - - - - - +-------------------------------+
|                               |Masking-key, if MASK set to 1  |
+-------------------------------+-------------------------------+
| Masking-key (continued)       |          Payload Data         |
+-------------------------------- - - - - - - - - - - - - - - - +
:                     Payload Data continued ...                :
+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
|                     Payload Data continued ...                |
+---------------------------------------------------------------+
```

## .NET 3.5 Constraints

### What's Available:
- Windows Forms (System.Windows.Forms)
- HttpWebRequest (System.Net)
- FileSystemWatcher (System.IO)
- TcpListener/TcpClient (System.Net.Sockets)
- ConfigurationManager (System.Configuration)
- ThreadPool (System.Threading)

### What's NOT Available:
- HttpClient (added in .NET 4.5)
- async/await (added in .NET 4.5)
- Task<T> (added in .NET 4.0)
- Built-in WebSocket support (added in .NET 4.5)
- JSON.NET or System.Text.Json

### Workarounds:
- Use HttpWebRequest instead of HttpClient
- Use ThreadPool.QueueUserWorkItem instead of async/await
- Manual WebSocket implementation
- Simple string parsing for JSON (no complex objects)

## Threading Model

```
Main Thread (UI Thread)
├─── NotifyIcon events
├─── Form events
└─── File detected callbacks

Background Threads
├─── FileSystemWatcher events
├─── WebSocket listener thread
├─── WebSocket client threads (one per connection)
└─── HTTP upload threads (ThreadPool)
```

## Data Flow

```
User Action → Settings Form → SettingsManager → app.config
                                    ↓
                              MainForm restarts FileWatcher
                                    ↓
File Created → FileSystemWatcher → FileWatcher → MainForm
                                                     ↓
                                          WebhookUploader (ThreadPool)
                                                     ↓
                                          HTTP POST → Webhook Server
                                                     ↓
                                          Response → Extract s3BucketUrl
                                                     ↓
                                          WebSocketServer.Broadcast()
                                                     ↓
                                          All connected clients
```

## Error Handling

- All file operations wrapped in try-catch
- HTTP errors logged and shown in notifications
- WebSocket errors logged, connections removed
- Settings errors logged, defaults used
- File watcher errors logged, watcher continues

## Security Considerations

- No authentication on WebSocket server (localhost only recommended)
- HTTP uploads use standard multipart/form-data
- No encryption (use HTTPS webhook URLs)
- File paths validated before processing
- Settings stored in plain text (app.config)

## Performance

- Minimal memory footprint (~15-30MB)
- Efficient file monitoring (OS-level events)
- Background thread uploads (non-blocking)
- HashSet for O(1) duplicate detection
- Lazy initialization of components

## Extensibility

To add new features:

1. **New WebSocket command**: Add handler in `WebSocketServer.HandleMessage()`
2. **New setting**: Add property in `SettingsManager` and UI in `SettingsForm`
3. **New file type**: Add MIME type in `WebhookUploader.GetMimeType()`
4. **New notification**: Call `MainForm.ShowNotification()`
5. **New log entry**: Call `Logger.Instance.Log()`

## Comparison with macOS Version

| Feature | macOS (Swift) | Windows (.NET 3.5) |
|---------|---------------|-------------------|
| UI Framework | Cocoa/AppKit | Windows Forms |
| Tray Icon | NSStatusItem | NotifyIcon |
| File Monitoring | FSEvents (DispatchSource) | FileSystemWatcher |
| HTTP Upload | URLSession | HttpWebRequest |
| WebSocket | NWProtocolWebSocket | Manual TCP implementation |
| Settings | UserDefaults | app.config |
| Threading | DispatchQueue | ThreadPool |
| Async | async/await | Callbacks |
| JSON | JSONSerialization | String parsing |
| Logging | Custom Logger | Custom Logger |

## Future Enhancements

- [ ] Add installer (WiX or NSIS)
- [ ] Add custom URL scheme registration
- [ ] Add auto-start registry setup
- [ ] Add system tray icon customization
- [ ] Add more robust JSON parsing
- [ ] Add WebSocket authentication
- [ ] Add file type filtering
- [ ] Add upload queue management
- [ ] Add retry logic for failed uploads
- [ ] Add progress indication for large files

