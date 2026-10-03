namespace Strafer.Core
{
    /// <summary>
    /// Conversion helpers between Source/Quake-style "units per second" (UPS)
    /// and Unity's native meters.
    ///
    /// Movement speeds in this project are authored in UPS because that is the
    /// scale most movement-shooter tuning is discussed in. One Source/Quake unit
    /// equals 0.75 inches, which is 1.905 cm.
    /// </summary>
    public static class StraferUnits
    {
        public const float MetersPerUnit = 0.01905f;

        /// <summary>Converts a speed in Source/Quake units per second to meters per second.</summary>
        public static float UpsToMetersPerSecond(float unitsPerSecond)
        {
            return unitsPerSecond * MetersPerUnit;
        }
    }
}
