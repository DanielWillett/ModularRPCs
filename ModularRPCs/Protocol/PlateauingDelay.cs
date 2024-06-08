using System;

namespace DanielWillett.ModularRpcs.Protocol;

/// <summary>
/// Utility to create a 'plateauing' delay that settles on a given number. Good for attempting reconnects with a greater spacing out.
/// </summary>
/// <remarks>
/// Use the following 4 formulas in <see href="https://www.desmos.com/calculator">Desmos</see> to see the effects of each value.
///
/// <code>
/// * \min\left(a^{d}x^{\frac{1}{d}}+s,m\right)\left\{x>0\right\}
/// * s='Start'
/// * a='Amplifier'
/// * m='Maximum'
/// * d='Climb'
/// </code>
///
/// </remarks>
public struct PlateauingDelay
{
    private readonly double _ampPwrToClimb;

    /// <summary>
    /// Offset of the delay, meaning the minimum/start value in <c>seconds</c>.
    /// </summary>
    public readonly double Start;

    /// <summary>
    /// How quickly the delay follows the curve in <c>seconds^(-<see cref="Climb"/>)</c>.
    /// </summary>
    public readonly double Amplifier;

    /// <summary>
    /// Hard maximum value in <c>seconds</c>. Ignored if less than zero.
    /// </summary>
    public readonly double Maximum;

    /// <summary>
    /// How sharp the curve is.
    /// </summary>
    public readonly double Climb;

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

    public PlateauingDelay(ref PlateauingDelay other, bool reset)
    {
        ref PlateauingDelay r = ref this;
        r = other;
        if (reset)
        {
            Reset();
        }
    }

    /// <summary>
    /// Create a new <see cref="PlateauingDelay"/> with the given parameters. Defaults are parameters for a good climb for a 5 minute maximum.
    /// </summary>
    /// <param name="amplifier">How quickly the delay follows the curve in <c>seconds^(-<paramref name="climb"/>)</c>.</param>
    /// <param name="climb">How sharp the curve is.</param>
    /// <param name="maximum">Hard maximum value in <c>seconds</c>. Ignored if less than zero.</param>
    /// <param name="start">Offset of the delay, meaning the minimum/start value in <c>seconds</c>.</param>
    /// <param name="startingTrials">The origin value of <see cref="Trials"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startingTrials"/> is less than 0 or <paramref name="amplifier"/> or <paramref name="climb"/> is less than or equal to 0.</exception>
    public PlateauingDelay(double amplifier = 6, double climb = 2.5, double maximum = 300, double start = 10, int startingTrials = 0)
    {
        if (startingTrials < 0)
            throw new ArgumentOutOfRangeException(nameof(startingTrials));

        if (amplifier <= 0)
            throw new ArgumentOutOfRangeException(nameof(amplifier));
        
        if (climb <= 0)
            throw new ArgumentOutOfRangeException(nameof(climb));
        
        Amplifier = amplifier;
        Climb = climb;
        Maximum = maximum;
        Start = start;
        StartingTrials = startingTrials;
        Trials = startingTrials;
        _ampPwrToClimb = Math.Pow(Amplifier, Climb);
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
    /// Calculate the next delay and increment <see cref="Trials"/> in <c>seconds</c>.
    /// </summary>
    public double CalculateNext()
    {
        double val = Calculate(++Trials);
        Value = val;
        return val;
    }

    /// <summary>
    /// Calculate the delay for a given number of trials in <c>seconds</c>.
    /// </summary>
    public readonly double Calculate(int trials)
    {
        if (trials <= 0)
            return 0;

        // (a^c) * (x^(1/c)) + s
        double val = _ampPwrToClimb * Math.Pow(trials, 1d / Climb) + Start;
        return Maximum < 0 ? val : Math.Min(val, Maximum);
    }
}