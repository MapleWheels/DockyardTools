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

    // internal vars
    private int _counterVal;
    private bool _updateOutputs;
    private readonly SimpleSynchroHelper<SignalCounter> _networkHelper;

    public SignalCounter(Item item, ContentXElement element) : base(item, element)
    {
        this._networkHelper = new(this, ReadData, WriteData);
        this._networkHelper.TicksBetweenUpdates = 4;    // 15 net-tps
    }

    public override void ReceiveSignal(Signal signal, Connection connection)
    {
        if (connection.Name.Equals(S_ADVNEXT_IN))
        {
            _counterVal++;
            if (_counterVal >= NumberOfActiveOutputs)
                _counterVal = 0;
            _networkHelper.NetworkUpdateReady();
        }

        _updateOutputs = true;
    }

    public override void OnMapLoaded()
    {
        _updateOutputs = true;
    }

    private void SendSignalUpdates()
    {
        for (int i = 0; i < NumberOfActiveOutputs; i++)
        {
            item.SendSignal(i == _counterVal ? "1" : "0", $"{S_OUTPUTPREFIX}{i}");
        }

        _updateOutputs = false;
    }

    public override void Update(float deltaTime, Camera cam)
    {
        if (_updateOutputs)
            SendSignalUpdates();
        _networkHelper.DelayedNetworkUpdate();
    }

    private void ReadData(IReadMessage msg)
    {
        _activeOutputs = msg.ReadInt32();
    }

    private void WriteData(IWriteMessage msg)
    {
        msg.WriteInt32(_activeOutputs);
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
        _networkHelper.SendUpdateNextTick();
    }

    public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
    {
        _networkHelper.WriteData(msg);
    }
}