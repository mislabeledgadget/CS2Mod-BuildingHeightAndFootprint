using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Globalization;
using BuildingHeightAndFootprint.Systems; // Units

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

        public ModSettings(IMod mod) : base(mod) { }

        public override void SetDefaults()
        {
            HeightUnit = HeightUnitKind.Feet;
            SeaLevelOffsetMeters = 0;
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
        /// Sea level offset in WORLD meters.
        /// This is applied as a correction to the computed elevation.
        /// Accepts positive or negative values.
        /// </summary>
        [SettingsUISection("Elevation")]
        [SettingsUISlider(
            min = -1000f,
            max = 1000f,
            step = 1f,
            scalarMultiplier = 1f,
            unit = Unit.kInteger
        )]
        public int SeaLevelOffsetMeters { get; set; }

        /// <summary>
        /// Button: resets SeaLevelOffsetMeters back to 0.
        /// CS2 Options UI calls the setter as the button action.
        /// </summary>
        [SettingsUISection("Elevation")]
        [SettingsUIButton]
        public bool ResetSeaLevelOffset
        {
            set
            {
                SeaLevelOffsetMeters = 0;
            }
        }

        /// <summary>
        /// Read-only helper text showing the feet equivalent.
        /// </summary>
        [SettingsUISection("Elevation")]
        public string SeaLevelOffsetFeetDisplay =>
            $"≈ {(SeaLevelOffsetMeters * Units.MetersToFeet).ToString("F1", CultureInfo.InvariantCulture)} ft";
    }
}
