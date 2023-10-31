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
    private bool _dockingSignal;
    public ref readonly Vector2 ThrustVec => ref _thrustVec;
    private int _ticksUntilWiringUpdate = 0;
    private ImmutableList<Controller> _linkedControllers = ImmutableList<Controller>.Empty;
    private bool _zeroOutput = false;

    [Editable(1f,10f), Serialize(5, IsPropertySaveable.Yes, description: "Ticks between sending network data. For performance.")]
    public int WaitTicksBetweenNetworkUpdate { get; set; }
    
    [Editable(1,10), Serialize(3, IsPropertySaveable.Yes, description: "Ticks between wiring updates. For performance.")]
    public int WaitTicksBetweenWiringUpdate { get; set; }

    [Editable(2f, 99f), Serialize(3, IsPropertySaveable.Yes, description: "Deadzone for value output.")]
    public int OutputDeadzone { get; set; }
    
    #endregion

    public PlayerInputCapture(Item item, ContentXElement element) : base(item, element)
    {
        _networkHelper = new(this, ReadEventData, WriteEventData);
        isActive = true;
    }

    public override void OnMapLoaded()
    {
        base.OnMapLoaded();
        _networkHelper.TicksBetweenUpdates = WaitTicksBetweenNetworkUpdate;
        _linkedControllers = item.linkedTo
            .Where(me => me is Item { } it
                         && it.GetComponent<Controller>() is not null)
            .Select(me => ((Item)me).GetComponent<Controller>())
            .ToImmutableList();
    }

    public override void Update(float deltaTime, Camera cam)
    {
#if CLIENT
        foreach (Controller controller in _linkedControllers)
        {
            if (controller.User is not null && Character.Controlled is not null && controller.User == Character.Controlled)
            {
                _zeroOutput = false;
                UpdatePlayerInput();
                _networkHelper.NetworkUpdateReady();
            }
            // reset outputs if no one is operating the controller
            else if (controller.User is null && !_zeroOutput)
            {
                _thrustVec = Vector2.Zero;
                _dockingSignal = false;
                _zeroOutput = true;
                _networkHelper.NetworkUpdateReady();
            }
        }
#endif
        _ticksUntilWiringUpdate--;
        if (_ticksUntilWiringUpdate < 0)
        {
            _ticksUntilWiringUpdate = WaitTicksBetweenWiringUpdate;
            UpdateWiring();
        }
        _networkHelper.DelayedNetworkUpdate();
    }

    private void UpdateWiring()
    {
        item.SendSignal(_thrustVec.X.FormatToDecimalPlace(1), S_VELX_OUT);
        item.SendSignal(_thrustVec.Y.FormatToDecimalPlace(1), S_VELY_OUT);
        item.SendSignal(_dockingSignal ? "1" : "0", S_DOCKCMD_OUT);
        _dockingSignal = false; // it's toggle, not set. Send once.
    }

    private void ReadEventData(IReadMessage msg)
    {
        _thrustVec.X = msg.ReadRangedSingle(-100, 100, 12);
        _thrustVec.Y = msg.ReadRangedSingle(-100, 100, 12);
        _dockingSignal = msg.ReadBoolean();
    }

    private void WriteEventData(IWriteMessage msg)
    {
        msg.WriteRangedSingle(_thrustVec.X, -100, 100, 12);
        msg.WriteRangedSingle(_thrustVec.Y, -100, 100, 12);
        msg.WriteBoolean(_dockingSignal);
    }
}