using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

public partial class DoorController : ItemComponent, IClientSerializable, IServerSerializable
{
    public void ClientEventWrite(IWriteMessage msg, NetEntityEvent.IData extraData = null)
    {
        msg.WriteBoolean(IsOpen);
    }

    public void ClientEventRead(IReadMessage msg, float sendingTime)
    {
        IsOpen = msg.ReadBoolean();
        item.SendSignal(IsOpen ? "1" : "0", S_DOORSTATE);
    }

    private void SendNetworkEvent()
    {
        item.CreateClientEvent(this);
    }
}