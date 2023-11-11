using Barotrauma.Items.Components;
using Barotrauma.Networking;
using ModdingToolkit.Config;
using ModdingToolkit.Networking;

namespace DockyardTools;

public partial class PlayerInputCapture : ItemComponent
{
    #region SYMBOLDEF

    public const string S_VELX_OUT = "VelXOut";
    public const string S_VELY_OUT = "VelYOut";
    public const string S_DOCKCMD_OUT = "DockSignalOut";

    #endregion
    
    #region VARDEF

    private readonly SimpleSynchroHelper<PlayerInputCapture> _networkHelper;
    private Vector2 _thrustVec;
    private bool _dockingSignalNetworked;
    private bool _dockingSignalLocal;
    private bool _isAuthority;
    public ref readonly Vector2 ThrustVec => ref _thrustVec;
    private int _ticksUntilWiringUpdate = 0;
    private ImmutableList<Controller> _linkedControllers = ImmutableList<Controller>.Empty;

    #endregion

    public PlayerInputCapture(Item item, ContentXElement element) : base(item, element)
    {
        _networkHelper = new(this, ReadEventData, WriteEventData);
        isActive = true;
        _isAuthority = false;
    }

    public override void OnMapLoaded()
    {
        base.OnMapLoaded();
        _networkHelper.TicksBetweenUpdates = 1;
        _linkedControllers = item.linkedTo
            .Where(me => me is Item { } it
                         && it.GetComponent<Controller>() is not null)
            .Select(me => ((Item)me).GetComponent<Controller>())
            .ToImmutableList();
    }

    public override void Update(float deltaTime, Camera cam)
    {
#if CLIENT
        bool controllerFound = false;
        bool anyControllerFound = false;
        foreach (Controller controller in _linkedControllers)
        {
            if (controller.User is not null && Character.Controlled is not null && controller.User == Character.Controlled)
            {
                _isAuthority = true;
                controllerFound = true;
                UpdatePlayerInput();
                _networkHelper.NetworkUpdateReady();
            }
            if (controller.User is not null)
            {
                anyControllerFound = true;
            }
        }
        // reset outputs if no one is operating the controller
        if (!controllerFound)
        {
            _isAuthority = false;
            _thrustVec = Vector2.Zero;
            _dockingSignalNetworked = false;
            // Someone is controlling this drone but it's not us so we shouldn't override their inputs to the server.
            if (!anyControllerFound)
            {
                _networkHelper.NetworkUpdateReady();
            }
        }
        
        _networkHelper.ImmediateNetworkUpdate();   
#endif
        UpdateWiring();
    }

    private void UpdateWiring()
    {
        item.SendSignal(_thrustVec.X.FormatToDecimalPlace(1), S_VELX_OUT);
        item.SendSignal(_thrustVec.Y.FormatToDecimalPlace(1), S_VELY_OUT);
        if (_dockingSignalLocal)
        {
            item.SendSignal("1", S_DOCKCMD_OUT);
            _dockingSignalLocal = false;
        }
    }

    private void ReadEventData(IReadMessage msg)
    {
        if (_isAuthority)
        {
            // read to blank because we are in control and don't want lerp-ness caused by server overwriting newer inputs
            // this could cause desync but the other option is worse.
            msg.ReadRangedSingle(-100, 100, 12);
            msg.ReadRangedSingle(-100, 100, 12);
            msg.ReadBoolean();
            return;
        }
        
        _thrustVec.X = msg.ReadRangedSingle(-100, 100, 12);
        _thrustVec.Y = msg.ReadRangedSingle(-100, 100, 12);
        bool dockS = msg.ReadBoolean();
        if (dockS)
        {
#if SERVER
            _dockingSignalNetworked = true;  // tracked for sending to other clients
#endif
            _dockingSignalLocal = true;
        }    
    }

    private void WriteEventData(IWriteMessage msg)
    {
        msg.WriteRangedSingle(_thrustVec.X, -100, 100, 12);
        msg.WriteRangedSingle(_thrustVec.Y, -100, 100, 12);
        msg.WriteBoolean(_dockingSignalNetworked);  
        _dockingSignalNetworked = false; // signal sent
    }
}