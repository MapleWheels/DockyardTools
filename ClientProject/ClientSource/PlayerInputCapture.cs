using Barotrauma.Networking;
using Microsoft.Xna.Framework.Input;
using ModdingToolkit.Config;
using ModdingToolkit.Networking;

namespace DockyardTools;

public partial class PlayerInputCapture : IClientSerializable, IServerSerializable
{
    private void UpdatePlayerInput()
    {
        bool xHit = false, yHit = false;
        if (DockyardToolsPlugin.ControlForwardX.IsHit())
        {
            xHit = true;
            _thrustVec.X = Math.Min(_thrustVec.X + DockyardToolsPlugin.ControlSensitivity.Value, 100f);
        }

        if (DockyardToolsPlugin.ControlReverseX.IsHit())
        {
            xHit = true;
            _thrustVec.X = Math.Max(-100f, _thrustVec.X - DockyardToolsPlugin.ControlSensitivity.Value);
        }
        
        if (DockyardToolsPlugin.ControlUpY.IsHit())
        {
            yHit = true;
            _thrustVec.Y = Math.Min(_thrustVec.Y + DockyardToolsPlugin.ControlSensitivity.Value, 100f);
        }

        if (DockyardToolsPlugin.ControlDownY.IsHit())
        {
            yHit = true;
            _thrustVec.Y = Math.Max(-100f, _thrustVec.Y - DockyardToolsPlugin.ControlSensitivity.Value);
        }

        // decay
        if (!xHit)
        {
            // above 0
            if (_thrustVec.X > float.Epsilon)
            {
                // going under 0, through dead zone
                if (_thrustVec.X - DockyardToolsPlugin.ControlSensitivity.Value < OutputDeadzone)
                {
                    _thrustVec.X = 0f;
                }
                else
                {
                    _thrustVec.X -= DockyardToolsPlugin.ControlSensitivity.Value;
                }
            }
            // under 0
            else if (_thrustVec.X < -float.Epsilon)
            {
                // going over 0, through dead zone
                if (_thrustVec.X + DockyardToolsPlugin.ControlSensitivity.Value > -OutputDeadzone)
                {
                    _thrustVec.X = 0f;
                }
                else
                {
                    _thrustVec.X += DockyardToolsPlugin.ControlSensitivity.Value;
                }
            }
            else
            {
                _thrustVec.X = +0f;
            }
        }
        
        // decay
        if (!yHit)
        {
            // above 0
            if (_thrustVec.Y > float.Epsilon)
            {
                // going under 0, through dead zone
                if (_thrustVec.Y - DockyardToolsPlugin.ControlSensitivity.Value < OutputDeadzone)
                {
                    _thrustVec.Y = 0f;
                }
                else
                {
                    _thrustVec.Y -= DockyardToolsPlugin.ControlSensitivity.Value;
                }
            }
            // under 0
            else if (_thrustVec.Y < -float.Epsilon)
            {
                // going over 0, through dead zone
                if (_thrustVec.Y + DockyardToolsPlugin.ControlSensitivity.Value > -OutputDeadzone)
                {
                    _thrustVec.Y = 0f;
                }
                else
                {
                    _thrustVec.Y += DockyardToolsPlugin.ControlSensitivity.Value;
                }
            }
            else
            {
                _thrustVec.Y = +0f;
            }
        }
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