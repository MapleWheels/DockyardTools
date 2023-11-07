using Barotrauma.Items.Components;

namespace DockyardTools;

public static class GUICloseOverride
{
    private static readonly ConcurrentDictionary<ItemComponent, Func<bool>> _guiCloseHUDOverride = new();
    
    public static bool Post_CloseHUD(bool result)
    {
        if (PlayerInput.KeyHit(Microsoft.Xna.Framework.Input.Keys.Escape)) { return true; }

        bool overrideOnFalse = true;
        if (_guiCloseHUDOverride.Any())
        {
            foreach (KeyValuePair<ItemComponent,Func<bool>> pair in _guiCloseHUDOverride)
            {
                if (pair.Value?.Invoke() ?? false)
                {
                    overrideOnFalse = false;
                    break;
                }
            }
        }

        return result & overrideOnFalse;
    }

    public static void Subscribe(ItemComponent component, Func<bool> handle)
    {
        if (!_guiCloseHUDOverride.ContainsKey(component))
            _guiCloseHUDOverride.TryAdd(component, handle);
    }

    public static void Unsubscribe(ItemComponent component)
    {
        _guiCloseHUDOverride.Remove(component, out _);
    }
    
    public static void Dipose()
    {
        _guiCloseHUDOverride.Clear();
    }
}