using Barotrauma.Items.Components;
using Barotrauma.LuaCs;
using Barotrauma.Networking;
using MoonSharp.Interpreter;

namespace DockyardTools
{
  public partial class LuaComponent : PowerContainer
  {
    [Editable, Serialize("", IsPropertySaveable.No, "Name of lua table that contains your functions.")]
    public string FunctionTableName { get; set; }
    
    [Editable, Serialize(false, IsPropertySaveable.No, $"Run {nameof(OnItemLoaded)}()?")]
    public bool RunOnItemLoaded { get; set; }
    
    [Editable, Serialize(false, IsPropertySaveable.No, $"Run {nameof(OnMapLoaded)}()?")]
    public bool RunOnMapLoaded { get; set; }
    
    [Editable, Serialize(false, IsPropertySaveable.No, $"Run {nameof(Update)}()?")]
    public bool RunOnUpdate { get; set; }
    
    [Editable, Serialize(false, IsPropertySaveable.No, $"Run {nameof(ReceiveSignal)}()?")]
    public bool RunOnReceiveSignal { get; set; }
    
    [Editable, Serialize(false, IsPropertySaveable.No, $"Run {nameof(GetConnectionPowerOut)}()?")]
    public bool RunOnGetConnectionPowerOut { get; set; }
    
    [Editable, Serialize(false, IsPropertySaveable.No, $"Run OnNetworkSignalReceived()?")]
    public bool RunOnNetworkSignalReceived { get; set; }
    
    [Editable, Serialize(NetSync.None, IsPropertySaveable.No, "Network sync mode.")]
    public NetSync NetSyncMode { get; set; }
    
    private record SignalMessage(string Id, string Value);
    
    public ConcurrentDictionary<string,object> LuaStore { get; } = new();
    private bool ReadyToRun => !FunctionTableName.IsNullOrWhiteSpace();
    public readonly string NullSignalId = "null";
    private readonly ConcurrentQueue<SignalMessage> _outgoingNetworkMessages = new();

    private object _itemLoadedFunc, _mapLoadedFunc, _updateFunc, _receiveSignalFunc, _networkSignalReceivedFunc,
      _getConnectionPowerOutFunc;
    private readonly ILuaScriptManagementService _scriptService;
    
    public LuaComponent(Item item, ContentXElement element) : base(item, element)
    {
      _scriptService = DockyardToolsPlugin.Instance.LuaScriptService;
      item.IsActive = true;
    }
    
    public override void OnItemLoaded()
    {
      base.OnItemLoaded();
      if (!ReadyToRun)
      {
        return;
      }

      DynValue? table = null;
      if (FunctionTableName.Contains('.'))
      {
        var tree = FunctionTableName.Split('.');
        foreach (var branch in tree)
        {
          if (table is null)
          {
            table = _scriptService.InternalScript!.Globals.Get(branch);
          }
          else
          {
            table = table.Table.Get(branch);
          }
        }
      }
      else
      {
        table = _scriptService.InternalScript!.Globals.Get(FunctionTableName);
      }
      

      if (RunOnItemLoaded)
      {
        _itemLoadedFunc = table!.Table.Get(nameof(OnItemLoaded));
      }

      if (RunOnMapLoaded)
      {
        _mapLoadedFunc = table!.Table.Get(nameof(OnMapLoaded));
      }

      if (RunOnUpdate)
      {
        _updateFunc = table!.Table.Get(nameof(Update));
      }

      if (RunOnReceiveSignal)
      {
        _receiveSignalFunc = table!.Table.Get(nameof(ReceiveSignal));
      }

      if (RunOnNetworkSignalReceived)
      {
        _networkSignalReceivedFunc = table!.Table.Get("OnNetworkSignalReceived");
      }

      if (RunOnGetConnectionPowerOut)
      {
        _getConnectionPowerOutFunc = table!.Table.Get(nameof(GetConnectionPowerOut));
      }
      
      if (!RunOnItemLoaded)
      {
        return;
      }
      
      _scriptService.CallFunctionSafe(_itemLoadedFunc, this);
    }

    public override void OnMapLoaded()
    {
      base.OnMapLoaded();
      if (!ReadyToRun || !RunOnMapLoaded)
      {
        return;
      }

      _scriptService.CallFunctionSafe(_mapLoadedFunc, this);
    }

    public override void Update(float deltaTime, Camera cam)
    {
      base.Update(deltaTime, cam);
      if (!ReadyToRun || !RunOnUpdate)
      {
        return;
      }

      _scriptService.CallFunctionSafe(_updateFunc, this, deltaTime, cam);
    }

    public override void ReceiveSignal(Signal signal, Connection connection)
    {
      base.ReceiveSignal(signal, connection);
      if (!ReadyToRun || !RunOnReceiveSignal)
      {
        return;
      }

      _scriptService.CallFunctionSafe(_receiveSignalFunc, this, signal, connection);
    }
    
    public void SendNetworkSignal(string signalName, string signalValue)
    {
      _outgoingNetworkMessages.Enqueue(new SignalMessage(signalName, signalValue));
#if CLIENT
      this.Item.CreateClientEvent(this);
#elif SERVER
      this.Item.CreateServerEvent(this);
#endif
    }

    public override float GetConnectionPowerOut(Connection connection, float power, PowerRange minMaxPower, float load)
    {
      if (!ReadyToRun || !RunOnGetConnectionPowerOut)
      {
        return base.GetConnectionPowerOut(connection, power, minMaxPower, load);
      }

      try
      {
        return (float)_scriptService.CallFunctionSafe(_getConnectionPowerOutFunc, this, connection, power, minMaxPower,
          load)!.Number;
      }
      catch
      {
        return base.GetConnectionPowerOut(connection, power, minMaxPower, load);
      }
    }

    private void ReadNetworkData(IReadMessage msg)
    {
      byte msgs = msg.ReadByte();
      for (int i = 0; i < msgs; i++)
      {
        try
        {
          var msgName = msg.ReadString();
          var msgData = msg.ReadString();

#if CLIENT
          if (NetSyncMode is NetSync.None or NetSync.ClientOneWay)
          {
            return;
          }
#elif SERVER
          if (NetSyncMode is NetSync.None or NetSync.ServerAuthority)
          {
            return;
          }
          _outgoingNetworkMessages.Enqueue(new SignalMessage(msgName, msgData));
#endif
          if (ReadyToRun && RunOnNetworkSignalReceived)
          {
            _scriptService.CallFunctionSafe(_networkSignalReceivedFunc, this, msgName, msgData);
          }
        }
        catch (Exception _)
        {
          continue;
        }
      }

#if SERVER
      if (_outgoingNetworkMessages.Count > 0)
      {
        this.Item.CreateServerEvent(this);
      }
#endif
    }

    private void WriteNetworkData(IWriteMessage msg)
    {
      byte count = (byte)_outgoingNetworkMessages.Count;
      if (count < 1)
      {
        return;
      }
      msg.WriteByte(count);
      for (int i = 0; i < count; i++)
      {
        if (_outgoingNetworkMessages.TryDequeue(out var signalMsg))
        {
          msg.WriteString(signalMsg.Id);
          msg.WriteString(signalMsg.Value);
        }
        else
        {
          // write filler
          msg.WriteString(NullSignalId);
          msg.WriteString(NullSignalId);
        }
      }
    }
  }
}