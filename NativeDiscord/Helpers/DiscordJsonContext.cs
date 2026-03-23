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
    [JsonSerializable(typeof(List<Relationship>))]
    [JsonSerializable(typeof(List<Server>))]
    [JsonSerializable(typeof(List<Channel>))]
    [JsonSerializable(typeof(List<Message>))]
    [JsonSerializable(typeof(List<Role>))]
    [JsonSerializable(typeof(MessageReference))]
    public partial class DiscordJsonContext : JsonSerializerContext
    {
    }
}
