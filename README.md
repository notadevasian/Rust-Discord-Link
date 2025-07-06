# WebSocket Client Extension - Plugin Hooks Documentation

## Overview

The WebSocket Client Extension provides several hooks that your plugins can implement to handle WebSocket events. These hooks are automatically called when specific WebSocket events occur.

### Available Hooks

---

### OnWebSocketConnected

Called when a WebSocket connection is successfully established.

**Parameters:**

*   `connectionId` (string): The unique identifier for the connection.

**Example:**

```csharp
void OnWebSocketConnected(string connectionId)
{
    Puts($"WebSocket connection '{connectionId}' established successfully");
}
```

---

### OnWebSocketConnectionFailed

Called when a WebSocket connection attempt fails.

**Parameters:**

*   `connectionId` (string): The unique identifier for the connection.
*   `errorMessage` (string): The error message describing why the connection failed.

**Example:**

```csharp
void OnWebSocketConnectionFailed(string connectionId, string errorMessage)
{
    Puts($"WebSocket connection '{connectionId}' failed: {errorMessage}");
}
```

---

### OnWebSocketMessage

Called when a message is received from the WebSocket server.

**Parameters:**

*   `connectionId` (string): The unique identifier for the connection.
*   `message` (string): The received message content.

**Example:**

```csharp
void OnWebSocketMessage(string connectionId, string message)
{
    Puts($"Received message from '{connectionId}': {message}");

    // Parse and handle the message
    // Example: JSON parsing, command processing, etc.
}
```

---

### OnWebSocketDisconnected

Called when a WebSocket connection is closed (either by the client or server).

**Parameters:**

*   `connectionId` (string): The unique identifier for the connection.

**Example:**

```csharp
void OnWebSocketDisconnected(string connectionId)
{
    Puts($"WebSocket connection '{connectionId}' has been disconnected");

    // Clean up any connection-specific data
    // Attempt reconnection if needed
}
```

---

### OnWebSocketError

Called when a WebSocket error occurs during operation.

**Parameters:**

*   `connectionId` (string): The unique identifier for the connection.
*   `errorMessage` (string): The error message describing what went wrong.

**Example:**

```csharp
void OnWebSocketError(string connectionId, string errorMessage)
{
    Puts($"WebSocket error on connection '{connectionId}': {errorMessage}");

    // Handle the error appropriately
    // Log for debugging, attempt reconnection, etc.
}
```