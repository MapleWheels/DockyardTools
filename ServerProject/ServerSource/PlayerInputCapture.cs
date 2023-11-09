using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

public partial class PlayerInputCapture : IServerSerializable, IClientSerializable
{
    public void ServerEventWrite(IWriteMessage msg, Client c, NetEntityEvent.IData extraData = null)
    {
        _networkHelper.WriteData(msg);
    }

    public void ServerEventRead(IReadMessage msg, Client c)
    {
        _networkHelper.ReadData(msg);
        _networkHelper.NetworkUpdateReady();
        _networkHelper.ImmediateNetworkUpdate();
    }
}