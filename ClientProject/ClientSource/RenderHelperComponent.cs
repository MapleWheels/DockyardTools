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
  public float FlashingTimeUntilReset;
  /// <summary>
  /// The ON/duty time remaining in the current flashing cycle.
  /// </summary>
  public float FlashingDutyTimeRemaining;
  
  public RenderHelperComponent(Action<RenderHelperComponent> onDraw, Action<RenderHelperComponent, float> onUpdate, bool shouldRender = true, bool shouldUpdate = true)
  {
    _onDraw = onDraw;
    _onUpdate = onUpdate;
    ShouldRender = shouldRender;
    ShouldUpdate = shouldUpdate;
  }

  public void Draw()
  {
    if (!ShouldRender)
    {
      return;
    }

    if (FlashingEnabled && FlashingDutyTimeRemaining < float.Epsilon)
    {
      return;
    }
    
    _onDraw?.Invoke(this);
  }

  public void Update(float deltaTime)
  {
    if (!ShouldUpdate)
    {
      return;
    }

    if (FlashingEnabled)
    {
      FlashingDutyTimeRemaining -= deltaTime;
      FlashingTimeUntilReset -= deltaTime;
      
      if (FlashingTimeUntilReset < float.Epsilon)
      {
        FlashingDutyTimeRemaining = FlashDuration * FlashingDutyCycle;
        FlashingTimeUntilReset = FlashDuration;
      }
    }
    
    _onUpdate?.Invoke(this, deltaTime);
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