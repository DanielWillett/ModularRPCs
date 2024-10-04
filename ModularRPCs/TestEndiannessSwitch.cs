#if USE_TEST_ENDIANNESS_SWITCH
namespace DanielWillett.ModularRpcs;
public static class TestEndiannessSwitch
{
    /// <summary>
    /// Used for testing big-endian reading and writing methods
    /// </summary>
    public static bool IsLittleEndian = false;// System.BitConverter.IsLittleEndian;
}
#endif