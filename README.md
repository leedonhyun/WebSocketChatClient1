# WebSocket Chat Client

This project is a .NET-based console application that acts as a client for the WebSocket Chat Server. It allows users to connect to the server, join chat rooms, and exchange messages in real-time.

## Features

- **WebSocket Connection**: Connects to the chat server using WebSockets.
- **Command-Line Interface**: A simple and interactive console interface for sending commands.
- **Real-Time Messaging**: Receives and displays messages from the server in real-time.
- **Command Handling**: Sends user input as commands to the server (e.g., creating/joining rooms, sending messages).
- **Asynchronous Operations**: Built with `async/await` to handle network communication without blocking the UI.

## Core Components

- **`ConsoleApplication`**: The main class that runs the client, handles user input, and displays messages.
- **`Client`**: Manages the WebSocket connection to the server.
- **`ChatHandler`**: Processes incoming messages from the server and displays them appropriately.
- **`ICommand` Interface**: Represents a command that can be sent from the client to the server (e.g., `SendRoomMessageCommand`).

## Getting Started

1.  **Prerequisites**: .NET 9 SDK.
2.  **Configuration**: Ensure the server address in the client configuration points to your running WebSocket Chat Server instance.
3.  **Run the client**: Launch the console application.

## How to Use

Once the application is running, you can use the following commands in the console:

- `create <roomName>`: Creates a new chat room.
- `join <roomId>`: Joins an existing chat room.
- `send <roomId> <message>`: Sends a message to all members of a room.
- `exit`: Closes the application.