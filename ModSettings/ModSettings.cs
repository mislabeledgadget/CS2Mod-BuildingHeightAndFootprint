using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Globalization;

namespace BuildingHeightAndFootprint
{
    [FileLocation(nameof(BuildingHeightAndFootprint))]
    public class ModSettings : ModSetting
    {
        public enum HeightUnitKind
        {
            Feet,
            Meters
        }

        private const float MetersToFeet = 3.28084f;

        public ModSettings(IMod mod) : base(mod) { }

        public override void SetDefaults()
        {
            HeightUnit = HeightUnitKind.Feet;
            SeaLevelBaselineMeters = 0;
        }

        // --------------------
        // General
        // --------------------

        [SettingsUISection("General")]
        public HeightUnitKind HeightUnit { get; set; }

        // --------------------
        // Elevation
        // --------------------

        /// <summary>
        /// Sea level baseline in WORLD meters (world Y = 0 ft reference).
        /// </summary>
        [SettingsUISection("Elevation")]
        [SettingsUISlider(
            min = -1000f,
            max = 1000f,
            step = 1f,
            scalarMultiplier = 1f,
            unit = Unit.kInteger
        )]
        public int SeaLevelBaselineMeters { get; set; }

        /// <summary>
        /// Read-only helper text showing the feet equivalent.
        /// </summary>
        [SettingsUISection("Elevation")]
        public string SeaLevelBaselineFeetDisplay =>
            $"≈ {(SeaLevelBaselineMeters * MetersToFeet).ToString("F1", CultureInfo.InvariantCulture)} ft";
    }
}
