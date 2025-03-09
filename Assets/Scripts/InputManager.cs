using Fusion;
using Fusion.Addons.KCC;
using Fusion.Menu;
using Fusion.Sockets;
using MultiClimb.Menu;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// This will be attached to the Runner, which isn't a NetworkObject. SimulationBehaviour can be attached to Network Runners for FixedNetworkUpdates (FixedUpdates for physics)
public class InputManager : SimulationBehaviour, IBeforeUpdate, IAfterUpdate, INetworkRunnerCallbacks
{
    public Player LocalPlayer;
    public Vector2 AccumulatedMouseDelta => _mouseDeltaAccumulator.AccumulatedValue;    

    private NetInput _accumulatedInput;
    private Vector2Accumulator _mouseDeltaAccumulator = new Vector2Accumulator() { SmoothingWindow = 0.025f };
    private bool _resetInput;

    void IBeforeUpdate.BeforeUpdate()
    {
        if (_resetInput)
        {
            _resetInput = false;
            _accumulatedInput = default;
        }
        Keyboard keyboard = Keyboard.current;
        // Checking whether the cursor is locked to (focused on) the game window
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // Prevent from accumulating input when the mouse isn't focused on the game window
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        NetworkButtons buttons = default;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            Vector2 lookRotationDelta = new Vector2(-mouseDelta.y, mouseDelta.x);
            // _accumulatedInput.LookDelta += lookRotationDelta;
            _mouseDeltaAccumulator.Accumulate(lookRotationDelta);
            buttons.Set(InputButton.Grapple, mouse.rightButton.isPressed);
            buttons.Set(InputButton.Laser, mouse.leftButton.isPressed);
        }

        if (keyboard != null)
        {
            if (keyboard.rKey.wasPressedThisFrame && LocalPlayer != null)
            {
                LocalPlayer.RPC_SetReady();
            }

            Vector2 moveDirection = Vector2.zero;
            if (keyboard.wKey.isPressed)
            {
                moveDirection += Vector2.up;
            }
            if (keyboard.sKey.isPressed)
            {
                moveDirection += Vector2.down;
            }
            if (keyboard.aKey.isPressed)
            {
                moveDirection += Vector2.left;
            }
            if (keyboard.dKey.isPressed)
            {
                moveDirection += Vector2.right;
            }
            // Set the direction based on input and the jump button
            _accumulatedInput.Direction += moveDirection;
            buttons.Set(InputButton.Jump, keyboard.spaceKey.isPressed);
            buttons.Set(InputButton.Glide, keyboard.leftShiftKey.isPressed);
            buttons.Set(InputButton.Break, keyboard.leftCtrlKey.isPressed);
        }
        // Pass the buttons set through the use of bitwise OR
        _accumulatedInput.Buttons = new NetworkButtons(_accumulatedInput.Buttons.Bits | buttons.Bits);
    }

    public void AfterUpdate()
    {

    }

    public void OnConnectedToServer(NetworkRunner runner)
    {

    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {

    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {

    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        _accumulatedInput.Direction.Normalize();
        _accumulatedInput.LookDelta = _mouseDeltaAccumulator.ConsumeTickAligned(runner);
        input.Set(_accumulatedInput);
        // Setting the reset flag to true so that the accumulation is reset upon the next call of BeforeUpdate()
        _resetInput = true;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Focusing the cursor on the game window when a player joins
        if (player == runner.LocalPlayer) 
        { 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {

    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {

    }

    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (shutdownReason == ShutdownReason.DisconnectedByPluginLogic)
        {
            await FindFirstObjectByType<MenuConnectionBehaviour>(FindObjectsInactive.Include).DisconnectAsync(ConnectFailReason.Disconnect);
            FindFirstObjectByType<FusionMenuUIGameplay>(FindObjectsInactive.Include).Controller.Show<FusionMenuUIMain>();
        }
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }
}