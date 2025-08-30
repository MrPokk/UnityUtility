using System;

public sealed class AIBrain : IBrain
{
    public AIRuntimeParameters RuntimeParameters { get; private set; }
    public AIParametersConfig Config { get; private set; }

    public AIBrain(AIParametersConfig config, AIRuntimeParameters runtimeParameters)
    {
        Config = config;
        RuntimeParameters = runtimeParameters;
    }
}

public interface IBrain
{
    public AIRuntimeParameters RuntimeParameters { get; }
    public AIParametersConfig Config { get; }
}
