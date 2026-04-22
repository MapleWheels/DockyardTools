using Barotrauma.Items.Components;
using Microsoft.Xna.Framework.Graphics;

namespace DockyardTools
{
  public partial class FighterHUD : ItemComponent
  {
    private const string S_CURRENT_VELX_IN = "CurrentVelX";
    private const string S_CURRENT_VELY_IN = "CurrentVelY";
    private const string S_CURRENT_POSX_IN = "CurrentPosX";
    private const string S_CURRENT_POSY_IN = "CurrentPosY";
    private const string S_AMMO1_IN = "Ammunition1In";
    private const string S_AMMO2_IN = "Ammunition2In";
    private const string S_HULLHP_IN = "HullHpIn";

    private Vector2 _currentPosition, _currentVelocity;
    private float _ammo1Percent, _ammo2Percent, _hullHpPercent;
    
    public FighterHUD(Item item, ContentXElement element) : base(item, element)
    {
      IsActive = true;
#if CLIENT
      CreateHUD();
#endif
    }

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
        case S_AMMO1_IN: _ammo1Percent = val; break;
        case S_AMMO2_IN: _ammo2Percent = val; break;
        case S_HULLHP_IN: _hullHpPercent = val; break;
      }
    }
  }
}