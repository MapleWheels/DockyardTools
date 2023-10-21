using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

public partial class DoorController : ItemComponent, IClientSerializable, IServerSerializable
{
    public void ServerEventRead(IReadMessage msg, Client c)
    {
        IsOpen = msg.ReadBoolean();
        item.SendSignal(IsOpen ? "1" : "0", S_DOORSTATE);
        SendNetworkEvent();
    }

    public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
    {
        msg.WriteBoolean(IsOpen);
    }
    
    private void SendNetworkEvent()
    {
        item.CreateServerEvent(this);
    }
}