using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

public partial class PlayerInputCapture : ItemComponent
{
    #region SYMBOLDEF

    public const string S_VELX_OUT = "VelXOut";
    public const string S_VELY_OUT = "VelYOut";
    public const string S_DOCKCMD_OUT = "DockSignalOut";
    public const string S_EXTRAATTACK_OUT = "ExtraAttackOut";
    public const string S_UTILITY_OUT = "UtilityOut";

    #endregion
    
    #region VARDEF

    private readonly SimpleSynchroHelper<PlayerInputCapture> _networkHelper;
    private Vector2 _thrustVec;
    private bool _dockingSignalNetworked, _extraAttackSignalNetworked, _utilitySignalNetworked;
    private bool _dockingSignalLocal, _extraAttackSignalLocal, _utilitySignalLocal;
    private bool _isAuthority;
    public ref readonly Vector2 ThrustVec => ref _thrustVec;
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
        SendFlagSignalConditional(ref _dockingSignalLocal, S_DOCKCMD_OUT);
        SendFlagSignalConditional(ref _extraAttackSignalLocal, S_EXTRAATTACK_OUT);
        SendFlagSignalConditional(ref _utilitySignalLocal, S_UTILITY_OUT);

        void SendFlagSignalConditional(ref bool flag, string connectionName)
        {
            if (flag)
            {
                item.SendSignal("1", connectionName);
            }
            flag = false;
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
            msg.ReadBoolean(); // docksignal
            msg.ReadBoolean(); // extraattack
            msg.ReadBoolean(); // utility
            return;
        }
        
        _thrustVec.X = msg.ReadRangedSingle(-100, 100, 12);
        _thrustVec.Y = msg.ReadRangedSingle(-100, 100, 12);
        bool dockSignal = msg.ReadBoolean();
        bool extraAttackSignal = msg.ReadBoolean();
        bool utilitySignal = msg.ReadBoolean();
        
        SetFlagSynchro(dockSignal, ref _dockingSignalLocal, ref _dockingSignalNetworked);
        SetFlagSynchro(extraAttackSignal, ref _extraAttackSignalLocal, ref _extraAttackSignalNetworked);
        SetFlagSynchro(utilitySignal, ref _utilitySignalLocal, ref _utilitySignalNetworked);

        void SetFlagSynchro(bool conditional, ref bool local, ref bool networked)
        {
            if (conditional)
            {
#if SERVER
                networked = true;
#endif
                local = true;
            }
        }
    }

    private void WriteEventData(IWriteMessage msg)
    {
        msg.WriteRangedSingle(_thrustVec.X, -100, 100, 12);
        msg.WriteRangedSingle(_thrustVec.Y, -100, 100, 12);
        
        WriteSynchroFlag(ref _dockingSignalNetworked);
        WriteSynchroFlag(ref _extraAttackSignalNetworked);
        WriteSynchroFlag(ref _utilitySignalNetworked);

        void WriteSynchroFlag(ref bool networkFlag)
        {
            msg.WriteBoolean(networkFlag);
            networkFlag = false;
        }
    }
}