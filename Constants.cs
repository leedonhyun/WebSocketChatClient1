namespace ChatSystem.Client
{
    /// <summary>
    /// Contains constant values for the Chat Client.
    /// </summary>
    public static class ClientConstants
    {
        public const string DefaultServerUrl = "ws://localhost:5106/ws";
        public const string RoomPrefix = "ROOM:";
        public const char CommandArgSeparator = '|';
        public const char MessagePartSeparator = ':';

        /// <summary>
        /// Constants for message types used in communication protocols.
        /// </summary>
        public static class MessageTypes
        {
            public const string Chat = "chat";
            public const string PrivateMessage = "privateMessage";
            public const string FileUpload = "fileUpload";
            public const string FileUploadComplete = "fileUploadComplete";
            public const string FileOffer = "fileOffer";
            public const string FileOfferAuto = "fileOfferAuto";
            public const string FileAccept = "fileAccept";
            public const string FileReject = "fileReject";
            public const string SetUsername = "setUsername";
            public const string ListUsers = "listUsers";
            public const string CreateRoom = "createRoom";
            public const string JoinRoom = "joinRoom";
            public const string LeaveRoom = "leaveRoom";
            public const string ListRooms = "listRooms";
            public const string ListRoomMembers = "listRoomMembers";
            public const string RoomMembers = "roomMembers";
            public const string InviteToRoom = "inviteToRoom";
            public const string KickFromRoom = "kickFromRoom";
            public const string System = "system";
            public const string RoomMessage = "roomMessage";
            public const string UserList = "userList";
            public const string RoomList = "roomList";
            public const string RoomJoined = "roomJoined";
            public const string RoomLeft = "roomLeft";
            public const string RoomCreated = "roomCreated";
            public const string FileError = "fileError";
            public const string FileData = "fileData";
            public const string FileComplete = "fileComplete";
        }

        /// <summary>
        /// Constants for client-side commands.
        /// </summary>
        public static class Commands
        {
            public const string Connect = "connect";
            public const string Disconnect = "disconnect";
            public const string Username = "username";
            public const string Users = "users";
            public const string Help = "help";
            public const string HelpAlt = "?";
            public const string Send = "send";
            public const string Accept = "accept";
            public const string Reject = "reject";
            public const string Msg = "msg";
            public const string Pm = "pm";
            public const string Private = "private";
            public const string PrivateMessage = "privateMessage";
            public const string Create = "create";
            public const string CreateRoom = "createroom";
            public const string Join = "join";
            public const string JoinRoom = "joinroom";
            public const string Leave = "leave";
            public const string LeaveRoom = "leaveroom";
            public const string Rooms = "rooms";
            public const string ListRooms = "listrooms";
            public const string Members = "members";
            public const string RoomMembers = "roommembers";
            public const string Invite = "invite";
            public const string Kick = "kick";
            public const string Room = "room";
            public const string Quit = "/quit";
        }

        /// <summary>
        /// Format strings for status messages displayed to the user.
        /// </summary>
        public static class StatusMessages
        {
            public const string RoomJoined = "Current room set to: {0}";
            public const string RoomLeft = "Left current room";
            public const string PrivateMessageSent = "Private message sent to {0}";
            public const string RoomMessageSent = "Message sent to room '{0}': {1}";
            public const string FileUploading = "Uploading file to server: {0} (ID: {1})";
            public const string FileUploadProgress = "Uploading: {0} - {1:F1}%";
            public const string FileUploadComplete = "File upload completed: {0} (ID: {1}). Sending offer...";
            public const string FileOfferSent = "{0}File offer sent to {1}: {2}";
            public const string FileOfferInfo = "File ID: {0} - Recipients can use '/accept {0}' to download";
            public const string FileAccepted = "File accepted: {0}";
            public const string FileRejected = "File rejected: {0}";
            public const string UsernameSet = "Username set to: {0}";
            public const string RoomCreating = "Creating room: {0}";
            public const string RoomJoining = "Joining room: {0}";
            public const string RoomLeftTarget = "Left room: {0}";
            public const string InvitingUser = "Inviting {0} to room {1}";
            public const string KickingUser = "Kicking {0} from room {1}";
            public const string CurrentRoomInfo = "  Current room: {0}";
        }

        /// <summary>
        /// Format strings for error and warning messages.
        /// </summary>
        public static class ErrorMessages
        {
            public const string FileNotFound = "File not found: {0}";
            public const string AccessDenied = "Access denied: Cannot read file {0}";
            public const string FileSendError = "File send error: {0}";
            public const string NoRoomToLeave = "No room to leave";
            public const string NoRoomSpecified = "No room specified";
            public const string NotConnected = "❌ Not connected to server. Use /connect to connect first.";
            public const string InvalidCommand = "Invalid command: {0}";
            public const string UnknownCommand = "Unknown command: {0}";
            public const string LogParseChatMessageFailed = "Failed to parse chat message";
            public const string LogParseFileMessageFailed = "Failed to parse file message";
            public const string LogReceiveMessageError = "Error receiving messages";
            public const string LogReceiveFileError = "Error receiving file transfers";
            public const string NotConnectedSimple = "Not connected to server. Use /connect to connect.";
        }

        /// <summary>
        /// Usage instructions for commands.
        /// </summary>
        public static class UsageMessages
        {
            public const string SendUsage = "Usage: /send [-a] <filepath> [username|roomid]";
            public const string SendExamplesHeader = "Examples:";
            public const string SendExamplePublic = "  /send myfile.txt - Send to public (all users)";
            public const string SendExampleUser = "  /send myfile.txt john - Send to user 'john'";
            public const string SendExampleRoom = "  /send myfile.txt room123 - Send to room 'room123'";
            public const string SendExampleAuto = "  /send -a myfile.txt john - Auto-accept for user 'john'";
            public const string PrivateMessageUsage = "Usage: /msg <username> <message>";
            public const string PrivateMessageExample = "  Example: /msg john Hello there!";
            public const string PrivateMessageAliases = "  Aliases: /pm, /private, /privateMessage";
            public const string CreateRoomUsage = "Usage: /create <roomname> [description] [-private] [-password <pwd>]";
            public const string CreateRoomExample = "  Example: /create myroom \"My cool room\" -private -password secret123";
            public const string JoinRoomUsage = "Usage: /join <roomid> [password]";
            public const string JoinRoomExample = "  Example: /join room123 mypassword";
            public const string InviteUsage = "Usage: /invite <roomid> <username>";
            public const string KickUsage = "Usage: /kick <roomid> <username>";
            public const string RoomMessageUsage = "Usage: /room <roomid> <message>";
            public const string RoomMessageAltUsage = "  Or if you're in a room: /room <message>";
            public const string RoomMessageExample = "  Example: /room general Hello everyone!";
        }

        /// <summary>
        /// Help text for the /help command.
        /// </summary>
        public static readonly string[] HelpText =
        {
            "=== Available Commands ===",
            "Basic Commands:",
            "  /connect [url] - Connect to server",
            "  /disconnect - Disconnect from server",
            "  /username <name> - Set username",
            "  /users - List online users",
            "  /help or /? - Show this help",
            "",
            "Chat Commands:",
            "  /msg <user> <message> - Send private message",
            "  /room <roomid> <message> - Send message to specific room",
            "",
            "Room Commands:",
            "  /create <name> [desc] [-private] [-password <pwd>] - Create room",
            "  /join <roomid> [password] - Join room",
            "  /leave [roomid] - Leave room",
            "  /rooms - List available rooms",
            "  /members [roomid] - List room members",
            "  /invite <roomid> <user> - Invite user to room",
            "  /kick <roomid> <user> - Kick user from room",
            "",
            "File Commands:",
            "  /send [-a] <filepath> [username|roomid] - Send file to user or room",
            "  /accept <fileId> - Accept incoming file",
            "  /reject <fileId> - Reject incoming file"
        };

        /// <summary>
        /// Constants specific to the Console UI.
        /// </summary>
        public static class ConsoleUI
        {
            public const string WelcomeHeader = "=== Advanced WebSocket Chat Client with Group Support ===";
            public const string Note = "Note: Any message not starting with '/' will be sent as public chat";
            public const string CurrentRoomInfo = "       Currently in room: {0}";
            public const string PromptPublic = "[Public] > ";
            public const string PromptDisconnected = "[Disconnected] > ";
            public const string PromptRoomFormat = "[{0}] > ";
            public const string StatusFormat = "Status: {0}";
            public const string FileOfferHeader = "\n*** FILE OFFER RECEIVED ***";
            public const string FileOfferFrom = "From: {0}";
            public const string FileOfferFile = "File: {0}";
            public const string FileOfferSize = "Size: {0:N0} bytes";
            public const string FileOfferId = "File ID: {0}";
            public const string FileOfferReady = "*** READY FOR DOWNLOAD ***";
            public const string FileOfferInstructions = "Use '/accept {0}' to accept or '/reject {0}' to reject";
            public const string FileTransferProgress = "File transfer progress: {0} - {1:F1}% ({2}/{3})";

            public static class MessageFormats
            {
                public const string System = "[{0}] * {1}";
                public const string Chat = "[{0}] {1}: {2}";
                public const string Private = "[{0}] [PRIVATE] {1}: {2}";
                public const string Room = "[{0}] [ROOM] {1}: {2}";
                public const string UserList = "[{0}] Online users: {1}";
                public const string RoomList = "[{0}] Available rooms: {1}";
                public const string RoomMembers = "[{0}] Room members: {1}";
                public const string RoomJoined = "[{0}] ✓ Joined room: {1}";
                public const string RoomLeft = "[{0}] ← Left room: {1}";
                public const string RoomCreated = "[{0}] ✓ Room created: {1}";
            }

            public static readonly string[] HelpText =
            {
                "Basic Commands:",
                "  /connect [url] - Connect to server",
                "  /disconnect - Disconnect from server",
                "  /username <name> - Set username",
                "  /users - List online users",
                "  /quit - Exit application",
                "",
                "Chat Commands:",
                "  /msg <user> <message> - Send private message",
                "  /pm <user> <message> - Send private message (alias)",
                "  /private <user> <message> - Send private message (alias)",
                "  /privateMessage <user> <message> - Send private message (alias)",
                "  /room <roomid> <message> - Send message to specific room",
                "  /room <message> - Send message to current room (if joined)",
                "",
                "Room Commands:",
                "  /create <name> [desc] [-private] [-password <pwd>] - Create room",
                "  /join <roomid> [password] - Join room",
                "  /leave [roomid] - Leave room",
                "  /rooms - List available rooms",
                "  /members [roomid] - List room members",
                "  /invite <roomid> <user> - Invite user to room",
                "  /kick <roomid> <user> - Kick user from room",
                "",
                "File Commands:",
                "  /send [-a] <filepath> [username|roomid] - Send file to user or room",
                "  /accept <fileId> - Accept incoming file",
                "  /reject <fileId> - Reject incoming file"
            };
        }

        /// <summary>
        /// Constants for message processors.
        /// </summary>
        public static class ProcessorMessages
        {
            public const string LogProcessingFileMessage = "Processing file message: {0} for file {1}";
            public const string LogUnknownFileType = "Unknown file transfer message type: {0}";
            public const string LogProcessingError = "Error processing file transfer message";
            public const string StatusFileAccepted = "File accepted by {0}";
            public const string StatusFileRejected = "File rejected by {0}";
            public const string StatusFileError = "❌ File transfer error: {0}";
            public const string StatusFileOfferReceived = "File offer received: {0}";
            public const string StatusAutoDownloading = "🔄 Auto-downloading: {0} from {1}";
            public const string StatusChunkSaved = "Chunk {0}/{1} saved";
            public const string StatusDownloadComplete = "✅ File download completed!";
            public const string StatusDownloadFrom = "👤 From: {0}";
            public const string StatusDownloadFile = "📁 File: {0}";
            public const string StatusDownloadLocation = "📍 Location: {0}";
            public const string StatusDownloadSize = "📏 Size: {0:N0} bytes";
            public const string StatusIntegrityVerified = "✅ File integrity verified!";
            public const string StatusIntegrityWarning = "⚠️ WARNING: Size mismatch! Expected {0:N0}, got {1:N0}";
            public const string StatusProcessingError = "Error processing file transfer: {0}";
        }
    }
}