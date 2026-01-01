# Native Discord (WinUI 3)

## Project Overview
This project is a lightweight, native Windows Discord client built using **WinUI 3 (Windows App SDK)** and **.NET 8**. It aims to provide a stable, responsive experience by using native controls and a custom networking layer, avoiding the resource heaviness of Electron.

## Technology Stack
*   **Framework:** WinUI 3 (Windows App SDK 1.6+)
*   **Language:** C# (.NET 8)
*   **Packaging:** Unpackaged (`WindowsPackageType=None`) for easier development and deployment.
*   **Dependencies:**
    *   `Markdig`: For Markdown parsing in chat.
    *   `ColorCode`: For syntax highlighting in code blocks.
    *   `Newtonsoft.Json`: For JSON serialization (implied by usage, though System.Text.Json is also mentioned in summary).

## Architecture
The application follows a modular architecture separating UI, Data, and Network logic:

### Core Services (`NativeDiscord/Services/`)
*   **`DiscordService.cs`:** The central hub that orchestrates data flow. It manages the state (current user, relationships, selected guild) and coordinates between the HTTP and Gateway services.
*   **`DiscordHttpService.cs`:** Handles all REST API calls to Discord (Login, Friend Requests, Channel messages) using a custom `HttpClient` wrapper.
*   **`DiscordGatewayService.cs`:** Manages the persistent WebSocket connection (`wss://gateway.discord.gg`) for real-time events like incoming messages, presence updates, and voice state changes.

### UI Structure (`NativeDiscord/Views/`)
*   **`MainWindow.xaml`:** The application shell containing the custom title bar, server rail, and main navigation frame.
*   **`FriendsListPage.xaml`:** The "Home" screen displaying friends (Online, All, Pending), active now sidebar, and friend requests.
*   **`ServerPage.xaml`:** Displays server channels (Text & Voice) and the chat interface.
*   **`ChatPage.xaml`:** The main chat view supporting rich text, embeds, and attachments.

### Data Models (`NativeDiscord/Models/`)
*   **`DataModels.cs`:** Contains strong-typed C# classes mirroring Discord's API objects (User, Message, Guild, Channel), using `[JsonPropertyName]` for serialization.

## Building and Running
**Note: Building is currently not supported in this environment.**

This is a standard .NET 8 project. Under normal circumstances, it would be built using the following steps:

**Prerequisites:**
*   .NET 8 SDK
*   Visual Studio 2022 (with "Windows App SDK C# Templates" workload) OR VS Code.

**Typical Commands:**
```powershell
# Restore dependencies
dotnet restore

# Build the project
dotnet build NativeDiscord/NativeDiscord.csproj

# Run the project (Unpackaged)
dotnet run --project NativeDiscord/NativeDiscord.csproj
```

## Key Features & Status
*   **Authentication:** WebView2-based login with token extraction.
*   **Messaging:** Real-time sending/receiving, Markdown support, Embeds, Attachments.
*   **Friends System:** Full management (Add, Block, Accept/Decline), with real-time status updates.
*   **Voice Support:** Visuals implemented (Channel lists, User states), but **Audio (WebRTC) is currently TODO**.
*   **Rich Presence:** View other users' activities (Games, Spotify) in the "Active Now" sidebar.

## Development Conventions
*   **Unpackaged App:** The project is configured as an unpackaged WinUI app to simplify the edit-debug loop.
*   **Manual JSON Mapping:** Data models often map manually to API responses; check `DataModels.cs` when adding new API fields.
*   **Asset Management:** Assets are stored in `NativeDiscord/Assets` and copied to the output directory on build.
