using System.Globalization;
using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

/// <summary>
/// Engine Trim Control Unit
/// </summary>
public partial class ETCU : Powered, IClientSerializable, IServerSerializable
{
    #region SYMBOL_&CONST_DEF

    // ReSharper disable InconsistentNaming
    
    public const string S_CMDVELX = "CmdVelX";
    public const string S_CMDVELY = "CmdVelY";
    public const string S_CURVELX = "CurVelX";
    public const string S_CURVELY = "CurVelY";
    public const string S_SETACTIVEOUTPUTSCALE = "setactiveoutputscale";
    public const string S_VELXOUT = "VelXOut";
    public const string S_VELYOUT = "VelYOut";
    public const string S_TRIMTANKLEVELOUT = "TrimPumpLevelOut";

    public const string S_TRIMTANKHULLNAME = "trimtank";

    public const float BUOYANCYRATIO = SubmarineBody.NeutralBallastPercentage;
    private const int DECIMALPLACES = 2;
    
    // ReSharper restore InconsistentNaming

    #endregion
    
    
    #region VARDEF

    [Editable, Serialize(true, IsPropertySaveable.Yes, description: "Use Engine Control Unit?")]
    public bool UseEngineControlUnit { get; set; }
    
    // horizontal speed
    [InGameEditable, Editable(1f,10000f), Serialize(5f, IsPropertySaveable.Yes, description: "The maximum horizontal speed of the ship.")]
    public float MaxShipSpeedX { get; set; }

    // vertical speed
    [InGameEditable, Editable(0f, 10000f), Serialize(5f, IsPropertySaveable.Yes, description: "The maximum vertical speed of the ship.")]
    public float MaxShipSpeedY { get; set; }
    
    
    
    // multiplier adjusts
    [Editable(0,100f), Serialize(5f, IsPropertySaveable.Yes, description: "The range in which the multiplier scale adjust doesn't apply. Used to reduce oscillations from nav computer.")]
    public float MultiScaleCutoff { get; set; }
    
    [InGameEditable, Serialize(1f, IsPropertySaveable.Yes, description: "Multiplier scale adjust. This adjusts the sensitivity of the smart engine power management.")]
    public float DiffMultiScale { get; set; }
    
    // trim system 
    [Editable, Serialize(true, IsPropertySaveable.Yes, description: "Should the trim ballast tank system be used? Note: trim tank hulls MUST use the tag 'trimtank'.")]
    public bool UseTrimTankLevel { get; set; }
    
    
    [Editable, Serialize(3, IsPropertySaveable.Yes, description: "Ticks between updating system. For performance.")]
    public int WaitTicksBetweenUpdate { get; set; }
    
    
    [Editable(1f,10f), Serialize(5, IsPropertySaveable.Yes, description: "Ticks between sending network data. For performance.")]
    public int WaitTicksBetweenNetworkUpdate { get; set; }
    
    
    [InGameEditable, Serialize(false, IsPropertySaveable.Yes, description: "Scale output?")]
    public bool ScaleOutput { get; set; }
    
    [Editable(0f, 1f), Serialize(0.4f, IsPropertySaveable.Yes, description: "Scale amount when active.")]
    public float OutputScaleRatio { get; set; }
    
    // props
    public float CmdVelocityX { get; set; }
    public float CmdVelocityY { get; set; }
    public float CurrVelX { get; set; }
    public float CurrVelY { get; set; }
    public float VelOutX { get; set; }
    public float VelOutY { get; set; }
    public float BuoyancyPercentageOut { get; set; }
    public bool ScaleOutputActive { get; set; }


    // Internals VARS
    private ImmutableList<Hull> _subHulls = ImmutableList<Hull>.Empty;
    private ImmutableList<Hull> _trimTankHulls = ImmutableList<Hull>.Empty;
    private ImmutableList<Steering> _navSteering = ImmutableList<Steering>.Empty;
    private float _totalHullVolume, _trimTankHullVolume, _buoyancyVolume;
    private int _ticksUntilNextUpdate, _ticksUntilNetworkUpdate;
    private bool _unsentChanges = false;

    #endregion
    
    
    
    public ETCU(Item item, ContentXElement element) : base(item, element)
    {
        IsActive = true;
    }


    public override void Update(float deltaTime, Camera cam)
    {
        base.Update(deltaTime, cam);
        if (!IsActive)
        {
            var s = new Signal("0");
            item.SendSignal(s, S_VELXOUT);
            item.SendSignal(s, S_VELYOUT);
            item.SendSignal(s, S_TRIMTANKLEVELOUT);
            return;
        }
        
        _ticksUntilNextUpdate--;
        if (_ticksUntilNextUpdate < 1)
        {
            _ticksUntilNextUpdate = WaitTicksBetweenUpdate;
            if (UseEngineControlUnit)
                UpdateEngineControls();
            if (UseTrimTankLevel)
                UpdateTrimTank();
        }

        if (_unsentChanges)
        {
            _ticksUntilNetworkUpdate--;
            if (_ticksUntilNetworkUpdate < 1)
            {
                _ticksUntilNetworkUpdate = WaitTicksBetweenNetworkUpdate;
                SendNetworkUpdateEvent();
            }
        }
    }

    public override void OnMapLoaded()
    {
        base.OnMapLoaded();

        _subHulls = Hull.HullList.Where(h => h.Submarine.Equals(item.Submarine)).ToImmutableList();
        _trimTankHulls = item.linkedTo
            .Where(me => me is Hull h 
                         && h.Submarine.Equals(item.Submarine) 
                         && !h.roomName.ToLowerInvariant().Trim().Contains(S_TRIMTANKHULLNAME))
            .Concat(_subHulls.Where(h => h.roomName.Contains(S_TRIMTANKHULLNAME)))
            .Select(me => (Hull)me).ToImmutableList();

        _navSteering = item.linkedTo
            .Where(me => me is Item it
                         && it.GetComponent<Steering>() is { }
                         && it.HasTag("navterminal"))
            .Select(me => ((Item)me).GetComponent<Steering>())
            .ToImmutableList();

        _totalHullVolume = 0f;
        foreach (Hull hull in _subHulls)
        {
            _totalHullVolume += hull.Volume;
        }

        _buoyancyVolume = _totalHullVolume * BUOYANCYRATIO;
        _trimTankHullVolume = 0f;
        foreach (Hull hull in _trimTankHulls)
        {
            _trimTankHullVolume += hull.Volume;
        }

        ScaleOutputActive = ScaleOutput;
        _ticksUntilNextUpdate = WaitTicksBetweenUpdate;
        _ticksUntilNetworkUpdate = WaitTicksBetweenNetworkUpdate;
    }

    protected virtual void UpdateEngineControls()
    {
        bool autoSteerEnabled = false;
        
        foreach (Steering steering in _navSteering)
        {
            if (steering.AutoPilot)
            {
                autoSteerEnabled = true;
                break;
            }
        }

        if (autoSteerEnabled)
        {
            VelOutX = CmdVelocityX;
            VelOutY = CmdVelocityY;
        }
        else
        {
            if (CmdVelocityX is > 95 or < -95)
                VelOutX = CmdVelocityX;
            else if (CmdVelocityX < MultiScaleCutoff && CmdVelocityX > -MultiScaleCutoff)
                VelOutX = CmdVelocityX*MaxShipSpeedX/100f - CurrVelX + CmdVelocityX;
            else
                VelOutX = (CmdVelocityX*MaxShipSpeedX/100f - CurrVelX) * DiffMultiScale + CmdVelocityX;

            if (CmdVelocityY is > 95 or < -95)
                VelOutY = CmdVelocityY;
            else if (CmdVelocityY < MultiScaleCutoff && CmdVelocityY > -MultiScaleCutoff)
                VelOutY = CmdVelocityY*MaxShipSpeedY/100f - CurrVelY + CmdVelocityY;
            else
                VelOutY = (CmdVelocityY*MaxShipSpeedY/100f - CurrVelY) * DiffMultiScale + CmdVelocityY;

            if (ScaleOutputActive)
            {
                VelOutX = VelOutX * OutputScaleRatio;
                VelOutY = VelOutY * OutputScaleRatio;
            }
        }
        
        if (GameMain.NetworkMember is { IsServer: true })
            _unsentChanges = true;

        item.SendSignal(new Signal(VelOutX.Clamp(-100, 100).FormatToDecimalPlace(DECIMALPLACES)), S_VELXOUT);
        item.SendSignal(new Signal(VelOutY.Clamp(-100, 100).FormatToDecimalPlace(DECIMALPLACES)), S_VELYOUT);
    }

    protected virtual void UpdateTrimTank()
    {
        float totalNonTrimWaterVolume = 0f;
        foreach (Hull hull in _subHulls)
        {
            if (!_trimTankHulls.Contains(hull))
                totalNonTrimWaterVolume += hull.WaterVolume;
        }

        float diff = _buoyancyVolume - totalNonTrimWaterVolume;
        BuoyancyPercentageOut = diff / _trimTankHullVolume * 100f;

        if (GameMain.NetworkMember is { IsServer: true })
            _unsentChanges = true;
        
        item.SendSignal(new Signal(BuoyancyPercentageOut.Clamp(-100f, 100f).FormatToDecimalPlace(DECIMALPLACES)), S_TRIMTANKLEVELOUT);
    }

    private void SendNetworkUpdateEvent()
    {
        _unsentChanges = false;
#if SERVER
        if (GameMain.Server is not null)
            item.CreateServerEvent(this);
#elif CLIENT
        if (GameMain.Client is not null)
            item.CreateClientEvent(this);
#endif
    }

    public override void ReceiveSignal(Signal signal, Connection connection)
    {
        switch (connection.Name)
        {
            case S_CMDVELX:
                CmdVelocityX = Utils.TryGetFloat(signal.value);
                break;
            case S_CMDVELY:
                CmdVelocityY = Utils.TryGetFloat(signal.value);
                break;
            case S_CURVELX:
                CurrVelX = Utils.TryGetFloat(signal.value);
                break;
            case S_CURVELY:
                CurrVelY = Utils.TryGetFloat(signal.value);
                break;
            case S_SETACTIVEOUTPUTSCALE:
                ScaleOutputActive = Utils.TryGetBool(signal.value);
                break;
        }
    }

    private void WriteEventData(IWriteMessage msg)
    {
        msg.WriteBoolean(ScaleOutputActive);
        msg.WriteSingle(BuoyancyPercentageOut);
        msg.WriteSingle(VelOutX);
        msg.WriteSingle(VelOutY);
    }

    private void ReadEventData(IReadMessage msg)
    {
        ScaleOutputActive = msg.ReadBoolean();
        BuoyancyPercentageOut = msg.ReadSingle();
        VelOutX = msg.ReadSingle();
        VelOutY = msg.ReadSingle();
    }

    public void ClientEventWrite(IWriteMessage msg, NetEntityEvent.IData extraData = null)
    {
        WriteEventData(msg);
    }

    public void ClientEventRead(IReadMessage msg, float sendingTime)
    {
        ReadEventData(msg);
    }

    public void ServerEventRead(IReadMessage msg, Client c)
    {
        ReadEventData(msg);
        _unsentChanges = true;  // need to send to clients
    }

    public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
    {
        WriteEventData(msg);
    }
}