using Barotrauma.Networking;

namespace DockyardTools;

public partial class DroneWifiDispatcher : IServerSerializable
{
    public void ClientEventRead(IReadMessage msg, float sendingTime)
    {
        DroneId = msg.ReadInt32();
        SendIdChannels();
    }
}