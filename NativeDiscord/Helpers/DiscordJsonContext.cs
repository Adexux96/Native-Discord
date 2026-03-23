using System.Collections.Generic;
using System.Text.Json.Serialization;
using NativeDiscord.Models;

namespace NativeDiscord.Helpers
{
    [JsonSerializable(typeof(User))]
    [JsonSerializable(typeof(Server))]
    [JsonSerializable(typeof(Channel))]
    [JsonSerializable(typeof(Message))]
    [JsonSerializable(typeof(Relationship))]
    [JsonSerializable(typeof(ReadyPayload))]
    [JsonSerializable(typeof(PresenceUpdate))]
    [JsonSerializable(typeof(GuildMember))]
    [JsonSerializable(typeof(Role))]
    [JsonSerializable(typeof(Application))]
    [JsonSerializable(typeof(GatewayHello))]
    [JsonSerializable(typeof(MessageDeletedPayload))]
    [JsonSerializable(typeof(VoiceState))]
    [JsonSerializable(typeof(TypingStartPayload))]
    [JsonSerializable(typeof(MessageReactionUpdatePayload))]
    [JsonSerializable(typeof(IdentifyPayload))]
    [JsonSerializable(typeof(IdentifyProperties))]
    [JsonSerializable(typeof(GatewayPayload))]
    [JsonSerializable(typeof(MessageReference))]
    [JsonSerializable(typeof(PermissionOverwrite))]
    [JsonSerializable(typeof(Activity))]
    [JsonSerializable(typeof(ActivityAssets))]
    [JsonSerializable(typeof(ActivityTimestamps))]
    [JsonSerializable(typeof(Attachment))]
    [JsonSerializable(typeof(Embed))]
    [JsonSerializable(typeof(EmbedImage))]
    [JsonSerializable(typeof(Emoji))]
    [JsonSerializable(typeof(Reaction))]
    [JsonSerializable(typeof(MessageRequest))]
    [JsonSerializable(typeof(EditMessageRequest))]
    [JsonSerializable(typeof(FriendRequest))]
    [JsonSerializable(typeof(List<Relationship>))]
    [JsonSerializable(typeof(List<Server>))]
    [JsonSerializable(typeof(List<Channel>))]
    [JsonSerializable(typeof(List<Message>))]
    [JsonSerializable(typeof(List<Role>))]
    [JsonSerializable(typeof(List<PermissionOverwrite>))]
    [JsonSerializable(typeof(List<User>))]
    [JsonSerializable(typeof(List<Activity>))]
    [JsonSerializable(typeof(List<Attachment>))]
    [JsonSerializable(typeof(List<Embed>))]
    [JsonSerializable(typeof(List<Reaction>))]
    [JsonSerializable(typeof(List<VoiceState>))]
    [JsonSerializable(typeof(List<PresenceUpdate>))]
    public partial class DiscordJsonContext : JsonSerializerContext
    {
    }
}
