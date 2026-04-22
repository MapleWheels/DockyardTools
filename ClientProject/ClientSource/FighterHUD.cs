using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace DockyardTools;

public partial class FighterHUD
{
  #region SerializedFields
  
  [Editable(0.1f, 1f, 3), Serialize(0.6f, IsPropertySaveable.No, description: "Width of the GUI relative to screen width.")]
  public float GuiSizeRatioX { get; set; }
  
  [Editable(0.1f, 1f, 3), Serialize(0.7f, IsPropertySaveable.No, description: "Height of the GUI relative to screen height.")]
  public float GuiSizeRatioY { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the depth gauge.")]
  public bool RenderDepthGauge { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the speed gauge.")]
  public bool RenderSpeedGauge { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the primary ammunition counter.")]
  public bool RenderPrimaryAmmoCounter { get; set; }
  
  
  
  #endregion
  
  // -- Working vars
  private GUICustomComponent _hudDraw;
  private Vector2 _screenDrawSize;
  private (Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft) _screenDrawArea;
  private (Vector2 Top, Vector2 Bottom) _depthBar, _speedBar;
  private (Vector2 Start, Vector2 End) _depthBarTopNotch, _depthBarBottomNotch, _depthBarMidNotch, 
    _speedBarTopNotch, _speedBarBottomNotch, _speedBarMidNotch;

  private float _depthNotchPixelSeparation, _speedNotchPixelSeparation;
  private float _depthNotchPixelsPerUnit, _speedNotchPixelsPerUnit;
  private float _depthBarHeight, _speedBarHeight;
  
  // -- Config
  private const float GAUGE_NOTCH_LINES_WIDTH = 20f;
  private const float GAUGE_NOTCH_DEPTH_LINES_COUNT = 10f;
  private const float GAUGE_NOTCH_SPEED_LINES_COUNT = 10f;
  private const float GAUGE_NOTCH_DEPTH_SEPARATION = 20f;
  private const float GAUGE_NOTCH_SPEED_SEPARATION = 5f;
  private const float GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH = 10f;
  private readonly Vector2 GAUGE_TEXT_DISPLAY_RECT = new Vector2(80f, 30f);
  private readonly Vector2 _gaugeRelativeOffset = new Vector2(0.0f, 0.5f); 
  private readonly Vector2 _gaugeFixedOffsetDepth = new Vector2(-90f, -20f) + new Vector2(-GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH, 0f); 
  private readonly Vector2 _gaugeFixedOffsetSpeed = new Vector2(20f, -20f) + new Vector2(GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH, 0f); 
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

    float depthBarMidPoint = (_depthBar.Bottom.Y + _depthBar.Top.Y) / 2f;
    float speedBarMidPoint = (_speedBar.Bottom.Y + _speedBar.Top.Y) / 2f;
    
    _depthBarTopNotch.Start = _depthBar.Top;
    _depthBarTopNotch.End = _depthBar.Top + new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);
    _depthBarBottomNotch.Start = _depthBar.Bottom;
    _depthBarBottomNotch.End = _depthBar.Bottom + new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);
    _depthBarMidNotch.Start = new Vector2(_depthBar.Top.X - GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH, depthBarMidPoint);
    _depthBarMidNotch.End = new Vector2(_depthBar.Top.X, depthBarMidPoint) + new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);

    _speedBarTopNotch.Start = _speedBar.Top;
    _speedBarTopNotch.End = _speedBar.Top - new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);
    _speedBarBottomNotch.Start = _speedBar.Bottom;
    _speedBarBottomNotch.End = _speedBar.Bottom - new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);
    _speedBarMidNotch.Start = new Vector2(_speedBar.Top.X + GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH, speedBarMidPoint);
    _speedBarMidNotch.End = new Vector2(_speedBar.Top.X, speedBarMidPoint) - new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);

    _depthNotchPixelSeparation = (_depthBar.Bottom.Y - _depthBar.Top.Y) / GAUGE_NOTCH_DEPTH_LINES_COUNT;
    _speedNotchPixelSeparation = (_speedBar.Bottom.Y - _speedBar.Top.Y) / GAUGE_NOTCH_SPEED_LINES_COUNT;

    _depthNotchPixelsPerUnit = _depthNotchPixelSeparation / GAUGE_NOTCH_DEPTH_SEPARATION;
    _speedNotchPixelsPerUnit = _speedNotchPixelSeparation / GAUGE_NOTCH_SPEED_SEPARATION;
    
    _hudDraw = new GUICustomComponent(new RectTransform(Vector2.One, GuiFrame.RectTransform),
      onDraw: (batch, component) =>
      {
        if (RenderDepthGauge)
        {
          DrawDepthGauge();
        }

        if (RenderSpeedGauge)
        {
          DrawSpeedGauge();
        }
        

        void DrawDepthGauge()
        {
          // --- Draw Depth Gauge
          
          DrawLineWithShadow(batch, _depthBar.Top, _depthBar.Bottom, _gaugeLineColor, width: 3f);
          // top, bottom, middle notches
          DrawLineWithShadow(batch, _depthBarTopNotch.Start, _depthBarTopNotch.End, _gaugeLineColor, width: 3f);
          DrawLineWithShadow(batch, _depthBarBottomNotch.Start, _depthBarBottomNotch.End, _gaugeLineColor, width: 3f);
          DrawLineWithShadow(batch, _depthBarMidNotch.Start, _depthBarMidNotch.End, _gaugeLineColor, width: 3f);
      
          // in-between moving lines
          DrawDepthNotches(_currentPosition.Y);
          
          // tests: depth number
          Vector2 offsetDepthTextPos = _gaugeFixedOffsetDepth + _depthBar.Top + (_depthBar.Bottom - _depthBar.Top) * new Vector2(-_gaugeRelativeOffset.X, _gaugeRelativeOffset.Y); // invert X
          GUI.DrawString(batch, offsetDepthTextPos, _currentPosition.Y.ToString("0000"), _gaugeTextColor, font: GUIStyle.DigitalFont);
        }
        
        void DrawDepthNotches(float currentDepth)
        {
          float minDisplayableDepth = currentDepth - (depthBarMidPoint - _depthBar.Top.Y) / _depthNotchPixelsPerUnit;

          // this is the lowest value notch that can display
          float minDepthNotchValue = GAUGE_NOTCH_DEPTH_SEPARATION - (minDisplayableDepth % GAUGE_NOTCH_DEPTH_SEPARATION) + minDisplayableDepth;
          float notchOffset = (GAUGE_NOTCH_DEPTH_SEPARATION - (minDisplayableDepth % GAUGE_NOTCH_DEPTH_SEPARATION)) * _depthNotchPixelsPerUnit;
          
          for (int i = 0; i < GAUGE_NOTCH_DEPTH_LINES_COUNT; i++)
          {
            float vertOffset = notchOffset + i * _depthNotchPixelSeparation;
            float notchDepth = minDepthNotchValue + GAUGE_NOTCH_DEPTH_SEPARATION * i;
            if (vertOffset + _depthBar.Top.Y > _depthBar.Bottom.Y || notchDepth < 0f)
            {
              continue;
            }
            DrawNotchDepthBar(batch, vertOffset, notchDepth);
          }
          
        }
        
        void DrawNotchDepthBar(SpriteBatch spriteBatch, float distanceFromTop, float depthValue)
        { 
          DrawLineWithShadow(spriteBatch, _depthBar.Top + new Vector2(0f, distanceFromTop), _depthBar.Top + new Vector2(GAUGE_NOTCH_LINES_WIDTH, distanceFromTop), _gaugeLineColor, width: 2f);
          GUI.DrawString(spriteBatch, _depthBar.Top + new Vector2(GAUGE_NOTCH_LINES_WIDTH + 20f, distanceFromTop - 5f), depthValue.ToString("0000"), _gaugeTextColor, font: GUIStyle.SmallFont);
        }

        void DrawSpeedGauge()
        {
          // --- Draw Speed Gauge
          DrawLineWithShadow(batch, _speedBar.Top, _speedBar.Bottom, _gaugeLineColor, width: 3f);
          // top, bottom, middle notches
          DrawLineWithShadow(batch, _speedBarTopNotch.Start, _speedBarTopNotch.End, _gaugeLineColor, width: 3f);
          DrawLineWithShadow(batch, _speedBarBottomNotch.Start, _speedBarBottomNotch.End, _gaugeLineColor, width: 3f);
          DrawLineWithShadow(batch, _speedBarMidNotch.Start, _speedBarMidNotch.End, _gaugeLineColor, width: 3f);
     
          // in-between moving lines
          // tests: speed number
          float speed = (float)Math.Sqrt(Math.Pow(_currentVelocity.X, 2) + Math.Pow(_currentVelocity.Y, 2));

          DrawSpeedNotches(speed);
          
          Vector2 offsetSpeedtextPos = _gaugeFixedOffsetSpeed + _speedBar.Top + (_speedBar.Bottom - _speedBar.Top) * _gaugeRelativeOffset;
          GUI.DrawString(batch, offsetSpeedtextPos, speed.ToString("000"), _gaugeTextColor, font: GUIStyle.DigitalFont);
        }
        
        void DrawSpeedNotches(float currentSpeed)
        {
          float minDisplayableSpeed = currentSpeed - (speedBarMidPoint - _speedBar.Top.Y) / _speedNotchPixelsPerUnit;

          // this is the lowest value notch that can display
          float minSpeedNotchValue = GAUGE_NOTCH_SPEED_SEPARATION - (minDisplayableSpeed % GAUGE_NOTCH_SPEED_SEPARATION) + minDisplayableSpeed;
          float notchOffset = (GAUGE_NOTCH_SPEED_SEPARATION - (minDisplayableSpeed % GAUGE_NOTCH_SPEED_SEPARATION)) * _speedNotchPixelsPerUnit;
          
          for (int i = 0; i < GAUGE_NOTCH_SPEED_LINES_COUNT; i++)
          {
            float vertOffset = notchOffset + i * _speedNotchPixelSeparation;
            float notchSpeed = minSpeedNotchValue + GAUGE_NOTCH_SPEED_SEPARATION * i;
            if (vertOffset + _speedBar.Top.Y > _speedBar.Bottom.Y || notchSpeed < 0f)
            {
              continue;
            }
            DrawNotchSpeedBar(batch, vertOffset, notchSpeed);
          }
          
        }
        
        void DrawNotchSpeedBar(SpriteBatch spriteBatch, float distanceFromTop, float speedValue)
        { 
          DrawLineWithShadow(spriteBatch, _speedBar.Top + new Vector2(0f, distanceFromTop), _speedBar.Top + new Vector2(-GAUGE_NOTCH_LINES_WIDTH, distanceFromTop), _gaugeLineColor, width: 2f);
          GUI.DrawString(spriteBatch, _speedBar.Top + new Vector2(-GAUGE_NOTCH_LINES_WIDTH - 18f, distanceFromTop - 5f), speedValue.ToString("000"), _gaugeTextColor, font: GUIStyle.SmallFont);
        }

        
        
      });

    
    
    void DrawLineWithShadow(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float depth = 0f,
      float width = 1f)
    {
      GUI.DrawLine(spriteBatch, start, end, new Color(20, 20, 20, 100), depth+1, width+3);
      GUI.DrawLine(spriteBatch, start, end, new Color(color.R/2, color.G/2, color.B/2, 175), depth+1, width+1);
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