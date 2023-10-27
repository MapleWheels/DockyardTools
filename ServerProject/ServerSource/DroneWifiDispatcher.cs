using Barotrauma.Networking;

namespace DockyardTools;

public partial class DroneWifiDispatcher : IServerSerializable
{
    public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
    {
        msg.WriteInt32(DroneId);
    }
}