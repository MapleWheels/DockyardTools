using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

public class SignalCounter : ItemComponent, IClientSerializable, IServerSerializable
{
    // symbols
    private const string S_ADVNEXT_IN = "advancenext";
    private const string S_OUTPUTPREFIX = "output";

    // cvars
    [InGameEditable, Serialize(0, IsPropertySaveable.Yes, "How many outputs are active? (1~8)")]
    public int NumberOfActiveOutputs
    {
        get => _activeOutputs;
        set => _activeOutputs = Math.Clamp(value, 1, 8);
    }
    private int _activeOutputs;

    [InGameEditable, Serialize(1f, IsPropertySaveable.Yes, "Cooldown time between switching turrets.")]
    public float SwitchTime
    {
        get => _switchTime;
        set => _switchTime = value < float.Epsilon ? 0f : value;
    }
    private float _switchTime;
    private float _remainingSwitchTime;
    
    // internal vars
    private int _counterVal;

    public SignalCounter(Item item, ContentXElement element) : base(item, element)
    {
        isActive = true;
    }

    public override void ReceiveSignal(Signal signal, Connection connection)
    {
        if (_remainingSwitchTime >= float.Epsilon)
            return;
        
        if (connection.Name.Equals(S_ADVNEXT_IN))
        {
            if (signal.value == "0")
                return;
            _counterVal++;
            if (_counterVal >= NumberOfActiveOutputs)
                _counterVal = 0;
            _remainingSwitchTime = SwitchTime;
            SendNetworkUpdateEvent();
            SendSignalUpdates();
        }
    }

    public override void Update(float deltaTime, Camera cam)
    {
        if (_remainingSwitchTime < float.Epsilon) 
            return;
        _remainingSwitchTime -= deltaTime;
        if (_remainingSwitchTime < float.Epsilon)
            _remainingSwitchTime = 0f;
    }

    public override void OnMapLoaded()
    {
        _counterVal = 0;
        _remainingSwitchTime = SwitchTime;
        SendSignalUpdates();
    }

    private void SendSignalUpdates()
    {
        for (int i = 0; i < NumberOfActiveOutputs; i++)
        {
            item.SendSignal(i == _counterVal ? "1" : "0", $"{S_OUTPUTPREFIX}{i}");
        }
    }

    private void ReadData(IReadMessage msg)
    {
        _counterVal = msg.ReadInt32();
        _activeOutputs = msg.ReadInt32();
    }

    private void WriteData(IWriteMessage msg)
    {
        msg.WriteInt32(_counterVal);
        msg.WriteInt32(_activeOutputs);
    }

    public void ClientEventWrite(IWriteMessage msg, NetEntityEvent.IData extraData = null)
    {
        WriteData(msg);
    }

    public void ClientEventRead(IReadMessage msg, float sendingTime)
    {
        ReadData(msg);
        SendSignalUpdates();
    }

    public void ServerEventRead(IReadMessage msg, Client c)
    {
        ReadData(msg);
        SendNetworkUpdateEvent();
        SendSignalUpdates();
    }

    public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
    {
        WriteData(msg);
    }
    
    private void SendNetworkUpdateEvent()
    {
#if SERVER
        if (GameMain.Server is not null)
            item.CreateServerEvent(this);
#elif CLIENT
        if (GameMain.Client is not null)
            item.CreateClientEvent(this);
#endif
    }
}