using Barotrauma.Networking;
using Microsoft.Xna.Framework.Input;
using ModdingToolkit.Config;
using ModdingToolkit.Networking;

namespace DockyardTools;

public partial class PlayerInputCapture : IClientSerializable, IServerSerializable
{
    private void UpdatePlayerInput()
    {
        float deltaX = 0f, deltaY = 0f;
        if (DockyardToolsPlugin.ControlForwardX.Value?.IsDown() ?? false)
        {
            deltaX += DockyardToolsPlugin.ControlSensitivity.Value;
        }

        if (DockyardToolsPlugin.ControlReverseX.Value?.IsDown() ?? false)
        {
            deltaX -= DockyardToolsPlugin.ControlSensitivity.Value;
        }
        
        if (DockyardToolsPlugin.ControlUpY.Value?.IsDown() ?? false)
        {
            deltaY -= DockyardToolsPlugin.ControlSensitivity.Value;   // y is inverted
        }

        if (DockyardToolsPlugin.ControlDownY.Value?.IsDown() ?? false)
        {
            deltaY += DockyardToolsPlugin.ControlSensitivity.Value;   // y is inverted
        }

        _thrustVec.X = Math.Clamp(_thrustVec.X + deltaX, -100f, 100f);
        _thrustVec.Y = Math.Clamp(_thrustVec.Y + deltaY, -100f, 100f);

        if (Math.Abs(deltaX) < float.Epsilon)
        {
            _thrustVec.X = 0f;
        }

        if (Math.Abs(deltaY) < float.Epsilon)
        {
            _thrustVec.Y = 0f;
        }

        if (DockyardToolsPlugin.ControlToggleDocking.IsHit())
            _dockingSignal = true;
    }
    
    
    public void ClientEventWrite(IWriteMessage msg, NetEntityEvent.IData extraData = null)
    {
        _networkHelper.WriteData(msg);
    }

    public void ClientEventRead(IReadMessage msg, float sendingTime)
    {
        _networkHelper.ReadData(msg);
    }
}