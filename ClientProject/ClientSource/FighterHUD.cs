using System.Numerics;
using Barotrauma.Sounds;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace DockyardTools;

public partial class FighterHUD
{
  #region SerializedFields
  
  [Editable(0.1f, 1f, 3), Serialize(0.55f, IsPropertySaveable.No, description: "Width of the GUI relative to screen width.")]
  public float GuiSizeRatioX { get; set; }
  
  [Editable(0.1f, 1f, 3), Serialize(0.7f, IsPropertySaveable.No, description: "Height of the GUI relative to screen height.")]
  public float GuiSizeRatioY { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the depth gauge.")]
  public bool RenderDepthGauge { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the speed gauge.")]
  public bool RenderSpeedGauge { get; set; }
  
  [Editable, Serialize(200f, IsPropertySaveable.No, "Offset between info display texts and the number values.")]
  public float InfoTextsHorizontalOffset { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the primary ammunition counter.")]
  public bool RenderPrimaryAmmoCounter { get; set; }
  
  [Editable, Serialize("PRIMARY_AMMO", IsPropertySaveable.No, "Primary Weapon Name.")]
  public string PrimaryWeaponName { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the secondary ammunition counter.")]
  public bool RenderSecondaryAmmoCounter { get; set; }
  
  [Editable, Serialize("SECONDARY_AMMO", IsPropertySaveable.No, "Secondary Weapon Name.")]
  public string SecondaryWeaponName { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the hull integrity percentage.")]
  public bool RenderHullIntegrity { get; set; }
  
  [Editable, Serialize("HULL_INTEGRITY", IsPropertySaveable.No, "Hull Integrity Display Text.")]
  public string HullIntegrityName { get; set; }
  
  [Editable(DecimalCount = 0, MaxValueFloat = 99f, MinValueFloat = 0f), Serialize(85f, IsPropertySaveable.No, "What hull integrity percent should be considered zero health.")]
  public float HullIntegrityMinimumTrueValue { get; set; }
  
  [Editable(DecimalCount = 0, MaxValueFloat = 99f, MinValueFloat = 0f), Serialize(95f, IsPropertySaveable.No, "What hull integrity percent should alarms activate.")]
  public float HullIntegrityWarningThreshold { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the crush depth.")]
  public bool RenderCrushDepth { get; set; }
  
  [Editable(0f, 10000f, 0), Serialize(500f, IsPropertySaveable.No, description: "Distance until crush depth to begin displaying the warning.")]
  public float CrushDepthDistanceThreshold { get; set; }
  
  [Editable(0f, 10000f, 0), Serialize(100f, IsPropertySaveable.No, description: "Distance until crush depth to begin audio warning.")]
  public float CrushDepthDistanceThresholdAudio { get; set; }
  
  [Editable, Serialize(true, IsPropertySaveable.No, "Should we render the notification system.")]
  public bool RenderNotificationSystem { get; set; }
  
  
  
  #endregion
  
  // -- Working vars
  private GUICustomComponent _hudDraw;
  private Sound? _activeSound = null;
  private readonly Queue<(Func<bool> Condition, Sound Sound)> _soundQueue = new();
  private Vector2 _screenDrawSize;
  private (Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft) _screenDrawArea;
  private (Vector2 Top, Vector2 Bottom) _depthBar, _speedBar;
  private (Vector2 Start, Vector2 End) _depthBarTopNotch, _depthBarBottomNotch, _depthBarMidNotch, 
    _speedBarTopNotch, _speedBarBottomNotch, _speedBarMidNotch;
  private Vector2 _infoTextsValuesOffset;
  private float _depthNotchPixelSeparation, _speedNotchPixelSeparation;
  private float _depthNotchPixelsPerUnit, _speedNotchPixelsPerUnit;
  private float _depthBarMidPoint, _speedBarMidPoint;
  
  // assets general
  private readonly Sprite _dangerIcon;
  private readonly ConcurrentDictionary<string, Sound> _sounds = new();
  private readonly ConcurrentDictionary<string, Sprite> _sprites = new();
  private readonly ConcurrentDictionary<string, Color> _colors = new();
  private readonly ConcurrentDictionary<string, NotificationDisplayHelper.Notification> _notifications = new();

  // general/master
  private readonly Vector2 _dangerIconFixedOffset = new Vector2(0f, 0f);

  // -- Warning Systems
  private RenderHelperComponent _hullDamageWarningInfo, _hullDamageWarningAudio;
  private RenderHelperComponent _masterCautionAudio;
  private bool _masterCautionAudioTriggered;
  private readonly NotificationDisplayHelper _notificationSystem;

  private readonly NotificationDisplayHelper.Notification
    _notificationPowerLow, _notificationDepthLimit, _notificationDescentSpeedHigh, _notificationHullDamaged;

  private readonly Color _infoTextsDescriptionColor;
  private readonly Color _infoTextsValueHighColor;
  private readonly Color _infoTextsValueLowColor;
  private readonly Color _infoTextsDangerColor;
  private readonly GUIFont _infoTextsDescriptionFont = GUIStyle.Font;
  private readonly GUIFont _infoTextsValuesFont = GUIStyle.DigitalFont;
  private readonly Vector2 _notificationsAreaTopLeftRelative = new Vector2(-0.16f, 0.61f);
  
  // tapes
  private const float GAUGE_NOTCH_LINES_WIDTH = 20f;
  private const float GAUGE_NOTCH_DEPTH_LINES_COUNT = 10f;
  private const float GAUGE_NOTCH_SPEED_LINES_COUNT = 10f;
  private const float GAUGE_NOTCH_DEPTH_SEPARATION = 20f;
  private const float GAUGE_NOTCH_SPEED_SEPARATION = 5f;
  private const float GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH = 10f;
  private const float GAUGE_NUMBER_DESC_VERT_SEPARATION = 45f;
  private readonly Vector2 _barLinesPadding = new Vector2(20f, 10f);
  private readonly Color _gaugeLineColor;
  
  // readings/gauges general
  private readonly Color _gaugeTextColor;
  private readonly GUIFont _gaugePrimaryNumberFont = GUIStyle.DigitalFont;
  private readonly GUIFont _gaugeNotchNumberFont = GUIStyle.SmallFont;
  private readonly Vector2 _gaugeRelativeOffset = new Vector2(0.0f, 0.5f); 
  private readonly Vector2 _gaugeFixedOffsetDepth = new Vector2(-130f, -20f) + new Vector2(-GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH, 0f); 
  private readonly Vector2 _gaugeFixedOffsetSpeed = new Vector2(20f, -20f) + new Vector2(GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH, 0f); 
  private readonly Vector2 _infoTextsRenderStartOffset = new Vector2(0f, 15f);
  private readonly Vector2 _infoTextsRenderSpacing = new Vector2(0f, 35f);
  
  // crush depth
  private float _submarineCrushDepth;
  private RenderHelperComponent _crushDepthWarningInfo, _crushDepthWarningAudio;
  private readonly Vector2 _gaugeFixedOffsetCrushDepth = new Vector2(-120f, 0f); // offset added to _gaugeFixedOffsetDepth.
  private readonly Vector2 _crushDepthDangerIconFixedOffset = new Vector2(-90f, 0f);
  private const float CRUSH_DEPTH_FLASHING_DUTY_CYCLE = 0.7f;
  private const float CRUSH_DEPTH_FLASHING_INTERVAL = 1.3f;
  private const float CRUSH_DEPTH_AUDIO_WARNING_INTERVAL = 5f;

  public FighterHUD(Item item, ContentXElement element) : base(item, element)
  {
    // ReSharper disable once VirtualMemberCallInConstructor
    IsActive = true;
    _notificationSystem = new();
    
    // value init
    // colors
    LoadColors();
    _infoTextsDescriptionColor = _colors["info-texts-description"];
    _infoTextsValueHighColor = _colors["info-texts-value-high"];
    _infoTextsValueLowColor = _colors["info-texts-value-low"];
    _infoTextsDangerColor = _colors["info-texts-danger"];
    _gaugeLineColor = _colors["gauge-line"];
    _gaugeTextColor = _colors["gauge-text"];
    
    // sprites
    LoadSprites();
    _dangerIcon = _sprites["danger-indicator-icon"];
    
    // sounds
    LoadSounds();
    
    // notifications
    _notificationSystem.DrawPastMaxHeight = false;
    _notificationSystem.MaxHeight = 500f;  
    NotificationDisplayHelper.Notification.DefaultDangerColor = _infoTextsDangerColor;
    NotificationDisplayHelper.Notification.DefaultWarningColor = _colors["info-texts-warning"];
    NotificationDisplayHelper.Notification.DefaultFont = GUIStyle.SmallFont;
    LoadNotificationsData();
    
    _notificationPowerLow = _notifications["power-low"];
    _notificationDepthLimit = _notifications["depth-limit"];
    _notificationDescentSpeedHigh = _notifications["descent-speed-high"];
    _notificationHullDamaged = _notifications["hull-damaged"];
    
    CreateHUD();


    void LoadNotificationsData()
    {
      var notificationsData = element.GetChildElement("Messages")!
        .GetChildElements("Message")
        .ToImmutableDictionary(elem => elem.GetAttributeString("name", string.Empty));

      foreach (var notificationData in notificationsData)
      {
        var notification = new NotificationDisplayHelper.Notification(
          notificationData.Key,
          TextManager.Get(notificationData.Value.GetAttributeString("identifier", string.Empty))
            .Fallback(notificationData.Key).ToString(),
          notificationData.Value.GetAttributeEnum("priority",
            NotificationDisplayHelper.Notification.MessagePriority.Advisory));
        notification.IsEnabled = false;
        
        _notifications.TryAdd(notificationData.Key, notification);
        _notificationSystem.AddMessage(notification);
      }
    }
    
    void LoadColors()
    {
      var colorData = element.GetChildElement("Colors")!
        .GetChildElements("Color")
        .ToImmutableDictionary(elem => elem.GetAttributeString("name", string.Empty));

      foreach (var color in colorData)
      {
        _colors.TryAdd(color.Key, color.Value.GetAttributeColor("rgba", new Color(255, 255, 255, 255)));
      }
    }
    
    void LoadSprites()
    {
      var spriteData = element.GetChildElement("Sprites")!
        .GetChildElements("Sprite")
        .ToImmutableDictionary(elem => elem.GetAttributeString("name", string.Empty));

      foreach (var sprite in spriteData)
      {
        _sprites.TryAdd(sprite.Key, new Sprite(sprite.Value));
      }
    } 
    
    void LoadSounds()
    {
      var soundData = element.GetChildElement("Sounds")!
        .GetChildElements("Sound")
        .ToImmutableDictionary(elem => elem.GetAttributeString("name", string.Empty));

      foreach (var sound in soundData)
      {
        _sounds.TryAdd(sound.Key, GameMain.SoundManager.LoadSound(soundData[sound.Key], false));
      }
    } 
  }
  
  public override void CreateGUI()
  {
    base.CreateGUI();
    CreateHUD();
  }

  private void OnItemLoadedClient()
  {
    CreateHUD();
  }

  public override void OnMapLoaded()
  {
    base.OnMapLoaded();
    _submarineCrushDepth = this.Item.Submarine.Info.GetSubCrushDepth();
  }

  private void CreateHUD()
  {
    GuiFrame.ClearChildren();
    CalculateDrawArea();

    _hudDraw = new GUICustomComponent(new RectTransform(Vector2.One, GuiFrame.RectTransform),
      onDraw: (batch, component) =>
      {
        ResetLoadedNotificationsStates();
        SetLoadedNotificationStatesConditional(out var notificationsActive);
        if (notificationsActive)
        {
          SetMasterCaution();
        }
        else
        {
          ResetMasterCaution();
        }

        if (RenderDepthGauge)
        {
          DrawDepthGauge();
        }

        if (RenderSpeedGauge)
        {
          DrawSpeedGauge();
        }

        DrawStatusInfoTexts(batch, _infoTextsRenderStartOffset + _depthBar.Bottom);

        if (RenderCrushDepth)
        {
          DrawCrushDepthWarnings();
        }

        if (RenderNotificationSystem)
        {
          DrawNotifications();
        }

        _masterCautionAudio?.Draw(batch);

        UpdateSoundQueue();


        #region DEPTH_TAPE

        void DrawDepthGauge()
        {
          // --- Draw Depth Gauge

          DrawLineWithShadow(batch, _depthBar.Top, _depthBar.Bottom, _gaugeLineColor, width: 3f);
          // top, bottom, middle notches
          DrawLineWithShadow(batch, _depthBarTopNotch.Start, _depthBarTopNotch.End, _gaugeLineColor, width: 3f);
          DrawLineWithShadow(batch, _depthBarBottomNotch.Start, _depthBarBottomNotch.End, _gaugeLineColor, width: 3f);
          DrawLineWithShadow(batch, _depthBarMidNotch.Start, _depthBarMidNotch.End, _gaugeLineColor, width: 3f);

          // in-between moving lines
          DrawDepthTape(_currentPosition.Y);

          // depth number
          Vector2 offsetDepthTextPos = _gaugeFixedOffsetDepth + _depthBar.Top + (_depthBar.Bottom - _depthBar.Top) *
            new Vector2(-_gaugeRelativeOffset.X, _gaugeRelativeOffset.Y); // invert X
          GUI.DrawString(batch, offsetDepthTextPos, _currentPosition.Y.ToString("0000"), _gaugeTextColor,
            font: _gaugePrimaryNumberFont);
          GUI.DrawString(batch, offsetDepthTextPos + new Vector2(5f, GAUGE_NUMBER_DESC_VERT_SEPARATION), "Depth",
            _gaugeTextColor, font: _gaugeNotchNumberFont);
        }

        void DrawDepthTape(float currentDepth)
        {
          float minDisplayableDepth = currentDepth - (_depthBarMidPoint - _depthBar.Top.Y) / _depthNotchPixelsPerUnit;

          // this is the lowest value notch that can display
          float minDepthNotchValue = GAUGE_NOTCH_DEPTH_SEPARATION -
            (minDisplayableDepth % GAUGE_NOTCH_DEPTH_SEPARATION) + minDisplayableDepth;
          float notchOffset = (GAUGE_NOTCH_DEPTH_SEPARATION - (minDisplayableDepth % GAUGE_NOTCH_DEPTH_SEPARATION)) *
                              _depthNotchPixelsPerUnit;

          for (int i = 0; i < GAUGE_NOTCH_DEPTH_LINES_COUNT; i++)
          {
            float vertOffset = notchOffset + i * _depthNotchPixelSeparation;
            float notchDepth = minDepthNotchValue + GAUGE_NOTCH_DEPTH_SEPARATION * i;
            if (vertOffset + _depthBar.Top.Y > _depthBar.Bottom.Y || notchDepth < 0f)
            {
              continue;
            }

            DrawDepthTapeNotch(batch, vertOffset, notchDepth);
          }

        }

        void DrawDepthTapeNotch(SpriteBatch spriteBatch, float distanceFromTop, float depthValue)
        {
          DrawLineWithShadow(spriteBatch, _depthBar.Top + new Vector2(0f, distanceFromTop),
            _depthBar.Top + new Vector2(GAUGE_NOTCH_LINES_WIDTH, distanceFromTop), _gaugeLineColor, width: 2f);
          GUI.DrawString(spriteBatch, _depthBar.Top + new Vector2(GAUGE_NOTCH_LINES_WIDTH + 15f, distanceFromTop - 5f),
            depthValue.ToString("0000"), _gaugeTextColor, font: _gaugeNotchNumberFont);
        }

        #endregion

        #region SPEED_TAPE

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

          DrawSpeedTape(speed);

          Vector2 offsetSpeedtextPos = _gaugeFixedOffsetSpeed + _speedBar.Top +
                                       (_speedBar.Bottom - _speedBar.Top) * _gaugeRelativeOffset;
          GUI.DrawString(batch, offsetSpeedtextPos, speed.ToString("000"), _gaugeTextColor,
            font: _gaugePrimaryNumberFont);
          GUI.DrawString(batch, offsetSpeedtextPos + new Vector2(0f, GAUGE_NUMBER_DESC_VERT_SEPARATION), "Speed",
            _gaugeTextColor, font: _gaugeNotchNumberFont);
        }

        void DrawSpeedTape(float currentSpeed)
        {
          float minDisplayableSpeed = currentSpeed - (_speedBarMidPoint - _speedBar.Top.Y) / _speedNotchPixelsPerUnit;

          // this is the lowest value notch that can display
          float minSpeedNotchValue = GAUGE_NOTCH_SPEED_SEPARATION -
            (minDisplayableSpeed % GAUGE_NOTCH_SPEED_SEPARATION) + minDisplayableSpeed;
          float notchOffset = (GAUGE_NOTCH_SPEED_SEPARATION - (minDisplayableSpeed % GAUGE_NOTCH_SPEED_SEPARATION)) *
                              _speedNotchPixelsPerUnit;

          for (int i = 0; i < GAUGE_NOTCH_SPEED_LINES_COUNT; i++)
          {
            float vertOffset = notchOffset + i * _speedNotchPixelSeparation;
            float notchSpeed = minSpeedNotchValue + GAUGE_NOTCH_SPEED_SEPARATION * i;
            if (vertOffset + _speedBar.Top.Y > _speedBar.Bottom.Y || notchSpeed < 0f)
            {
              continue;
            }

            DrawSpeedTapeNotch(batch, vertOffset, notchSpeed);
          }

        }

        void DrawSpeedTapeNotch(SpriteBatch spriteBatch, float distanceFromTop, float speedValue)
        {
          DrawLineWithShadow(spriteBatch, _speedBar.Top + new Vector2(0f, distanceFromTop),
            _speedBar.Top + new Vector2(-GAUGE_NOTCH_LINES_WIDTH, distanceFromTop), _gaugeLineColor, width: 2f);
          GUI.DrawString(spriteBatch, _speedBar.Top + new Vector2(-GAUGE_NOTCH_LINES_WIDTH - 35f, distanceFromTop - 5f),
            speedValue.ToString("000"), _gaugeTextColor, font: _gaugeNotchNumberFont);
        }

        #endregion

        #region INFO_TEXTS

        void DrawCrushDepthWarnings()
        {
          CheckInitCrushDepthWarningComponent();

          // we do audio using draw() so that it's only played on the client if they are controlling the periscope.
          _crushDepthWarningInfo?.Draw(batch);
          _crushDepthWarningAudio?.Draw(batch);
        }

        void DrawNotifications()
        {
          var drawPos = new Vector2(_notificationsAreaTopLeftRelative.X * _screenDrawSize.X + _screenDrawArea.TopLeft.X,
            _notificationsAreaTopLeftRelative.Y * _screenDrawSize.Y + _screenDrawArea.TopLeft.Y);

          _notificationSystem.Draw(batch, drawPos, out var finalPos);
        }

        #endregion
      });

    void CheckInitCrushDepthWarningComponent()
    {
      if (_crushDepthWarningInfo is null)
      {
        InitCrushDepthWarningDisplayComponent();
      }

      if (_crushDepthWarningAudio is null)
      {
        InitCrushDepthWarningAudioComponent();
      }

      void InitCrushDepthWarningDisplayComponent()
      {
        _crushDepthWarningInfo = new RenderHelperComponent(
          onDraw: (comp, batch) =>
          {
            Vector2 offsetCrushDepthTextPos = _gaugeFixedOffsetCrushDepth + _gaugeFixedOffsetDepth + _depthBar.Top +
                                              (_depthBar.Bottom - _depthBar.Top) * new Vector2(-_gaugeRelativeOffset.X,
                                                _gaugeRelativeOffset.Y); // invert X
            _dangerIcon.Draw(batch, offsetCrushDepthTextPos + _dangerIconFixedOffset + _crushDepthDangerIconFixedOffset,
              Color.White,
              _dangerIcon.RelativeOrigin, 0f, _dangerIcon.RelativeSize);
            GUI.DrawString(batch, offsetCrushDepthTextPos, _submarineCrushDepth.ToString("0000"), _infoTextsDangerColor,
              font: _gaugePrimaryNumberFont);
            GUI.DrawString(batch, offsetCrushDepthTextPos + new Vector2(5f, GAUGE_NUMBER_DESC_VERT_SEPARATION),
              "Crush Depth", _infoTextsDangerColor, font: _gaugeNotchNumberFont);
          },
          onUpdate: (comp, deltaTime) =>
          {
            comp.ShouldRender = GetDistanceToCrushDepth() < CrushDepthDistanceThreshold;
            _notificationDepthLimit.IsEnabled = comp.ShouldRender;
          });
        _crushDepthWarningInfo.FlashingEnabled = true;
        _crushDepthWarningInfo.FlashDuration = CRUSH_DEPTH_FLASHING_INTERVAL;
        _crushDepthWarningInfo.FlashingDutyCycle = CRUSH_DEPTH_FLASHING_DUTY_CYCLE;
      }

      void InitCrushDepthWarningAudioComponent()
      {
        _crushDepthWarningAudio = new RenderHelperComponent(onDraw: (comp, batch) =>
          {
            var depthWarning = _sounds["depth-limit-0-female"];
            _soundQueue.Enqueue((() =>
            {
              // we want to stop enqueuing sounds until the one in queue is played, so we re-enable right before playing the sound.
              comp.ShouldUpdate = true;
              return !depthWarning.IsPlaying();
            }, depthWarning));
            comp.FlashingDutyTimeRemaining = 0f;
            comp.ShouldUpdate = false;
          },
          onUpdate: (comp, deltaTime) =>
          {
            comp.ShouldRender = GetDistanceToCrushDepth() < CrushDepthDistanceThresholdAudio;
          }, false, true);

        _crushDepthWarningAudio.FlashingEnabled = true;
        _crushDepthWarningAudio.FlashDuration = CRUSH_DEPTH_AUDIO_WARNING_INTERVAL;
        _crushDepthWarningAudio.FlashingDutyCycle = 1f; // disabled in draw after playing
      }
    }

    void CheckInitHullDamageWarning()
    {
      if (_hullDamageWarningInfo is null)
      {
        _hullDamageWarningInfo = new RenderHelperComponent(onDraw: (comp, batch) =>
          {
            _notificationHullDamaged.IsEnabled = true;
            comp.ShouldRender = false;
          }, onUpdate: (comp, deltaTime) =>
          {
            comp.ShouldRender = _hullHpPercent < HullIntegrityWarningThreshold;
          },
          shouldRender: false, shouldUpdate: true);
      }

      if (_hullDamageWarningAudio is null)
      {
        _hullDamageWarningAudio = new RenderHelperComponent(onDraw: (comp, batch) =>
          {
            if (comp.ShouldUpdate)
            {
              var sound = _sounds["hull-integrity"];
              _soundQueue.Enqueue((() => !sound.IsPlaying(), sound));
              comp.ShouldUpdate = false;
              return;
            }

            if (_hullHpPercent > HullIntegrityWarningThreshold)
            {
              comp.ShouldUpdate = true;
              comp.ShouldRender = false;
            }
          }, onUpdate: (comp, deltaTime) =>
          {
            if (_hullHpPercent < HullIntegrityWarningThreshold)
            {
              comp.ShouldRender = true;
            }
          },
          shouldRender: false, shouldUpdate: true);
      }
    }

    float GetDistanceToCrushDepth() => _submarineCrushDepth - _currentPosition.Y;

    void UpdateSoundQueue()
    {
      if (_activeSound is not null && _activeSound.IsPlaying())
      {
        return;
      }

      // don't play enqueued sounds while not in use if supported.
      if (_controller is not null && _controller.User is null)
      {
        return;
      }

      if (_soundQueue.TryDequeue(out var enqueuedSound))
      {
        if (enqueuedSound.Condition())
        {
          _activeSound = enqueuedSound.Sound;
          _activeSound.Play(new Vector3(Item.PositionX, Item.PositionY, 0f), 1f);
        }
      }
    }

    void ResetLoadedNotificationsStates()
    {
      foreach (var notification in _notifications)
      {
        notification.Value.IsEnabled = false;
      }
    }

    void SetLoadedNotificationStatesConditional(out bool notificationsActive)
    {
      notificationsActive = false;
      //_notifications["power-low"].IsEnabled = // not implemented
      //_notifications["descent-speed-high"].IsEnabled = // not implemented
      notificationsActive |= CheckSetNotification(in _notificationDepthLimit, () => GetDistanceToCrushDepth() < CrushDepthDistanceThreshold);
      notificationsActive |= CheckSetNotification(in _notificationHullDamaged, () => RenderHullIntegrity && _hullHpPercent < HullIntegrityWarningThreshold);

      bool CheckSetNotification(in NotificationDisplayHelper.Notification notification, Func<bool> predicate)
      {
        bool val = predicate();
        notification.IsEnabled = val;
        return val;
      }
    }

    void DrawStatusInfoTexts(SpriteBatch batch, Vector2 infoTextsNextRenderPosition)
    {
      if (RenderPrimaryAmmoCounter)
      {
        var valueColor = Color.Lerp(_infoTextsValueLowColor, _infoTextsValueHighColor, _ammo1Percent);
        DrawInfoTextBottomArea(PrimaryWeaponName, _ammo1Percent, valueColor);
      }

      if (RenderSecondaryAmmoCounter)
      {
        var valueColor = Color.Lerp(_infoTextsValueLowColor, _infoTextsValueHighColor, _ammo2Percent);
        DrawInfoTextBottomArea(SecondaryWeaponName, _ammo2Percent, valueColor);
      }

      if (RenderHullIntegrity)
      {
        float adjustedHpRatio =
          (Math.Clamp(_hullHpPercent, HullIntegrityMinimumTrueValue, 100f) - HullIntegrityMinimumTrueValue) /
          Math.Max(100f - HullIntegrityMinimumTrueValue, 0.01f);
        float displayedHpPercentage = float.Lerp(0f, 100f, adjustedHpRatio);
        var valueColor = Color.Lerp(_infoTextsValueLowColor, _infoTextsValueHighColor, adjustedHpRatio);
        DrawInfoTextBottomArea(HullIntegrityName, displayedHpPercentage, valueColor);
        _notificationHullDamaged.IsEnabled = displayedHpPercentage < 99f;

        CheckInitHullDamageWarning();
        _hullDamageWarningInfo?.Draw(batch);
        _hullDamageWarningAudio?.Draw(batch);
      }

      void DrawInfoTextBottomArea(string infoDescription, float infoValue, Color infoValueColor)
      {
        var pos = GetNextInfoTextsPosition();
        GUI.DrawString(batch, pos, infoDescription, _infoTextsDescriptionColor, font: _infoTextsDescriptionFont);
        GUI.DrawString(batch, pos + _infoTextsValuesOffset, $"{infoValue.ToString("F0")}", infoValueColor,
          font: _infoTextsValuesFont);
      }

      Vector2 GetNextInfoTextsPosition()
      {
        var curr = infoTextsNextRenderPosition;
        infoTextsNextRenderPosition += _infoTextsRenderSpacing;
        return curr;
      }
    }

    void DrawLineWithShadow(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float depth = 0f,
      float width = 1f)
    {
      GUI.DrawLine(spriteBatch, start, end, new Color(20, 20, 20, 100), depth + 1, width + 3);
      GUI.DrawLine(spriteBatch, start, end, new Color(color.R / 2, color.G / 2, color.B / 2, 175), depth + 1,
        width + 1);
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
        BottomRight: new Vector2(GameMain.GraphicsWidth - spacingHorizontal, GameMain.GraphicsHeight - spacingVertical),
        BottomLeft: new Vector2(spacingHorizontal, GameMain.GraphicsHeight - spacingVertical));

      _depthBar = (
        Top: _screenDrawArea.TopLeft + new Vector2(_barLinesPadding.X, _barLinesPadding.Y),
        Bottom: _screenDrawArea.BottomLeft + new Vector2(_barLinesPadding.X, -_barLinesPadding.Y));

      _speedBar = (
        Top: _screenDrawArea.TopRight + new Vector2(-_barLinesPadding.X, _barLinesPadding.Y),
        Bottom: _screenDrawArea.BottomRight + new Vector2(-_barLinesPadding.X, -_barLinesPadding.Y));

      _depthBarMidPoint = (_depthBar.Bottom.Y + _depthBar.Top.Y) / 2f;
      _speedBarMidPoint = (_speedBar.Bottom.Y + _speedBar.Top.Y) / 2f;

      _depthBarTopNotch.Start = _depthBar.Top;
      _depthBarTopNotch.End = _depthBar.Top + new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);
      _depthBarBottomNotch.Start = _depthBar.Bottom;
      _depthBarBottomNotch.End = _depthBar.Bottom + new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);
      _depthBarMidNotch.Start =
        new Vector2(_depthBar.Top.X - GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH, _depthBarMidPoint);
      _depthBarMidNotch.End = new Vector2(_depthBar.Top.X, _depthBarMidPoint) +
                              new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);

      _speedBarTopNotch.Start = _speedBar.Top;
      _speedBarTopNotch.End = _speedBar.Top - new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);
      _speedBarBottomNotch.Start = _speedBar.Bottom;
      _speedBarBottomNotch.End = _speedBar.Bottom - new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);
      _speedBarMidNotch.Start =
        new Vector2(_speedBar.Top.X + GAUGE_NOTCH_MIDDLE_OUTEREXTENSION_LENGTH, _speedBarMidPoint);
      _speedBarMidNotch.End = new Vector2(_speedBar.Top.X, _speedBarMidPoint) -
                              new Vector2(GAUGE_NOTCH_LINES_WIDTH + 5f, 0f);

      _depthNotchPixelSeparation = (_depthBar.Bottom.Y - _depthBar.Top.Y) / GAUGE_NOTCH_DEPTH_LINES_COUNT;
      _speedNotchPixelSeparation = (_speedBar.Bottom.Y - _speedBar.Top.Y) / GAUGE_NOTCH_SPEED_LINES_COUNT;

      _depthNotchPixelsPerUnit = _depthNotchPixelSeparation / GAUGE_NOTCH_DEPTH_SEPARATION;
      _speedNotchPixelsPerUnit = _speedNotchPixelSeparation / GAUGE_NOTCH_SPEED_SEPARATION;

      _infoTextsValuesOffset = new Vector2(InfoTextsHorizontalOffset, -5f);
    }
  }



  void SetMasterCaution()
  {
    if (_masterCautionAudio is null)
    {
      _masterCautionAudio = new RenderHelperComponent(onDraw: (comp, batch) =>
      {
        var masterCaution = _sounds["master_caution"];
        _soundQueue.Enqueue((() => !masterCaution.IsPlaying(), masterCaution));

        comp.ShouldRender = false;
      }, onUpdate: (comp, deltaTime) =>
      {
        if (!_masterCautionAudioTriggered)
        {
          comp.ShouldRender = true;
          _masterCautionAudioTriggered = true;
        }

        comp.ShouldUpdate = false;
      }, false, false);
    }
      
    _masterCautionAudio.ShouldUpdate = true;
  }

  void ResetMasterCaution()
  {
    _masterCautionAudioTriggered = false;
  }
  
  public override void Update(float deltaTime, Camera cam)
  {
    _crushDepthWarningInfo?.Update(deltaTime);
    _crushDepthWarningAudio?.Update(deltaTime);
    _masterCautionAudio?.Update(deltaTime);
    _notificationSystem?.Update(deltaTime);
    _hullDamageWarningInfo?.Update(deltaTime);
    _hullDamageWarningAudio?.Update(deltaTime);
  }

  public override void DrawHUD(SpriteBatch spriteBatch, Character character)
  {
    base.DrawHUD(spriteBatch, character);
    CreateHUD();
  }
}