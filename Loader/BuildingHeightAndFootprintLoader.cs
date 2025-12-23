using Game;
using Unity.Entities;

namespace BuildingHeightAndFootprint.Loader
{
    /// <summary>
    /// Minimal loader/bridge for the BuildingHeightAndFootprint mod.
    /// - Ensures the BuildingHeightAndFootprintSystem is present in the world.
    /// - Exposes a helper to get the current stats as formatted text
    ///   for any UI code that wants to display it.
    /// </summary>
    public sealed class BuildingHeightAndFootprintLoader
    {
        /// <summary>
        /// Call this once from your mod's OnLoad (or equivalent) to make
        /// sure the BuildingHeightAndFootprintSystem is created and running.
        /// 
        /// Example (inside your Mod.OnLoad):
        ///   BuildingHeightAndFootprintLoader.EnsureSystem(updateSystem);
        /// </summary>
        public static void EnsureSystem(UpdateSystem updateSystem)
        {
            if (updateSystem == null)
                return;

            World world = updateSystem.World;
            if (world == null)
                return;

            world.GetOrCreateSystemManaged<BuildingHeightAndFootprintSystem>();
        }

        /// <summary>
        /// Convenience helper for UI/overlay code:
        /// returns a multi-line string describing the current selection,
        /// based on the data maintained by BuildingHeightAndFootprintSystem.
        /// 
        /// Example:
        ///   myLabel.text = BuildingHeightAndFootprintLoader.GetCurrentStatsText();
        /// </summary>
        public static string GetCurrentStatsText()
        {
            return BuildingHeightAndFootprintSystem.FormatCurrentStatsForUi();
        }
    }
}
