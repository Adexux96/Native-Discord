# Project Status: Native Discord (WinUI 3)

**Objective:** Build a lightweight, native Windows (WinUI 3) Discord client using a hybrid HTTP/WebSocket architecture to ensure both stability and real-time responsiveness.

### 1. Architecture & Core Decisions
*   **Framework:** WinUI 3 (Windows App SDK), **Unpackaged** (`WindowsPackageType=None`).
*   **Networking:** Custom [DiscordHttpService](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Services/DiscordHttpService.cs) (using `HttpClient`).
*   **Authentication:** [WebView2](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Views/LoginPage.xaml.cs) for login; user token extracted via script injection.
*   **Data Models:** Custom models in [DataModels.cs](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Models/DataModels.cs) with `[JsonPropertyName]` for serialization.

### 2. Libraries & External Dependencies (Planned)
*   **WebRTC (Voice/Video):** Need a wrapper like `MixedReality-WebRTC` or `SIPSorcery` for UDP voice packets and Opus encoding. WinUI 3 has no native WebRTC.
*   **Markdown Parsing:** Implemented via **Markdig** and **ColorCode.WinUI**. Features nested blocks, 1:1 Discord dark theme colors, and a custom "Super Booster" for C# syntax.
*   **Audio Management:** `NAudio` recommended for device enumeration and volume control.
*   **Global Inputs:** P/Invoke (`user32.dll`) required for system-wide Push-to-Talk keybinds.

### 3. Recent Accomplishments
*   **Friends UI Implementation:**
    *   **Tabs:** Online, All, Pending, Blocked, Add Friend tabs in [FriendsListPage.xaml](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Views/FriendsListPage.xaml).
    *   **Filtering:** Dynamically filters based on relationship type (Type 1=Friend, 2=Blocked, 3=Incoming, 4=Outgoing).
    *   **Search:** Implemented search functionality that filters by DisplayName/Username.
    *   **Add Friend View:** Custom UI with Wumpus illustration and "Send Request" functionality.
    *   **Status Logic:** Default status set to "Offline" (Gray). Online tab correctly filters using `IsOnline` property.
    *   **FriendsTabStyle:** Custom RadioButton style defined in Page.Resources.
*   **Search & Navigation (Quick Switcher):**
    *   **Global Search (Ctrl+K):** Implemented a quick switcher overlay in `MainWindow` for searching Servers, Channels, and Friends (`Type=1` Relationships).
    *   **Recents:** Automatically tracks visited channels and DMs in a "Recents" list, displayed when the search box is empty.
    *   **Navigation:** clicking a result now correctly navigates to the specific Chat (DM or Server Channel) using `FriendsPageNavigationArgs`.
    *   **UI Safety:** Fixed crashes related to missing icons in Recents by implementing robust null checks and switching to `Image` controls.
*   **Relationship Model Enhancements ([DataModels.cs](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Models/DataModels.cs)):**
    *   Added `StatusText` property (returns "Blocked", "Incoming Friend Request", "Outgoing Friend Request", or "Offline").
    *   Added `StatusColor` property (Red for blocked, Gray for others).
    *   Added `IsOnline` property (dynamically updated via Gateway presence events).
*   **Assets Configuration:**
    *   Created `Assets` folder with Wumpus image.
    *   Updated [NativeDiscord.csproj](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/NativeDiscord.csproj) to include Assets as Content with `CopyToOutputDirectory`.
*   **User Bar:** Display Name shown on top, Username on bottom (in [FriendsPage.xaml.cs](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Views/FriendsPage.xaml.cs)).
*   **Git:** Created [.gitignore](file:///c:/Users/Adexux/Desktop/Native%20Discord/.gitignore) for C#/.NET projects.
*   **Recents Persistence:** 
    *   Implemented local JSON storage in `DiscordHttpService`.
    *   Recents are now saved to `ApplicationData.Current.LocalFolder` and loaded on startup.
    *   Updated `DiscordService` to load recents during initialization.
*   **Voice Channels:**
    *   Implemented rendering of Voice Channels in `ServerPage`.
    *   Added `IconGlyph` support to distinguish between Text (#) and Voice (Speaker) channels.
    *   Filtered to show inline with text channels in the correct categories.
    *   **Voice State Tracking:**
        *   **Real-time Updates:** Subscribed to `VOICE_STATE_UPDATE` to track users joining/leaving/moving channels in real-time.
        *   **Visuals:** Integrated `VoiceUser` list into channel items, displaying avatars and names.
        *   **UI Polish:** Implemented accurate Mute/Deafen status icons (Red/Off glyphs) and User Limit display (e.g., "1/99").
*   **Real-time Gateway Integration:**
    *   **DiscordGatewayService:** Implemented WebSocket connection (`wss://gateway.discord.gg`), handling Heartbeats, Hello, Identify, and Dispatch events.
    *   **Live Updates:** Subscribed to `PRESENCE_UPDATE` and `MESSAGE_CREATE` events in `DiscordService`.
    *   **Initial Sync:** Successfully parsing `READY` payload to populate initial friend statuses (fixing "Offline on startup" issue).
    *   **Status Logic:** Full support for Online (Green), Idle (Yellow), DND (Red), and Offline (Gray) statuses.
    *   **Real-time Messages:** Chat pages now auto-update when new messages arrive without manual refresh.

*   **Complex Embeds & Attachments:**
    *   Implemented full support for displaying image attachments and rich embeds in `ChatPage`.
    *   Created reusable `AttachmentControl` and `EmbedControl` in a new `Controls` namespace.
    *   Updated `DataModels.cs` to handle `Attachments` and `Embeds` properties in messages.
    *   Fixed build issues by wrapping images in Borders for `CornerRadius`.
    
*   **Rich Presence / Activities:**
    *   **Data Models:** Added `Activity` and `ActivityAssets` support.
    *   **Status Sync:** `StatusText` now displays games/activities (e.g., "Playing Minecraft") instead of just "Online" in the Friends List.
    *   **Gateway:** Fully parsing `activities` from `PRESENCE_UPDATE` and `READY` events.
    *   **Active Now Sidebar:** 
        *   **UI:** Fully implemented Rich Presence cards with large/small images, headers, timestamps, and details.
        *   **Crash Fix:** Implemented `Converters.ToImageSource` to safely handle malformed or missing asset URLs.
        *   **Filtering:** Filters irrelevant activities (Custom Status) to show only real games/music.
        *   **Game Icon Resolution:** Successfully implemented fetching of missing game icons (e.g. Roblox) using the `GET /applications/{id}/rpc` endpoint.
        *   **Caching:** Implemented an in-memory cache for resolved application icons to minimize API calls.
        *   **Smart UI:** Refined the "Active Now" list to conditionally hide the large Rich Presence card for basic activities (like Roblox), ensuring a cleaner look similar to the official client.
    *   **Status Indicator & Stability Fixes:**
        *   **Status Colors:** Fixed hardcoded green status indicators; now dynamically Green (Online), Yellow (Idle), Red (DND/Blocked), or Gray (Offline).
        *   **Crash Resolution:** Fixed a `SolidColorBrush` threading crash by refactoring `StatusColor` to return a `Color` struct and creating the Brush in XAML.
        *   **Race Condition Fix:** Resolved an issue where initial presence data from `READY` payload was lost if processed before HTTP relationships were loaded. Now correctly caches and applies pending presences.
*   **Markdown & Syntax Highlighting (Perfect Discord 1:1):**
    *   **Engine:** Switched from basic Regex to **Markdig** for robust parsing and **ColorCode.WinUI** for syntax rendering.
    *   **Discord Theme:** Implemented a mathematically exact 1:1 Discord Dark Theme palette (#2c2d32 bg, #ff7b72 keywords, #d2a8ff functions, etc.).
    *   **Super Booster (v5.0):** Developed a metadata-aware recursive engine that traces tokens across WinUI tree boundaries to ensure perfect C# function highlighting without color bleeding into structural symbols like `()`.
    *   **Quotes:** Restored native Discord quote styling (thin gray bar) and added special green highlighting (#57f287) for Markdown quotes found inside code blocks.
    *   **Robustness:** Implemented surgical text splitting to handle fragmented tokens generated by high-performance UI rendering.
    *   **Message Editing & Chat Polish:**
    *   **Editing:** Full edit flow with `Enter` to save, `Esc` to cancel, and visual hints.
    *   **Context Aware:** Edit/Delete options (Context Menu & Hover Toolbar) restricted to user's own messages.
    *   **Visuals:** Added `(edited)` inline label and robust Date Headers (e.g., "Today", "December 28, 2025") with smart timestamp formatting.
    *   **Backend:** Integrated `MESSAGE_UPDATE` & `MESSAGE_DELETE` Gateway events for real-time sync.
    *   **Typing Indicators:**
        *   **Gateway:** Added handling for `TYPING_START` dispatch event.
        *   **UI:** Implemented "User is typing..." indicator with pulsing animation above input box.
    *   **Logic:** Smart handling of multiple users and 10s auto-expiration.
    *   **Message Reactions:**
    *   **Display:** Implemented `ReactionControl` to display emoji reactions with counts and "Me" highlighting (Blurple).
    *   **Interactive:** Clicking reactions toggles them via API (`AddReactionAsync`/`DeleteReactionAsync`).
    *   **Real-time:** Subscribed to `MESSAGE_REACTION_ADD` and `MESSAGE_REACTION_REMOVE` Gateway events for instant updates across clients.
    *   **Models:** Updated `DataModels.cs` with `Reaction` and `Emoji` support, including `INotifyPropertyChanged` for live count updates.
    *   **Interaction & Context Features:**
        *   **Reply Threading:**
            *   **UI:** Implemented "Spine" visual, mini-avatar, and referenced message preview in `ChatPage`.
            *   **Interaction:** Added "Reply" context menu item and "Replying to..." input bar with cancellation.
            *   **Backend:** Updated `DataModels.cs` and `DiscordHttpService` to handle `MessageReference` and `ReferencedMessage`.
        *   **Hover Toolbar:**
            *   **Design:** Floating toolbar (Row 1 anchor) matching Discord's layout with Quick Reactions (`👍 😂 🔥`).
            *   **Smart Visibility:** Dynamic showing of Edit (User's) vs Forward/Reply (Other's) buttons. Correctly implemented icon toggle (Pencil vs Arrow).
            *   **Actions:** Wired "Add Reaction", "Reply", "Forward", "Edit", and "Delete" actions directly from toolbar buttons.
            *   **Visuals:** Added message darken-on-hover effect.
        *   **Mention & Deep Link Improvements:**
            *   **Context-Aware Mentions:** Implemented smart channel mention display that conditionally hides the guild name if the mentioned channel belongs to the currently active server.
            *   **Deep Linking:** Enhanced the application's navigation system to support seamless "server-to-channel" jumps, ensuring the correct channel is selected and loaded when clicking mentions or using external links.
            *   **Embed Markdown Performance:**
                *   Upgraded `EmbedControl` to use `RichTextBlock` for descriptions, enabling full markdown support (including interactive mentions) within embeds.
                *   Implemented a notification system (`RefreshId`) that triggers instant UI updates as soon as a user or channel name is resolved in the background.
                *   Optimized server loading to proactively populate the channel cache, making mentions of channels within the current server resolve instantly without network overhead.

### 4. Current State of Code
*   **[MainWindow.xaml](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/MainWindow.xaml):** Custom TitleBar + Custom Server Rail + **Search Overlay**.
*   **[FriendsPage.xaml](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Views/FriendsPage.xaml):** Navigation Shell with Sidebar (Friends/Shop/Nitro/DMs) + Content Frame + User Footer. (Now accepts `FriendsPageNavigationArgs`).
*   **[FriendsListPage.xaml](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Views/FriendsListPage.xaml):** Full Friends UI with Tabs, Search, Filtering, Add Friend, and Active Now sidebar. Now live-updates via `DiscordService.PresenceUpdated`.
*   **[ChatPage.xaml](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Views/ChatPage.xaml):** Chats with **RichTextBlock** (Markdown) and **Profile Sidebar**. Handles live messages, editing, and deletion.
*   **[ServerPage.xaml](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Views/ServerPage.xaml):** 2-Column grid (Channel List | Chat Frame) with permission checks. Supports Voice Channels.
*   **[Helpers/MarkdownTextHelper.cs](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Helpers/MarkdownTextHelper.cs):** Advanced Markdown engine using **Markdig** + **ColorCode** + **Super Booster v5.0**. Handles syntax highlighting, nested blocks, and Discord-spec quotes.
*   **[Views/Converters.cs](file:///c:/Users/Adexux/Desktop/Native%20Discord/NativeDiscord/Views/Converters.cs):** Safe Type Converters (e.g. `ToImageSource`) for XAML bindings.

### 5. Key Files
| File | Purpose |
|------|---------|
| `DiscordHttpService.cs` | HTTP API calls (`GetRelationshipsAsync`, `SendFriendRequestAsync`, etc.) |
| `DiscordGatewayService.cs` | WebSocket connection for real-time events (`READY`, `PRESENCE_UPDATE`, `MESSAGE_CREATE`) |
| `DiscordService.cs` | Central hub managing HTTP+Gateway, shared state (Relationships, Guilds), and event propagation |
| `DataModels.cs` | User, Server, Channel, Message, Relationship models (updated with Gateway payloads) |
| `FriendsListPage.xaml/.cs` | Friends UI with tabs, search, filtering |
| `ChatPage.xaml/.cs` | Message display with markdown and send functionality |

### 6. Known Issues / Next Steps

#### 🎤 Voice & Audio (Priority: High)
1.  **Voice Audio Connection:** Implement WebRTC/UDP voice logic using `SIPSorcery` or `MixedReality-WebRTC`.
2.  **Opus Codec:** Integrate Opus encoding/decoding for Discord's audio format.
3.  **Audio Device Management:** Use `NAudio` for microphone/speaker selection and volume control.
4.  **Push-to-Talk:** Implement global keyboard hooks via P/Invoke (`user32.dll`) for system-wide keybinds.
5.  **Voice Activity Detection (VAD):** Add automatic voice detection as an alternative to PTT.

#### 💬 Messaging & Content (Priority: Medium)
7.  **File Uploads:** Implement file/image upload functionality in chat.
8.  **Message History:** Implement infinite scroll / "Load More" for older messages.
9.  **System Notifications:** Trigger Windows toast notifications for DMs and @mentions when the app is backgrounded.
10. **Reaction Picker:** Implement UI for selecting any emoji (currently only quick reactions work).
11. **Message Forwarding:** Implement logic to forward messages to other channels/users.

#### 🖥️ UI/UX Enhancements (Priority: Medium)
12. **Server Settings:** View/edit server settings (name, icon, roles) for admins.
13. **User Settings:** Profile editing, status customization, privacy settings.
14. **Theming:** Allow custom themes or light mode support.
15. **Notifications:** Windows toast notifications for DMs and mentions.
16. **Unread Indicators:** Badge counts and visual indicators for unread messages/channels.
17. **Keyboard Navigation:** Full keyboard shortcuts (Escape to close, Enter to send, etc.).

#### 🔒 Security & Auth (Priority: Medium)
20. **Token Refresh:** Handle token expiration and automatic re-authentication.
21. **2FA Support:** Handle two-factor authentication during login.
22. **Secure Storage:** Store tokens securely using Windows Credential Manager.

#### 🌐 Advanced Features (Priority: Low)
23. **Video Calls:** Extend WebRTC to support video streams.
24. **Screen Sharing:** Capture and stream desktop/application windows.
25. **Nitro Features:** Animated avatars, custom emojis, higher upload limits (if applicable).
26. **Stickers & GIFs:** Full sticker/GIF picker and rendering.
27. **Threads:** Support for Discord thread channels.
28. **Forums:** Display and interact with Forum channels.
29. **Stage Channels:** Support for Stage/broadcast channels.
30. **Scheduled Events:** Display and RSVP to server events.

#### 🛠️ Technical Debt & Polish
31. **Error Handling:** Comprehensive error messages and retry logic for API failures.
32. **Offline Mode:** Cache messages and show offline indicator when disconnected.
33. **Performance:** Virtualize long lists (messages, members) for smooth scrolling.
34. **Unit Tests:** Add test coverage for services and critical logic.
35. **Logging:** Implement structured logging for debugging production issues.