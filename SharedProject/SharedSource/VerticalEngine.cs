using Barotrauma.Items.Components;

namespace DockyardTools;

public partial class VerticalEngine : Engine
{
    public VerticalEngine(Item item, ContentXElement element) : base(item, element)
    {
    }
    

    public override void Update(float deltaTime, Camera cam)
    {
        UpdateOnActiveEffects(deltaTime);

        UpdateAnimation(deltaTime);

        controlLockTimer -= deltaTime;

        if (powerConsumption == 0.0f)
        {
            prevVoltage = 1;
            hasPower = true;
        }
        else
        {
            hasPower = Voltage > MinVoltage;
        }

        if (lastReceivedTargetForce.HasValue)
        {
            targetForce = lastReceivedTargetForce.Value;
        }
        Force = MathHelper.Lerp(force, (Voltage < MinVoltage) ? 0.0f : targetForce, deltaTime * 10.0f);
        if (Math.Abs(Force) > 1.0f)
        {
            float voltageFactor = MinVoltage <= 0.0f ? 1.0f : Math.Min(Voltage, MaxOverVoltageFactor);
            float currForce = force * voltageFactor;
            float condition = item.MaxCondition <= 0.0f ? 0.0f : item.Condition / item.MaxCondition;
            // Broken engine makes more noise.
            float noise = Math.Abs(currForce) * MathHelper.Lerp(1.5f, 1f, condition);
            UpdateAITargets(noise);
            //arbitrary multiplier that was added to changes in submarine mass without having to readjust all engines
            float forceMultiplier = 0.1f;
            if (User != null)
            {
                forceMultiplier *= MathHelper.Lerp(0.5f, 2.0f, (float)Math.Sqrt(User.GetSkillLevel("helm") / 100));
            }
            currForce *= item.StatManager.GetAdjustedValue(ItemTalentStats.EngineMaxSpeed, MaxForce) * forceMultiplier;
            if (item.GetComponent<Repairable>() is { IsTinkering: true } repairable)
            {
                currForce *= 1f + repairable.TinkeringStrength * TinkeringForceIncrease;
            }

            currForce = item.StatManager.GetAdjustedValue(ItemTalentStats.EngineSpeed, currForce);

            //less effective when in a bad condition
            currForce *= MathHelper.Lerp(0.5f, 2.0f, condition);
            if (item.Submarine.FlippedX) { currForce *= -1; }
            Vector2 forceVector = new Vector2(0, -currForce);   //invert direction to make it match the navterminal steering output
            item.Submarine.ApplyForce(forceVector * deltaTime * Timing.FixedUpdateRate);
            UpdatePropellerDamage(deltaTime);
#if CLIENT
            float particleInterval = 1.0f / particlesPerSec;
            particleTimer += deltaTime;
            while (particleTimer > particleInterval)
            {
                Vector2 particleVel = -forceVector.ClampLength(5000.0f) / 5.0f;
                GameMain.ParticleManager.CreateParticle("bubbles", item.WorldPosition + PropellerPos * item.Scale,
                    particleVel * Rand.Range(0.8f, 1.1f),
                    0.0f, item.CurrentHull);
                particleTimer -= particleInterval;
            }
#endif
        }
    }
}