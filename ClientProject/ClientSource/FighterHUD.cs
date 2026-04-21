using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace DockyardTools;

public partial class FighterHUD
{
  #region SerializedFields
  
  [Editable(0.1f, 1f, 3), Serialize(0.6f, IsPropertySaveable.Yes, description: "Width of the GUI relative to screen width.")]
  public float GuiSizeRatioX { get; set; }
  
  [Editable(0.1f, 1f, 3), Serialize(0.65f, IsPropertySaveable.Yes, description: "Height of the GUI relative to screen height.")]
  public float GuiSizeRatioY { get; set; }
  
  #endregion
  
  // -- Working vars
  private GUICustomComponent _hudDraw;
  private Vector2 _screenDrawSize;
  private (Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft) _screenDrawArea;
  private (Vector2 Top, Vector2 Bottom) _depthBar, _speedBar;
  private float _depthBarHeight, _speedBarHeight;
  
  // -- Config
  private const float GAUGE_NOTCH_LINES_WIDTH = 20f;
  private const float GAUGE_NOTCH_LINES_SEPARATION = 50f;
  private const float GAUGE_NOTCH_LINES_DEPTH_MULTI = 2.5f;
  private const float GAUGE_NOTCH_LINES_SPEED_MULTI = 2.5f;
  private readonly Vector2 GAUGE_TEXT_DISPLAY_RECT = new Vector2(80f, 30f);
  private readonly Vector2 _gaugeRelativeOffset = new Vector2(0.0f, 0.5f); 
  private readonly Vector2 _gaugeFixedOffsetDepth = new Vector2(-90f, 0f); 
  private readonly Vector2 _gaugeFixedOffsetSpeed = new Vector2(20f, 0f); 
  private readonly Color _gaugeTextColor = new Color(41,255,62, 255);
  private readonly Color _gaugeLineColor = new Color(41,255,62, 175);
  private readonly Vector2 _barLinesPadding = new Vector2(20f, 10f);
  
  private void CreateHUD()
  {
    GuiFrame.ClearChildren();
    CalculateDrawArea();

    _depthBar = (
      Top: _screenDrawArea.TopLeft + new Vector2(_barLinesPadding.X, _barLinesPadding.Y),
      Bottom: _screenDrawArea.BottomLeft + new Vector2(_barLinesPadding.X, -_barLinesPadding.Y));
    
    _speedBar = (
      Top: _screenDrawArea.TopRight + new Vector2(-_barLinesPadding.X, _barLinesPadding.Y),
      Bottom: _screenDrawArea.BottomRight + new Vector2(-_barLinesPadding.X, -_barLinesPadding.Y));

    _depthBarHeight = _depthBar.Bottom.Y - _depthBar.Top.Y;
    _speedBarHeight = _speedBar.Bottom.Y - _speedBar.Top.Y;
    
    _hudDraw = new GUICustomComponent(new RectTransform(Vector2.One, GuiFrame.RectTransform),
      onDraw: (batch, component) =>
      {
#if DEBUG
        // draw test
        GUI.DrawLine(batch, new Vector2(10f, 10f), new Vector2(GameMain.GraphicsWidth, GameMain.GraphicsHeight), Color.White, 1f);
        // debug: draw boundary box
        GUI.DrawLine(batch, _screenDrawArea.TopLeft, _screenDrawArea.TopRight, Color.Magenta, 1f);
        GUI.DrawLine(batch, _screenDrawArea.BottomLeft, _screenDrawArea.BottomRight, Color.Magenta, 1f);
#endif
        
        // --- Draw Depth Gauge
        // line 
        DrawLineWithShadow(batch, _depthBar.Top, _depthBar.Bottom, _gaugeLineColor, width: 3f);
        // top, bottom notches
        DrawLineWithShadow(batch, _depthBar.Top, _depthBar.Top + new Vector2(GAUGE_NOTCH_LINES_WIDTH, 0f), _gaugeLineColor, width: 3f);
        DrawLineWithShadow(batch, _depthBar.Bottom, _depthBar.Bottom + new Vector2(GAUGE_NOTCH_LINES_WIDTH, 0f), _gaugeLineColor, width: 3f);
        // tests: in-between

        float vHeightOffset = GAUGE_NOTCH_LINES_SEPARATION - (Math.Abs(_currentPosition.Y * GAUGE_NOTCH_LINES_DEPTH_MULTI) % GAUGE_NOTCH_LINES_SEPARATION);
        for (float vHeight = vHeightOffset; vHeight + _depthBar.Top.Y < _depthBar.Bottom.Y; vHeight += GAUGE_NOTCH_LINES_SEPARATION)
        {
          DrawNotchDepthBar(batch, vHeight);
        }
        
        // tests: depth number
        Vector2 offsetDepthTextPos = _gaugeFixedOffsetDepth + _depthBar.Top + (_depthBar.Bottom - _depthBar.Top) * new Vector2(-_gaugeRelativeOffset.X, _gaugeRelativeOffset.Y); // invert X
        GUI.DrawString(batch, offsetDepthTextPos, _currentPosition.Y.ToString("0000"), _gaugeTextColor, font: GUIStyle.DigitalFont);
        
        // --- Draw Speed Gauge
        DrawLineWithShadow(batch, _speedBar.Top, _speedBar.Bottom, _gaugeLineColor, width: 3f);
        // top, bottom notches
        DrawLineWithShadow(batch, _speedBar.Top, _speedBar.Top - new Vector2(GAUGE_NOTCH_LINES_WIDTH, 0f), _gaugeLineColor, width: 3f);
        DrawLineWithShadow(batch, _speedBar.Bottom, _speedBar.Bottom - new Vector2(GAUGE_NOTCH_LINES_WIDTH, 0f), _gaugeLineColor, width: 3f);
        // tests: in-between
        
        
        
        // tests: speed number
        Vector2 offsetSpeedtextPos = _gaugeFixedOffsetSpeed + _speedBar.Top + (_speedBar.Bottom - _speedBar.Top) * _gaugeRelativeOffset;
        float speed = (float)Math.Sqrt(Math.Pow(_currentVelocity.X, 2) + Math.Pow(_currentVelocity.Y, 2));
        GUI.DrawString(batch, offsetSpeedtextPos, speed.ToString("0000"), _gaugeTextColor, font: GUIStyle.DigitalFont);

        float vSpeedOffset = GAUGE_NOTCH_LINES_SEPARATION - (speed * GAUGE_NOTCH_LINES_SPEED_MULTI) % GAUGE_NOTCH_LINES_SEPARATION;
        for (float vSpeed = vSpeedOffset; vSpeed + _speedBar.Top.Y < _speedBar.Bottom.Y; vSpeed += GAUGE_NOTCH_LINES_SEPARATION)
        {
          DrawNotchSpeedBar(batch, vSpeed);
        }
        
      });

    void DrawNotchDepthBar(SpriteBatch spriteBatch, float distanceFromTop)
    { 
      DrawLineWithShadow(spriteBatch, _depthBar.Top + new Vector2(0f, distanceFromTop), _depthBar.Top + new Vector2(GAUGE_NOTCH_LINES_WIDTH, distanceFromTop), _gaugeLineColor, width: 3f);
    }
    
    void DrawNotchSpeedBar(SpriteBatch spriteBatch, float distanceFromTop)
    { 
      DrawLineWithShadow(spriteBatch, _speedBar.Top + new Vector2(0f, distanceFromTop), _speedBar.Top + new Vector2(-GAUGE_NOTCH_LINES_WIDTH, distanceFromTop), _gaugeLineColor, width: 3f);
    }
    
    void DrawLineWithShadow(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float depth = 0f,
      float width = 1f)
    {
      GUI.DrawLine(spriteBatch, start, end, new Color(10, 10, 10, 100), depth+1, width+3);
      GUI.DrawLine(spriteBatch, start, end, new Color(50, 50, 50, 175), depth+1, width+1);
      GUI.DrawLine(spriteBatch, start, end, color, depth, width);
    }
    void CalculateDrawArea()
    {
      _screenDrawSize = new Vector2(GameMain.GraphicsWidth * GuiSizeRatioX, GameMain.GraphicsHeight * GuiSizeRatioY);
      float spacingHorizontal = (GameMain.GraphicsWidth - _screenDrawSize.X) / 2f;
      float spacingVertical = (GameMain.GraphicsHeight - _screenDrawSize.Y) / 2f;
      _screenDrawArea = (
        TopLeft: new Vector2(spacingHorizontal, spacingVertical),
        TopRight: new Vector2(GameMain.GraphicsWidth - spacingHorizontal, spacingVertical),
        BottomRight: new Vector2(GameMain.GraphicsWidth -spacingHorizontal, GameMain.GraphicsHeight - spacingVertical),
        BottomLeft: new Vector2(spacingHorizontal, GameMain.GraphicsHeight - spacingVertical));
    }
  }

  public override void DrawHUD(SpriteBatch spriteBatch, Character character)
  {
    base.DrawHUD(spriteBatch, character);
    CreateHUD();
  }
}