using System.Collections.Specialized;
using Microsoft.Xna.Framework.Graphics;

namespace DockyardTools;

public class NotificationDisplayHelper
{
  public class Notification
  {
    public static Color DefaultAdvisoryColor = Color.White;
    public static Color DefaultWarningColor = Color.Yellow;
    public static Color DefaultDangerColor = Color.Red;
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public static GUIFont DefaultFont = GUIStyle.SmallFont;
    public static float DefaultVerticalSpacing = 10f;
    
    public string Name { get; init; } 
    public MessagePriority Priority { get; init; }
    public string Message { get; init; }
    public float VerticalSpacing { get; init; }
    public Color Color { get; init; } 
    public GUIFont Font { get; init; }
    public float Duration { get; private set; }
    public bool IsEnabled { get; set; }
    public bool IsExpired => Duration is < float.Epsilon and > (InfiniteDurationValue + 1f);
    public bool ShouldRender => IsEnabled && !IsExpired;
    
    public const float InfiniteDurationValue = -999f;

    public Notification(string name, string message, MessagePriority priority)
    {
      if (name.IsNullOrWhiteSpace())
      {
        throw new ArgumentException("Notification name cannot be null or whitespace", nameof(name));
      }
      
      if (message.IsNullOrWhiteSpace())
      {
        throw new ArgumentException("Notification message cannot be null or whitespace", nameof(message));
      }
      
      Name = name;
      Message = message;
      Priority = priority;
      switch (priority)
      {
        case MessagePriority.Advisory: Color = DefaultAdvisoryColor; break;
        case MessagePriority.Warning: Color = DefaultWarningColor; break;
        case MessagePriority.Danger: Color = DefaultDangerColor; break;
      }

      Font = DefaultFont;
      Duration = InfiniteDurationValue;
      VerticalSpacing = DefaultVerticalSpacing;
      IsEnabled = true;
    }
    
    public Notification(string name, string message, MessagePriority priority, Color color, GUIFont font, float duration = InfiniteDurationValue, float verticalSpacing = 10f)
    {
      if (name.IsNullOrWhiteSpace())
      {
        throw new ArgumentException("Notification name cannot be null or whitespace", nameof(name));
      }

      if (message.IsNullOrWhiteSpace())
      {
        throw new ArgumentException("Notification message cannot be null or whitespace", nameof(message));
      }
      Name = name;
      Message = message;
      Priority = priority;
      Color = color;
      Font = font;
      Duration = duration;
      VerticalSpacing = verticalSpacing;
      IsEnabled = true;
    }
    
    public enum MessagePriority
    {
      Danger = 0,
      Warning,
      Advisory
    }

    public void SetDuration(float duration)
    {
      if (duration <= InfiniteDurationValue)
      {
        Duration = InfiniteDurationValue;
        return;
      }
      
      Duration = duration;
    }
    
    public void Update(float deltaTime)
    {
      if (Duration < float.Epsilon)
      {
        return;
      }
      
      Duration = Math.Max(Duration - deltaTime, 0f);
    }
  }

  public readonly ConcurrentDictionary<string, Notification> Notifications = new();
  public readonly ConcurrentDictionary<Notification.MessagePriority, List<Notification>> NotificationsByPriority = new();
 
  public float MaxHeight { get; set; } = 500f;
  public bool DrawPastMaxHeight { get; set; } = false;
  
  public NotificationDisplayHelper()
  {
    foreach (var priority in Enum.GetValues<Notification.MessagePriority>())
    {
      NotificationsByPriority.TryAdd(priority, new List<Notification>());
    }
  }

  public void Draw(SpriteBatch spriteBatch, Vector2 topLeftPosition, out Vector2 finalPosition)
  {
    foreach (var priority in Enum.GetValues<Notification.MessagePriority>())
    {
      if (!NotificationsByPriority.TryGetValue(priority, out var notificationsList))
      {
        continue;
      }

      foreach (var notification in notificationsList)
      {
        if (!notification.ShouldRender)
        {
          continue;
        }
        
        if (topLeftPosition.Y > MaxHeight && !DrawPastMaxHeight)
        {
          break;
        }
        GUI.DrawString(spriteBatch, topLeftPosition, notification.Message, notification.Color, null, 0, notification.Font);
        topLeftPosition.Y += notification.VerticalSpacing;
      }
    }
    
    finalPosition = topLeftPosition;
  }
  
  public void AddMessage(Notification notification)
  {
    if (Notifications.TryAdd(notification.Name, notification))
    {
      NotificationsByPriority[notification.Priority].Add(notification);
    }
  }

  public void RemoveMessage(Notification notification)
  {
    Notifications.TryRemove(notification.Name, out _);
    NotificationsByPriority[notification.Priority].Remove(notification);
  }
  
  public void RemoveMessage(string name)
  {
    Notifications.TryRemove(name, out var notification);
    if (notification != null)
    {
      NotificationsByPriority[notification.Priority].Remove(notification);
    }
  }
  
  public void Update(float deltaTime)
  {
    List<(Notification.MessagePriority Priority, Notification Notification)> toRemove = new();
    
    foreach (var notification in Notifications.Values)
    {
      notification.Update(deltaTime);
      if (notification.IsExpired)
      {
        toRemove.Add((notification.Priority, notification));
      }
    }
    
    foreach (var (priority, notification) in toRemove)
    {
      NotificationsByPriority[priority].Remove(notification);
      Notifications.TryRemove(notification.Name, out _);
    }
  }
}