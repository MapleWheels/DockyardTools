using Barotrauma.Items.Components;
using Microsoft.Xna.Framework.Graphics;

namespace DockyardTools
{
  public partial class FighterHUD : ItemComponent, IDataBusSubscriber
  {
    private const string S_CURRENT_VELX_IN = "CurrentVelX";
    private const string S_CURRENT_VELY_IN = "CurrentVelY";
    private const string S_CURRENT_POSX_IN = "CurrentPosX";
    private const string S_CURRENT_POSY_IN = "CurrentPosY";
    private const string S_AMMO1_IN = "Ammunition1In";
    private const string S_AMMO2_IN = "Ammunition2In";
    private const string S_HULLHP_IN = "HullHpIn";
    
    private const string SBC_MESSAGE_BATTPOWERAUX = "BattPowerAux";
    private const string SBC_MESSAGE_BATTPOWERAUXACTIVE = "BattPowerAuxActive";
    private const string SBC_MESSAGE_BATTPOWERMAIN = "BattPowerMain";
    private const string SBC_MESSAGE_ENGINEINTEGRITY = "EngineIntegrity";
    private const string SBC_MESSAGE_FIREALARMGENERAL = "FireAlarmGeneral";
    private const string SBC_MESSAGE_POWERSYSTEMINTEGRITY = "PowerSystemIntegrity";
    

    private Vector2 _currentPosition, _currentVelocity;
    private float _ammo1Percent, _ammo2Percent, _hullHpPercent, _battPowerMainPercent, _battPowerAuxPercent, _powerSystemIntegrityPercent, _engineIntegrityPercent;
    private bool _battPowerAuxActive, _fireAlarmGeneralActive;
    private SignalBusController? _signalBusController;
    private Controller _controller;
    
    public override void ReceiveSignal(Signal signal, Connection connection)
    {
      base.ReceiveSignal(signal, connection);
      if (!float.TryParse(signal.value.ToString(), out var val))
      {
        return;
      }
      
      switch (connection.Name)
      {
        case S_CURRENT_VELX_IN: _currentVelocity = new Vector2(val, _currentVelocity.Y); break;
        case S_CURRENT_VELY_IN: _currentVelocity = new Vector2(_currentVelocity.X, val); break;
        case S_CURRENT_POSX_IN: _currentPosition = new Vector2(val, _currentPosition.Y); break;
        case S_CURRENT_POSY_IN: _currentPosition = new Vector2(_currentPosition.X, val); break;
        case S_AMMO1_IN: _ammo1Percent = Math.Clamp(val, 0f, 100f); break;
        case S_AMMO2_IN: _ammo2Percent = Math.Clamp(val, 0f, 100f); break;
        case S_HULLHP_IN: _hullHpPercent = Math.Clamp(val, 0f, 100f); break;
      }
    }

    public override void OnItemLoaded()
    {
      base.OnItemLoaded();
      
      if (Item.GetComponent<Controller>() is { } controller)
      {
        _controller = controller;
        LuaCsSetup.Instance.Logger.Log($"{nameof(OnItemLoaded)}: Controller found: {controller.Name}");
      }
      
      if (Item.GetComponents<SignalBusController>().FirstOrDefault(sbc => sbc?.SignalBusInName == "DataBusIn", null) is
          { } busController)
      {
        _signalBusController = busController;
        _signalBusController.Subscribe(SBC_MESSAGE_BATTPOWERAUX, this);
        _signalBusController.Subscribe(SBC_MESSAGE_BATTPOWERAUXACTIVE, this);
        _signalBusController.Subscribe(SBC_MESSAGE_BATTPOWERMAIN, this);
        _signalBusController.Subscribe(SBC_MESSAGE_ENGINEINTEGRITY, this);
        _signalBusController.Subscribe(SBC_MESSAGE_FIREALARMGENERAL, this);
        _signalBusController.Subscribe(SBC_MESSAGE_POWERSYSTEMINTEGRITY, this);
      }
      
#if CLIENT
      OnItemLoadedClient();
#endif
    }

    public void OnSignalReceived(Signal source, Connection connection, string messageId, object? data)
    {
      if (data is float val)
      {
        switch (messageId)
        {
          case SBC_MESSAGE_BATTPOWERAUX: _battPowerAuxPercent = val; break;
          case SBC_MESSAGE_BATTPOWERMAIN: _battPowerMainPercent = val; break;
          case SBC_MESSAGE_ENGINEINTEGRITY: _engineIntegrityPercent = val; break;
          case SBC_MESSAGE_POWERSYSTEMINTEGRITY: _powerSystemIntegrityPercent = val; break;
        }
      }
      else if (data is bool flag)
      {
        switch (messageId)
        {
          case SBC_MESSAGE_BATTPOWERAUXACTIVE: _battPowerAuxActive = flag; break;
          case SBC_MESSAGE_FIREALARMGENERAL: _fireAlarmGeneralActive = flag; break;
        }
      }
    }
  }
}