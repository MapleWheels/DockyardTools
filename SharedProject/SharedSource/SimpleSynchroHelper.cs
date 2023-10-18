using System.Diagnostics.CodeAnalysis;
using Barotrauma.Items.Components;
using Barotrauma.Networking;

namespace DockyardTools;

public class SimpleSynchroHelper<T> where T : ItemComponent, IClientSerializable, IServerSerializable
{
    protected event System.Action<IReadMessage> _clientEventRead, _serverEventRead; 
    protected event System.Action<IWriteMessage> _clientEventWrite, _serverEventWrite; 
    protected readonly T _component;
    protected bool _unsentChanges;
    protected int TicksUntilNextNetworkUpdate;
    private int _ticksBetweenUpdates;
    public int TicksBetweenUpdates
    {
        get => _ticksBetweenUpdates;
        set => _ticksBetweenUpdates = Math.Clamp(value, 1, 10); // never more than 10 ticks
    }

    public SimpleSynchroHelper([NotNull] T component, System.Action<IReadMessage> clientServerEventRead, 
        System.Action<IWriteMessage> clientServerEventWrite)
    {
        _unsentChanges = false;
        (_clientEventRead, _serverEventRead) = (clientServerEventRead, clientServerEventRead);
        (_clientEventWrite, _serverEventWrite) = (clientServerEventWrite, clientServerEventWrite);
        _component = component;
    }
    
    public SimpleSynchroHelper([NotNull] T component, 
        System.Action<IReadMessage> clientEventRead, 
        System.Action<IWriteMessage> clientEventWrite,
        System.Action<IReadMessage> serverEventRead, 
        System.Action<IWriteMessage> serverEventWrite)
    {
        _unsentChanges = false;
        (_clientEventRead, _serverEventRead) = (clientEventRead, serverEventRead);
        (_clientEventWrite, _serverEventWrite) = (clientEventWrite, serverEventWrite);
        _component = component;
    }

    public virtual void NetworkUpdateReady()
    {
        _unsentChanges = true;
    }

    public virtual void ReadData(IReadMessage msg)
    {
#if CLIENT
        _clientEventRead?.Invoke(msg);
#elif SERVER
        _serverEventRead?.Invoke(msg);
#endif
    }

    public virtual void WriteData(IWriteMessage msg)
    {
#if CLIENT
        _clientEventWrite?.Invoke(msg);
#elif SERVER
        _serverEventWrite?.Invoke(msg);
#endif  
    }

    public virtual void NetworkUpdate()
    {
        if (!_unsentChanges)
            return;
        SendNetworkUpdateEvent();
    }

    public void SendUpdateNextTick()
    {
        _unsentChanges = true;
        TicksUntilNextNetworkUpdate = 0;
    }

    public virtual void DelayedNetworkUpdate()
    {
        if (!_unsentChanges)
            return;
        TicksUntilNextNetworkUpdate--;
        if (TicksUntilNextNetworkUpdate < 1)
        {
            SendNetworkUpdateEvent();
            TicksUntilNextNetworkUpdate = TicksBetweenUpdates;
        }
    }

    private void SendNetworkUpdateEvent()
    {
#if SERVER
        if (GameMain.Server is not null)
            _component.item.CreateServerEvent(_component);
#elif CLIENT
        if (GameMain.Client is not null)
            _component.item.CreateClientEvent(_component);
#endif
        _unsentChanges = false;
    }
}