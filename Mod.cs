using BuildingHeightAndFootprint.Loader;
using BuildingHeightAndFootprint.Systems.UI;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;

namespace BuildingHeightAndFootprint
{
    public class Mod : IMod
    {
        public static readonly ILog log =
            LogManager
                .GetLogger($"{nameof(BuildingHeightAndFootprint)}.{nameof(Mod)}")
                .SetShowsErrorsInUI(false);

        public static ModSettings Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            // Settings (use whatever version you already had wired)
            Settings = new ModSettings(this);
            ((ModSetting)Settings).RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(
                ModAssemblyInfo.Name,
                Settings,
                new ModSettings(this)
            );

            // Make sure the sim system exists (optional but explicit)
            BuildingHeightAndFootprintLoader.EnsureSystem(updateSystem);

            // Simulation-side system (does height/floor/footprint math)
            updateSystem.UpdateAt<BuildingHeightAndFootprintSystem>(SystemUpdatePhase.GameSimulation);
            log.Info($"{nameof(BuildingHeightAndFootprintSystem)} registered in {SystemUpdatePhase.GameSimulation}");

            // UI-side system (exposes bindings to React UI)
            updateSystem.UpdateAt<BuildingHeightAndFootprintUISystem>(SystemUpdatePhase.UIUpdate);
            log.Info($"{nameof(BuildingHeightAndFootprintUISystem)} registered in {SystemUpdatePhase.UIUpdate}");
        }

        public void OnDispose()
        {
            log.Info($"{nameof(Mod)}.{nameof(OnDispose)} called");
        }
    }
}
