using Barotrauma.Items.Components;

namespace DockyardTools;

public partial class VerticalEngine : Engine
{
    #region REIMPLEMENTATIONS

    new void UpdateAnimation(float deltaTime)
    {
        if (propellerSprite == null) { return; }
        spriteIndex += (force / 100.0f) * AnimSpeed * deltaTime;
        if (spriteIndex < 0)
        {
            spriteIndex = propellerSprite.FrameCount;
        }
        if (spriteIndex >= propellerSprite.FrameCount)
        {
            spriteIndex = 0.0f;
        }
    }

    #endregion
}