using UnityEngine;

// ================================================================
// AnimalData.cs
// ================================================================
// Defineste tipurile de animale si parametrii lor de comportament.
// Fiecare tip are viteza, raza de detectie si reactie diferita.
// ================================================================

public enum AnimalType
{
    Deer,    // Cerb   - detecteaza de departe, fuge rapid si drept
    Boar,    // Mistret - detecteaza tarziu, fuge haotic
    Rabbit,  // Iepure  - detecteaza aproape, fuge rapid in zig-zag
    Wolf,    // Lup     - detecteaza de departe, fuge organizat
    Fox      // Vulpe   - comportament intermediar, inteligenta
}

public enum AnimalState
{
    Wandering,  // Ratacire aleatoare pe teren
    Resting,    // Odihna (animal obosit)
    Fleeing,    // Fuga activa de foc
    Avoiding,   // Evitare preventiva (foc detectat dar departe)
    Dead        // Mort (prins de foc)
}

[System.Serializable]
public struct AnimalStats
{
    public AnimalType type;
    public string displayName;

    public float detectionRadius;   // cat de departe detecteaza focul
    public float criticalRadius;    // distanta la care incepe fuga activa
    public float moveSpeed;         // viteza normala
    public float fleeSpeed;         // viteza de fuga
    public float zigzagIntensity;   // cat de haotic fuge (0 = drept, 1 = mult zig-zag)
    public float staminaMax;        // cat timp poate fugi inainte sa oboseasca
    public float restTime;          // cat timp se odihneste

    // Returneaza parametrii pentru fiecare tip de animal
    public static AnimalStats Get(AnimalType type)
    {
        return type switch
        {
            AnimalType.Deer => new AnimalStats
            {
                type = AnimalType.Deer,
                displayName = "Cerb",
                detectionRadius = 25f,   // detecteaza de foarte departe
                criticalRadius = 12f,
                moveSpeed = 4f,
                fleeSpeed = 10f,   // fuge rapid
                zigzagIntensity = 0.1f,  // fuge aproape drept
                staminaMax = 15f,
                restTime = 3f
            },
            AnimalType.Boar => new AnimalStats
            {
                type = AnimalType.Boar,
                displayName = "Mistret",
                detectionRadius = 10f,   // detecteaza tarziu
                criticalRadius = 6f,
                moveSpeed = 3f,
                fleeSpeed = 7f,    // fuge mai lent
                zigzagIntensity = 0.8f,  // fuge haotic
                staminaMax = 20f,   // rezistenta mare
                restTime = 2f
            },
            AnimalType.Rabbit => new AnimalStats
            {
                type = AnimalType.Rabbit,
                displayName = "Iepure",
                detectionRadius = 15f,
                criticalRadius = 8f,
                moveSpeed = 5f,
                fleeSpeed = 14f,   // cel mai rapid
                zigzagIntensity = 0.9f,  // zig-zag intens
                staminaMax = 8f,    // oboseste repede
                restTime = 4f
            },
            AnimalType.Wolf => new AnimalStats
            {
                type = AnimalType.Wolf,
                displayName = "Lup",
                detectionRadius = 30f,   // cel mai bun simtz
                criticalRadius = 15f,
                moveSpeed = 5f,
                fleeSpeed = 11f,
                zigzagIntensity = 0.05f, // fuge foarte drept, organizat
                staminaMax = 25f,   // cea mai mare rezistenta
                restTime = 2f
            },
            AnimalType.Fox => new AnimalStats
            {
                type = AnimalType.Fox,
                displayName = "Vulpe",
                detectionRadius = 20f,
                criticalRadius = 10f,
                moveSpeed = 4.5f,
                fleeSpeed = 9f,
                zigzagIntensity = 0.4f,
                staminaMax = 12f,
                restTime = 3f
            },
            _ => default
        };
    }
}