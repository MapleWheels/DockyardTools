using System.Globalization;
using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

/// <summary>
/// Electrical System Control Unit
/// </summary>
public class ESCU : Powered, IClientSerializable, IServerSerializable
{
    #region SYMBOL_&CONST_DEF

    // ReSharper disable InconsistentNaming
    // input
    private const string S_SETACTIVE = "set_active";
    private const string S_GRIDLOAD = "current_load";
    private const string S_DOCKPOWEROVERRIDE = "dockpoweroverride";
    private const string S_LOWPOWERONLY = "lowpowerreactorsonly";
    private const string S_REACTORPOWEROFF = "reactorpoweroff";
    // output
    private const string S_RELAYLOW_OUT = "relaylowpowerout";
    private const string S_RELAYHIGH_OUT = "relayhighpowerout";
    private const string S_RELAYSTANDBY_OUT = "relaystandbypowerout";
    private const string S_RELAYDOCK_OUT = "relaydockpowerout";
    private const string S_STANDBYREMAINING_OUT = "standbypowerout";
    private const string S_STANDBYPERCENT_OUT = "standbypowerpercentout";
    // tags
    private const string TAG_REACTOR_BUFFER_DEVICE = "psource_buffer";
    private const string TAG_STANDBYPOWER_DEVICE = "psource_standby";
    private const string TAG_REACTOR_LOW = "psource_reactorlow";
    private const string TAG_REACTOR_HIGH = "psource_reactorhigh";
    private const string TAG_DOCKPOWERRELAY = "psource_dock";
    // ReSharper restore InconsistentNaming

    #endregion

    #region VARDEF

    // -- Config vardef
    
    [InGameEditable, Serialize(true, IsPropertySaveable.Yes, description: "IsEnabled")]
    public bool IsOn { get; set; }
    
    [Editable, Serialize(true, IsPropertySaveable.Yes, "If a dock-power relay is connected")]
    public bool UseDockPowerRelay { get; set; }
    
    [Editable, Serialize(true, IsPropertySaveable.Yes, "Use reactor control?")]
    public bool UseReactorControl { get; set; }
    
    [Editable, Serialize(true, IsPropertySaveable.Yes, 
         "Use a high/low split power regime for reactor control?")]
    public bool UseHighLowPowerRegimes { get; set; }
    
    [Editable, Serialize(true, IsPropertySaveable.Yes, description: "Only control reactors when Auto Reactor is enabled?")]
    public bool OnlyControlReactorsInAuto { get; set; }
    
    [Editable(1f,1.9f), Serialize(1.1f, IsPropertySaveable.Yes, description: "Multiplier: How much power to produce for the measured load.")]
    public float PowerToLoadScale { get; set; }
    
    [Editable(1,10), Serialize(4, IsPropertySaveable.Yes, description: "Ticks between network updates")]
    public int TicksBetweenNetUpdate { get; set; }

    // -- Working vardef
    
    private ImmutableList<PowerContainer> _bufferDevices = ImmutableList<PowerContainer>.Empty;
    private ImmutableList<PowerContainer> _standbyDevices = ImmutableList<PowerContainer>.Empty;
    private ImmutableList<Reactor> _reactorsLowPower = ImmutableList<Reactor>.Empty;
    private ImmutableList<Reactor> _reactorsHighPower = ImmutableList<Reactor>.Empty;
    private ImmutableList<RelayComponent> _dockPowerRelays = ImmutableList<RelayComponent>.Empty;
    private readonly SimpleSynchroHelper<ESCU> _networkHelper;

    private bool _relayLowPowerOn = false,
        _relayHighPowerOn = false,
        _relayDockPowerOn = false,
        _relayStandbyPowerOn = false,
        _dockPowerOnlyOverride = false,
        _lowPowerReactorsOnly = false,
        _reactorPowerOff = false;

    private ImmutableList<float> _reactorsLowMaxPowers = ImmutableList<float>.Empty;
    private float _reactorsLowMaxCombinedPowers, _reactorsHighMaxCombinedPowers, _currentLoad, _standbyPowerCurrent, _standbyPowerMax, _standbyPowerPercent;
    private ImmutableList<float> _reactorsHighMaxPowers = ImmutableList<float>.Empty;

    #endregion
    
    public ESCU(Item item, ContentXElement element) : base(item, element)
    {
        IsActive = true;
        _networkHelper = new(this, ReadEventData, WriteEventData);
        _networkHelper.TicksBetweenUpdates = this.TicksBetweenNetUpdate;
    }

    public override void OnMapLoaded()
    {
        base.OnMapLoaded();
        var ptDevices = item.linkedTo
            .Where(me => me is Item it
                         && it.GetComponent<PowerContainer>() is { } pt)
            .Select(me => ((Item)me).GetComponent<PowerContainer>())
            .ToImmutableList();

        _bufferDevices = ptDevices
            .Where(pt => pt.item.HasTag(TAG_REACTOR_BUFFER_DEVICE))
            .ToImmutableList();

        foreach (PowerContainer device in _bufferDevices)
        {
            device.ExponentialRechargeSpeed = false;
        }

        _standbyPowerMax = 0f;
        
        _standbyDevices = ptDevices
            .Where(pt =>
            {
                if (!pt.item.HasTag(TAG_STANDBYPOWER_DEVICE)) 
                    return false;
                _standbyPowerMax += pt.Capacity;
                return true;

            })
            .ToImmutableList();

        
        var reactorDevices = item.linkedTo
            .Where(me => me is Item it
                         && it.GetComponent<Reactor>() is { } r)
            .Select(me => ((Item)me).GetComponent<Reactor>())
            .ToImmutableList();
        
        _reactorsLowPower = reactorDevices
            .Where(r => r.item.HasTag(TAG_REACTOR_LOW))
            .ToImmutableList();
        
        _reactorsHighPower = reactorDevices
            .Where(r => r.item.HasTag(TAG_REACTOR_HIGH))
            .ToImmutableList();

        _reactorsLowMaxCombinedPowers = 0;
        _reactorsHighMaxCombinedPowers = 0;
        _currentLoad = 0;

        _reactorsLowMaxPowers = _reactorsLowPower.Select(r =>
        {
            // turn on and sync
            r.IsActive = true;
            r.PowerOn = true;
            r.unsentChanges = true;
            _reactorsLowMaxCombinedPowers += r.MaxPowerOutput;
            return r.MaxPowerOutput;
        }).ToImmutableList();
        
        _reactorsHighMaxPowers = _reactorsHighPower.Select(r =>
        {
            // turn on and sync
            r.IsActive = true;
            r.PowerOn = true;
            r.unsentChanges = true;
            _reactorsHighMaxCombinedPowers += r.MaxPowerOutput;
            return r.MaxPowerOutput;
        }).ToImmutableList();

        _dockPowerRelays = item.linkedTo
            .Where(me => me is Item it
                         && it.GetComponent<RelayComponent>() is { } r
                         && it.HasTag(TAG_DOCKPOWERRELAY))
            .Select(me => ((Item)me).GetComponent<RelayComponent>())
            .ToImmutableList();
    }

    #region OVERRIDES_&PUBLIC

    public override void Update(float deltaTime, Camera cam)
    {
        if (!IsActive || !IsOn)
        {
            return;
        }
        
        UpdatePowerContainers();
        UpdateReactorOutput();
        SendUpdateSignals();
        _networkHelper.DelayedNetworkUpdate();
    }

    public override void ReceiveSignal(Signal signal, Connection connection)
    {
        base.ReceiveSignal(signal, connection);
        if (connection.IsPower)
            return;
        
        if (connection.LowerInvariantNameIs(S_SETACTIVE))
        {
            if (bool.TryParse(signal.value, out bool r1))
            {
                this.IsOn = r1;
            }
            else if (float.TryParse(signal.value, out float r2))
            {
                this.IsOn = Math.Abs(r2) < float.Epsilon || float.PositiveInfinity - Math.Abs(r2) < float.Epsilon;
            }
            _networkHelper.NetworkUpdateReady();
            return;
        }

        if (connection.LowerInvariantNameIs(S_GRIDLOAD))
        {
            if (float.TryParse(signal.value, out float r1))
            {
                _currentLoad = MathF.Max(-float.Epsilon, r1);
            }

            return;
        }

        if (connection.LowerInvariantNameIs(S_DOCKPOWEROVERRIDE))
        {
            _dockPowerOnlyOverride = Utils.TrySetBool(signal.value);
            _networkHelper.NetworkUpdateReady();
            return;
        }
        
        if (connection.LowerInvariantNameIs(S_LOWPOWERONLY))
        {
            _lowPowerReactorsOnly = Utils.TrySetBool(signal.value);
            _networkHelper.NetworkUpdateReady();
            return;
        }

        if (connection.LowerInvariantNameIs(S_REACTORPOWEROFF))
        {
            _reactorPowerOff = Utils.TrySetBool(signal.value);
            _networkHelper.NetworkUpdateReady();
            return;
        }
    }

    #endregion


    #region INTERNAL_FUNCS

    private void SendUpdateSignals()
    {
        item.SendSignal(_relayHighPowerOn.ToString(CultureInfo.InvariantCulture), S_RELAYHIGH_OUT);
        item.SendSignal(_relayLowPowerOn.ToString(CultureInfo.InvariantCulture), S_RELAYLOW_OUT);
        item.SendSignal(_relayStandbyPowerOn.ToString(CultureInfo.InvariantCulture), S_RELAYSTANDBY_OUT);
        item.SendSignal(_relayDockPowerOn.ToString(CultureInfo.InvariantCulture), S_RELAYDOCK_OUT);
        item.SendSignal(_standbyPowerCurrent.FormatToDecimalPlace(0), S_STANDBYREMAINING_OUT);
        item.SendSignal(_standbyPowerPercent.FormatToDecimalPlace(1), S_STANDBYPERCENT_OUT);
    }

    private void UpdatePowerContainers()
    {
        foreach (PowerContainer device in _bufferDevices)
        {
            if (device.powerOut is not null && device.powerOut.Grid is not null)
            {
                float rechargeSpeed = Math.Clamp(device.MaxRechargeSpeed * (1f - device.Charge / device.Capacity), 0f, device.MaxRechargeSpeed);
                device.RechargeSpeed = rechargeSpeed;
                device.Efficiency = 1.0f;
#if CLIENT
                device.rechargeSpeedSlider.BarScroll = rechargeSpeed / device.MaxRechargeSpeed;
#endif
            }
        }

        _standbyPowerCurrent = 0f;
        foreach (PowerContainer container in _standbyDevices)
        {
            _standbyPowerCurrent += container.Charge;
        }

        _standbyPowerPercent = _standbyPowerCurrent / _standbyPowerMax * 100f;
    }

    private void UpdateReactorOutput()
    {
       // configure what devices we need
        _relayDockPowerOn = false;
        _relayLowPowerOn = false;
        _relayHighPowerOn = false;
        _relayStandbyPowerOn = false;
        
        
        if (_dockPowerRelays.Any() && UseDockPowerRelay)
        {
            // check for dock/shore power
            foreach (RelayComponent relay in _dockPowerRelays)
            {
                if (relay.newVoltage is not null && relay.newVoltage > 0)
                {
                    _relayDockPowerOn = true;
                    break;
                }
            }
        }

        if (!_relayDockPowerOn || _dockPowerOnlyOverride)
        {
            _relayLowPowerOn = true;
            float expectedPower = _currentLoad * PowerToLoadScale * (_reactorPowerOff ? 0f : 1f);
            
            
            if (!UseHighLowPowerRegimes)
            {
                SetReactorsToAvg(_reactorsLowPower.Concat(_reactorsLowPower), expectedPower, _reactorsLowMaxCombinedPowers + _reactorsHighMaxCombinedPowers);
            }
            else
            {
                // try to use low power first, then high power
                float lowPowerLoad = Math.Clamp(expectedPower, 0f, _reactorsLowMaxCombinedPowers);
                float highPowerLoad = 0;
                if (!_lowPowerReactorsOnly)
                {
                    highPowerLoad = Math.Clamp(expectedPower - lowPowerLoad, 0f,
                        _reactorsHighMaxCombinedPowers);
                    _relayHighPowerOn = true;
                }

                SetReactorsToAvg(_reactorsLowPower, lowPowerLoad, _reactorsLowMaxCombinedPowers);
                SetReactorsToAvg(_reactorsHighPower, highPowerLoad, _reactorsHighMaxCombinedPowers);
            }
        }
        
        void SetReactorsToAvg(IEnumerable<Reactor> reactors, float currentLoad, float maxLoad)
        {
            float turbinePercent = Math.Clamp(currentLoad / maxLoad * 100f, 0f, 100f);
            
            foreach (Reactor reactor in reactors)
            {
                if (OnlyControlReactorsInAuto && !reactor.AutoTemp)
                    continue;

                if (!reactor.PowerOn || reactor.AvailableFuel < float.Epsilon)
                {
                    reactor.TargetTurbineOutput = 0f;
                    reactor.TargetFissionRate = 0f;
                }
                else
                {
                    reactor.TargetTurbineOutput = turbinePercent;
                    reactor.TargetFissionRate =  75f / reactor.AvailableFuel * turbinePercent;
                }
                
                reactor.unsentChanges = true;
            }
        }
        
        _networkHelper.NetworkUpdateReady();
    }

    #endregion

    
    private void WriteEventData(IWriteMessage msg)
    {
        msg.WriteBoolean(IsOn);
        msg.WriteBoolean(_reactorPowerOff);
        msg.WriteBoolean(_dockPowerOnlyOverride);
        msg.WriteBoolean(_lowPowerReactorsOnly);
    }
    
    private void ReadEventData(IReadMessage msg)
    {
        IsOn = msg.ReadBoolean();
        _reactorPowerOff = msg.ReadBoolean();
        _dockPowerOnlyOverride = msg.ReadBoolean();
        _lowPowerReactorsOnly = msg.ReadBoolean();
    }
    
    public void ClientEventWrite(IWriteMessage msg, NetEntityEvent.IData extraData = null)
    {
        _networkHelper.WriteData(msg);
    }

    public void ClientEventRead(IReadMessage msg, float sendingTime)
    {
        _networkHelper.ReadData(msg);
    }

    public void ServerEventRead(IReadMessage msg, Client c)
    {
        _networkHelper.ReadData(msg);
        _networkHelper.SendUpdateNextTick();
    }

    public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
    {
        _networkHelper.WriteData(msg);
    }
}