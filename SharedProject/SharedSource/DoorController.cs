using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

public partial class DoorController : ItemComponent
{
    #region SYMBOLDEF

    public const string S_SMOKE = "in_smoke";
    public const string S_WATER = "in_waterpercent";
    public const string S_MONSTER = "in_monster";
    public const string S_MOTION = "in_motion";
    public const string S_SETSTATE = "in_setstate";
    public const string S_LOCKSTATE = "in_lockstate";
    public const string S_DOORSTATE = "out_isopen";

    #endregion

    #region SERIALVARDEF

    [Editable(1f, 99f), Serialize(2f, IsPropertySaveable.Yes, "% water to trigger.")]
    public float WaterThreshold { get; set; }
    
    [InGameEditable, Serialize(true, IsPropertySaveable.Yes, "Use motion detection?")]
    public bool UseMotionDetection { get; set; }

    #endregion
    
    public DoorController(Item item, ContentXElement element) : base(item, element)
    {
    }

    // vars
    public bool IsOpen { get; set; }
    
    private bool _smokeDetected = false,
        _waterDetected = false,
        _monsterDetected = false,
        _motionDetected = false,
        _lockState = false,
        _forcedStateValue = false;

    public override void OnMapLoaded()
    {
        base.OnMapLoaded();
        ProcessInputStates();
    }

    private void ProcessInputStates()
    {
        if (_lockState)
        {
            IsOpen = _forcedStateValue;
        }
        else
        {
            IsOpen = !(_smokeDetected | _waterDetected | _monsterDetected) 
                          && (!UseMotionDetection || _motionDetected);
        }
        item.SendSignal(IsOpen ? "1" : "0", S_DOORSTATE);
        SendNetworkEvent();
    }

    public override void ReceiveSignal(Signal signal, Connection connection)
    {
        base.ReceiveSignal(signal, connection);
        switch (connection.Name)
        {
            case S_SMOKE:
                _smokeDetected = Utils.TryGetBool(signal.value);
                break;
            case S_WATER:
                float x = Utils.TryGetFloat(signal.value);
                _waterDetected = x > WaterThreshold;
                break;
            case S_MONSTER:
                _monsterDetected = Utils.TryGetBool(signal.value);
                break;
            case S_MOTION:
                _motionDetected = Utils.TryGetBool(signal.value);
                break;
            case S_SETSTATE:
                _forcedStateValue = Utils.TryGetBool(signal.value);
                break;
            case S_LOCKSTATE:
                _lockState = Utils.TryGetBool(signal.value);
                break;
        }
        ProcessInputStates();
    }
    
    
}