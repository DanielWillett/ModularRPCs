using System;

namespace DanielWillett.ModularRpcs.Protocol;

/// <summary>
/// Utility to create a 'plateauing' delay that settles on a given number. Good for attempting reconnects with a greater spacing out.
/// </summary>
/// <remarks>
/// Use the following 4 formulas in <see href="https://www.desmos.com/calculator">Desmos</see> to see the effects of each value.
///
/// <code>
/// * \min\left(\sqrt[3]{\left(m^{a}x-m\right)}+m^{\frac{1}{3}},m\right)\left\{x\ >\ 0\right\}
/// * m='Plateau'
/// * a='Amplifier'
/// * y=m
/// </code>
///
/// </remarks>
public struct PlateauingDelay
{
    private readonly double _cubeRootOfPlateau;

    /// <summary>
    /// This is the maximum amount of time between any two events.
    /// </summary>
    public readonly double Plateau;

    /// <summary>
    /// This number affects how quickly the delay ramps up. Negative values ramp up slower than positive values. This value will usually stay within <c>[-5, 5]</c>.
    /// </summary>
    public readonly double Amplifier;

    /// <summary>
    /// The origin value of <see cref="Trials"/>.
    /// </summary>
    public readonly int StartingTrials;

    /// <summary>
    /// The last calculated delay.
    /// </summary>
    public double Value { get; private set; }

    /// <summary>
    /// Number of trials since the last reset.
    /// </summary>
    public int Trials { get; private set; }

    public PlateauingDelay(double plateau, double amplifier, int startingTrials = 0)
    {
        if (startingTrials < 0)
            throw new ArgumentOutOfRangeException(nameof(startingTrials));

        if (plateau <= 0)
            throw new ArgumentOutOfRangeException(nameof(plateau));

        Plateau = plateau;
        _cubeRootOfPlateau = Math.Pow(plateau, 1d / 3d);
        Amplifier = amplifier;
        StartingTrials = startingTrials;
        Trials = startingTrials;
        Calculate(startingTrials);
    }

    /// <summary>
    /// Reset <see cref="Trials"/> to <see cref="StartingTrials"/>.
    /// </summary>
    public void Reset()
    {
        int trials = StartingTrials;
        Trials = trials;
        Value = Calculate(trials);
    }

    /// <summary>
    /// Calculate the next delay and increment <see cref="Trials"/>.
    /// </summary>
    public double CalculateNext()
    {
        double val = Calculate(++Trials);
        Value = val;
        return val;
    }
    private double Calculate(int trials)
    {
        if (trials <= 0)
            return 0;

        // min( 3rt(p^a * t - p) + 3rt(p) , p )
        return Math.Min(
            Math.Pow(
                Math.Pow(Plateau, Amplifier) * Trials - Trials,
                1d / 3d
            )
            + _cubeRootOfPlateau,
            Plateau
        );
    }
}