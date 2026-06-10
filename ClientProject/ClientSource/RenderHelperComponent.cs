namespace DockyardTools;

public class RenderHelperComponent : IDisposable
{
  public bool ShouldRender { get; set; }
  public bool ShouldUpdate { get; set; }
  public bool FlashingEnabled { get; set; }
  public float FlashDuration { get; set; }
  /// <summary>
  /// Ratio of time spent on/displayed.
  /// </summary>
  public float FlashingDutyCycle { get; set; }

  //
  private Action<RenderHelperComponent> _onDraw;
  private Action<RenderHelperComponent, float> _onUpdate;
  /// <summary>
  /// The total time remaining until the flashing cycle is restarted.
  /// </summary>
  private float _flashingTimeUntilReset;
  /// <summary>
  /// The ON/duty time remaining in the current flashing cycle.
  /// </summary>
  private float _flashingDutyTimeRemaining;
  
  public RenderHelperComponent(Action<RenderHelperComponent> onDraw, Action<RenderHelperComponent, float> onUpdate)
  {
    _onDraw = onDraw ?? throw new ArgumentNullException(nameof(onDraw));
    _onUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));
    ShouldRender = true;
    ShouldUpdate = true;
  }

  public void Draw()
  {
    if (!ShouldRender)
    {
      return;
    }

    if (FlashingEnabled && _flashingDutyTimeRemaining < float.Epsilon)
    {
      return;
    }
    
    _onDraw(this);
  }

  public void Update(float deltaTime)
  {
    if (!ShouldUpdate)
    {
      return;
    }

    if (FlashingEnabled)
    {
      _flashingDutyTimeRemaining -= deltaTime;
      _flashingTimeUntilReset -= deltaTime;
      
      if (_flashingTimeUntilReset < float.Epsilon)
      {
        _flashingDutyTimeRemaining = FlashDuration * FlashingDutyCycle;
        _flashingTimeUntilReset = FlashDuration;
      }
    }
    
    _onUpdate(this, deltaTime);
  }


  public void Dispose()
  {
    ShouldRender = false;
    ShouldUpdate = false;
    FlashingEnabled = false;
    _onDraw = null;
    _onUpdate = null;
  }
}