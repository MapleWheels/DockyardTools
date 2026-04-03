using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools
{
  public class LinkedStructureRepair : Powered, IClientSerializable, IServerSerializable
  {
    private const string S_CURRENT_HEAL_RATE_PERCENT = "CurrentHealRatePercent";
    
    private ImmutableArray<Structure> LinkedStructures { get; set; } 
    private float _intervalTimeRemaining;
    private bool _isReady = false;
    private SimpleSynchroHelper<LinkedStructureRepair> _networkHelper;
    /// <summary>
    /// Healing rate per second at full strength.
    /// </summary>
    [Editable(0.01f, 1f, 2), Serialize(0.05f, IsPropertySaveable.Yes, "Heal rate in % health per second.")]
    public float HealRate { get; set; }

    [Editable(0.1f, 1f, 1), Serialize(1f, IsPropertySaveable.Yes, "Time in seconds between healing checks (does not affect rate).")]
    public float IntervalTime { get; set; }
    
    public LinkedStructureRepair(Item item, ContentXElement element) : base(item, element)
    {
      _networkHelper = new SimpleSynchroHelper<LinkedStructureRepair>(this, ReadMessage, WriteMessage);
      _networkHelper.TicksBetweenUpdates = 10;
      this.IsActive = true;
    }

    private void WriteMessage(IWriteMessage msg)
    {
      msg.WriteBoolean(_isReady);
    }

    private void ReadMessage(IReadMessage msg)
    {
      _isReady = msg.ReadBoolean();
    }

    public override void OnMapLoaded()
    {
      base.OnMapLoaded();
      LinkedStructures = item.linkedTo
        .Where(i => i is Structure)
        .Cast<Structure>()
        .Where(s => !s.Indestructible)
        .ToImmutableArray();
      _networkHelper.NetworkUpdateReady();
    }

    public override void Update(float deltaTime, Camera cam)
    {
      if (LinkedStructures.IsDefaultOrEmpty || !IsActive || !HasPower)
      {
        if (_isReady)
        {
          _isReady = false;
          _networkHelper.NetworkUpdateReady();
          _networkHelper.ImmediateNetworkUpdate();
        }
      }
      else
      {
        if (!_isReady)
        {
          _isReady = true;
          _networkHelper.NetworkUpdateReady();
          _networkHelper.ImmediateNetworkUpdate();
        }
      }
      
      _networkHelper.DelayedNetworkUpdate();
      
      if (!_isReady)
      {
        item.SendSignal(S_CURRENT_HEAL_RATE_PERCENT, $"0%/s");
        return;
      }
      
      item.SendSignal((HealRate * 100f * Math.Clamp(Voltage, 0f, 2f)).ToString("F2") + "%/s", S_CURRENT_HEAL_RATE_PERCENT);
      
      _intervalTimeRemaining -= deltaTime;
      if (_intervalTimeRemaining <= 0)
      {
        _intervalTimeRemaining += IntervalTime;
        ApplyHealing();
      }
    }

    public void ApplyHealing()
    {
      float healFraction = HealRate * IntervalTime * Math.Clamp(Voltage, 0f, 2f);
      foreach (var structure in LinkedStructures)
      {
        for (int sectionIndex = 0; sectionIndex < structure.Sections.Length; sectionIndex++)
        {
          var section = structure.Sections[sectionIndex];
          if (section.damage > float.Epsilon)
          {
            structure.AddDamage(sectionIndex, -structure.MaxHealth * healFraction);
          }
        }
      }
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
      _networkHelper.NetworkUpdateReady();
      _networkHelper.ImmediateNetworkUpdate();
    }

    public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
    {
      _networkHelper.WriteData(msg);
    }
  }
}