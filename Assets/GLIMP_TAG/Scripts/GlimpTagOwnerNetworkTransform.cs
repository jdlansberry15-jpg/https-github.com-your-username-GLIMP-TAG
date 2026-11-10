using Unity.Netcode.Components;

/// <summary>
/// Owner-authoritative player transform for responsive arm-swing locomotion.
/// Add this instead of the stock NetworkTransform on the player root.
/// The server receives the owner's transform updates, but must validate them
/// before this is safe for public competitive matches.
/// </summary>
public sealed class GlimpTagOwnerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
