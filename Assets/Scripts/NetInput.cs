using Fusion;
using UnityEngine;

public enum InputButton
{
    Jump,
    Grapple,
    Glide,
    Break,
    Laser,
}

// Input storage to feed to Fusion so they can be replicated to the server
public struct NetInput : INetworkInput
{
    public NetworkButtons Buttons;
    public Vector2 Direction;
    public Vector2 LookDelta;
}