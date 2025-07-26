# WebSocket Chat Client

Advanced WebSocket chat client with group support, built with .NET 9.0 and Nerdbank.Streams.

## Features

### 🔗 **Connection Management**
- WebSocket connection with multiplexing support
- Automatic reconnection handling
- Real-time connection status

### 💬 **Chat Features**
- **Public Chat**: Send messages to all users
- **Private Messages**: Direct messaging between users
- **Room Chat**: Group messaging in dedicated rooms
- Real-time message delivery

### 🏠 **Room Management**
- Create public/private rooms
- Password-protected rooms
- Join/leave rooms
- Room member management
- Invite/kick users

### 📁 **File Transfer**
- Send files to users or rooms
- Progress tracking
- Auto-accept option
- Multiple file format support
- Organized downloads by sender

## Commands

### Basic Commands
```
/connect [url]     - Connect to server (default: ws://localhost:5106/ws)
/disconnect        - Disconnect from server
/username <name>   - Set username
/users             - List online users
/help or /?        - Show help
/quit              - Exit application
```

### Chat Commands
```
/msg <user> <message>              - Send private message
/pm <user> <message>               - Send private message (alias)
/private <user> <message>          - Send private message (alias)
/privateMessage <user> <message>   - Send private message (alias)
/room <roomid> <message>           - Send message to specific room
/room <message>                    - Send message to current room (if joined)
```

### Room Commands
```
/create <name> [desc] [-private] [-password <pwd>]  - Create room
/join <roomid> [password]                           - Join room
/leave [roomid]                                     - Leave room
/rooms                                              - List available rooms
/members [roomid]                                   - List room members
/invite <roomid> <user>                            - Invite user to room
/kick <roomid> <user>                              - Kick user from room
```

### File Commands
```
/send [-a] <filepath> [username|roomid]  - Send file to user or room
/accept <fileId>                         - Accept incoming file
/reject <fileId>                         - Reject incoming file
```

## Architecture

### Core Components
- **ChatClient**: Main client implementation
- **WebSocketConnectionManager**: Connection handling with multiplexing
- **FileManager**: File upload/download management
- **MessageProcessors**: Chat and file transfer message processing
- **CommandParser**: Command line parsing with options support

### Design Patterns
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Observer Pattern**: Event-driven architecture
- **Strategy Pattern**: Different message processors
- **Factory Pattern**: Service registration

### Technologies
- **.NET 9.0**: Latest .NET framework
- **Nerdbank.Streams**: Multiplexing WebSocket streams
- **System.Text.Json**: JSON serialization
- **Microsoft.Extensions.Hosting**: Application hosting
- **Microsoft.Extensions.Logging**: Structured logging

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- WebSocket chat server running on ws://localhost:5106/ws

### Installation
```bash
git clone <repository-url>
cd WebSocketChatClient1
dotnet restore
dotnet build
```

### Running
```bash
dotnet run
```

### Basic Usage
1. Start the application
2. Connect to server: `/connect ws://your-server-url/ws`
3. Set username: `/username YourName`
4. Start chatting!

## File Structure
```
WebSocketChatClient1/
├── Client.cs              # Main chat client implementation
├── ConsoleApplication.cs  # Console UI and interaction
├── Connection.cs          # WebSocket connection management
├── Processors.cs          # Message processing logic
├── Services.cs            # File management and utilities
├── Models.cs              # Data models and DTOs
├── Interfaces.cs          # Service interfaces
├── Extensions.cs          # Dependency injection setup
├── Program.cs             # Application entry point
└── downloads/             # Downloaded files (organized by sender)
```

## Configuration

### Default Settings
- **Server URL**: ws://localhost:5106/ws
- **Download Path**: ./downloads/
- **File Chunk Size**: 4096 bytes

### Customization
Modify `Program.cs` or use configuration files to customize:
- Server endpoints
- Download directories
- Logging levels
- Connection timeouts

## Examples

### Basic Chat Session
```
[Public] > /connect
Status: Connected to server
[Public] > /username Alice
Status: Username set to: Alice
[Public] > Hello everyone!
[12:34:56] Alice: Hello everyone!
```

### Room Chat
```
[Public] > /create myroom "My Cool Room"
[12:34:56] ✓ Room created: myroom
[Public] > /join myroom
[12:34:56] ✓ Joined room: myroom
[myroom] > Hello room members!
[12:34:56] [ROOM] Alice: Hello room members!
```

### File Transfer
```
[Public] > /send document.pdf bob
Status: Uploading file to server: document.pdf (ID: abc123-def456)
Status: File offer sent to user 'bob': document.pdf
Status: File ID: abc123-def456 - Recipients can use '/accept abc123-def456' to download
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions:
- Create an issue in the repository
- Check existing documentation
- Review the code comments for implementation details
