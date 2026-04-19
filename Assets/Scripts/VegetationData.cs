public struct VegetationData
{
    public float ignitionChance;
    public float burnDuration;
    public float spreadMultiplier;
    public float moisture;

    public static VegetationData Get(VegetationType type)
    {
        return type switch
        {
            VegetationType.Grass => new VegetationData
            {
                ignitionChance = 0.85f,
                burnDuration = 4f,
                spreadMultiplier = 1.8f,
                moisture = 0.1f
            },
            VegetationType.Shrub => new VegetationData
            {
                ignitionChance = 0.65f,
                burnDuration = 10f,
                spreadMultiplier = 1.3f,
                moisture = 0.25f
            },
            VegetationType.Forest => new VegetationData
            {
                ignitionChance = 0.35f,
                burnDuration = 25f,
                spreadMultiplier = 0.9f,
                moisture = 0.5f
            },
            VegetationType.Rock => new VegetationData
            {
                ignitionChance = 0f,
                burnDuration = 0f,
                spreadMultiplier = 0f,
                moisture = 1f
            },
            _ => default
        };
    }
}