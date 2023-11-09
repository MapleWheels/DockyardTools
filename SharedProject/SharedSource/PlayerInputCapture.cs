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
    public ref readonly Vector2 ThrustVec => ref _thrustVec;
    private int _ticksUntilWiringUpdate = 0;
    private ImmutableList<Controller> _linkedControllers = ImmutableList<Controller>.Empty;

    #endregion

    public PlayerInputCapture(Item item, ContentXElement element) : base(item, element)
    {
        _networkHelper = new(this, ReadEventData, WriteEventData);
        isActive = true;
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
        foreach (Controller controller in _linkedControllers)
        {
            if (controller.User is not null && Character.Controlled is not null && controller.User == Character.Controlled)
            {
                controllerFound = true;
                UpdatePlayerInput();
                _networkHelper.NetworkUpdateReady();
            }
        }
        // reset outputs if no one is operating the controller
        if (!controllerFound)
        {
            _thrustVec = Vector2.Zero;
            _dockingSignalNetworked = false;
            _networkHelper.NetworkUpdateReady();
        }
#endif
        UpdateWiring();
        _networkHelper.ImmediateNetworkUpdate();
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