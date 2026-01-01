using System.Text;
using BuildingHeightAndFootprint.Systems;
using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;

namespace BuildingHeightAndFootprint
{
    /// <summary>
    /// Selection-driven building stats:
    /// - Only runs this when the selected entity changes.
    /// - Uses instance geometry first, then prefab geometry, to estimate height, floors, and footprint.
    /// - Stores height internally in feet.
    /// - Exposes CurrentStats for UI/overlay code.
    /// - Exposes CurrentLayoutKind ("Level" / "Description" / "") for UI layout decisions.
    /// </summary>
    public partial class BuildingHeightAndFootprintSystem : GameSystemBase
    {
        public struct BuildingStats
        {
            public Entity Entity;

            /// <summary>Height in feet (internal storage).</summary>
            public float HeightFeet;

            /// <summary>Estimated number of floors.</summary>
            public int Floors;

            /// <summary>Total approximate zoning cells occupied by the footprint.</summary>
            public int FootprintCells;

            /// <summary>Approximate footprint width in cells.</summary>
            public int FootprintWidthCells;

            /// <summary>Approximate footprint depth in cells.</summary>
            public int FootprintDepthCells;

            /// <summary>Lowest point of building bounds in feet.</summary>
            public float BaseElevationFeet;

            /// <summary>
            /// Approximate footprint area in acres (always stored in acres;
            /// converted to ha for metric display).
            /// </summary>
            public float FootprintAcres;

            public bool HasData;
        }

        /// <summary>
        /// Latest stats for the currently selected building (if any).
        /// </summary>
        public static BuildingStats CurrentStats;

        /// <summary>
        /// Layout hint for the UI:
        /// - "Level"       → zonable buildings (use LevelSection)
        /// - "Description" → ploppables / services / uniques (use DescriptionSection)
        /// - ""            → no valid building selected
        /// </summary>
        public static string CurrentLayoutKind { get; private set; } = string.Empty;

        private ToolSystem _toolSystem;
        private Entity _lastSelected;
        private ILog _log;

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = Mod.log;
            _log.Info($"{nameof(BuildingHeightAndFootprintSystem)}.{nameof(OnCreate)}");

            // ToolSystem lives in the tools/UI world, not the sim world
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _toolSystem = world.GetExistingSystemManaged<ToolSystem>()
                    ?? world.GetOrCreateSystemManaged<ToolSystem>();
            }

            _lastSelected = Entity.Null;

            CurrentStats = new BuildingStats
            {
                Entity = Entity.Null,
                HeightFeet = 0f,
                Floors = 0,
                FootprintCells = 0,
                FootprintWidthCells = 0,
                FootprintDepthCells = 0,
                BaseElevationFeet = 0f,
                FootprintAcres = 0f,
                HasData = false
            };

            CurrentLayoutKind = string.Empty;
        }

        protected override void OnUpdate()
        {
            if (_toolSystem == null)
                return;

            var selected = _toolSystem.selected;

            // No change → skip
            if (selected == _lastSelected)
                return;
            _lastSelected = selected;

            var em = EntityManager;

            // Deselection → clear stats (no data)
            if (selected == Entity.Null)
            {
                CurrentStats = new BuildingStats
                {
                    Entity = Entity.Null,
                    HeightFeet = 0f,
                    Floors = 0,
                    FootprintCells = 0,
                    FootprintWidthCells = 0,
                    FootprintDepthCells = 0,
                    BaseElevationFeet = 0f,
                    FootprintAcres = 0f,
                    HasData = false
                };
                CurrentLayoutKind = string.Empty;
                return;
            }

            // Not a building → no data
            if (!em.HasComponent<Building>(selected))
            {
                CurrentStats = new BuildingStats
                {
                    Entity = selected,
                    HeightFeet = 0f,
                    Floors = 0,
                    FootprintCells = 0,
                    FootprintWidthCells = 0,
                    FootprintDepthCells = 0,
                    BaseElevationFeet = 0f,
                    FootprintAcres = 0f,
                    HasData = false
                };
                CurrentLayoutKind = string.Empty;
                return;
            }

            // Prefab
            Entity prefabEntity = Entity.Null;
            if (em.HasComponent<PrefabRef>(selected))
            {
                prefabEntity = em.GetComponentData<PrefabRef>(selected).m_Prefab;
            }

            bool hasPrefab = prefabEntity != Entity.Null;
            bool hasPrefabGeometry = hasPrefab && em.HasComponent<ObjectGeometryData>(prefabEntity);
            bool hasInstanceGeometry = em.HasComponent<ObjectGeometryData>(selected);
            bool hasSpawn = hasPrefab && em.HasComponent<SpawnableBuildingData>(prefabEntity);
            bool hasSig = hasPrefab && em.HasComponent<SignatureBuildingData>(prefabEntity);

            CurrentLayoutKind = (hasSpawn && !hasSig)
                ? "Level"
                : "Description";

            float heightFeet = 0f;
            int floors = 0;
            int footprintCells = 0;
            int footprintWidthCells = 0;
            int footprintDepthCells = 0;
            float footprintAcres = 0f;
            bool okHeight = false;

            float baseElevationFeet = 0f;
            bool okElevation = false;

            // Sea-level offset (user setting, can be +/-). Default to 0.
            float seaLevelOffsetMeters = Mod.Settings?.SeaLevelOffsetMeters ?? 0f;

            try
            {
                // 1) Prefer instance geometry when available
                if (hasInstanceGeometry)
                {
                    var instGeo = em.GetComponentData<ObjectGeometryData>(selected);

                    // Height/floor logic (partial file)
                    okHeight = TryGetHeightFeetAndFloors(instGeo, out heightFeet, out floors);

                    // Footprint is optional: failure here does NOT affect okHeight
                    TryGetFootprint(
                        instGeo,
                        out footprintCells,
                        out footprintWidthCells,
                        out footprintDepthCells,
                        out footprintAcres);

                    // Elevation (partial file)
                    // Instance geometry bounds are treated as WORLD space (do NOT add Transform offset).
                    okElevation = TryGetBaseElevationAboveSeaFeet(
                        selected,
                        instGeo,
                        boundsAreLocalSpace: false,
                        seaLevelOffsetMeters,
                        out baseElevationFeet);
                }

                // 2) If instance failed or not present, try prefab geometry
                if ((!okHeight || !okElevation) && hasPrefabGeometry)
                {
                    var prefabGeo = em.GetComponentData<ObjectGeometryData>(prefabEntity);

                    if (!okHeight)
                        okHeight = TryGetHeightFeetAndFloors(prefabGeo, out heightFeet, out floors);

                    if (!okElevation)
                    {
                        // Prefab geometry bounds are treated as LOCAL space (apply Transform offset).
                        okElevation = TryGetBaseElevationAboveSeaFeet(
                            selected,
                            prefabGeo,
                            boundsAreLocalSpace: true,
                            seaLevelOffsetMeters,
                            out baseElevationFeet);
                    }

                    // Only try prefab footprint if instance footprint did not succeed
                    if (footprintCells == 0 && footprintAcres == 0f)
                    {
                        TryGetFootprint(
                            prefabGeo,
                            out footprintCells,
                            out footprintWidthCells,
                            out footprintDepthCells,
                            out footprintAcres);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _log.Warn(ex, $"{nameof(BuildingHeightAndFootprintSystem)} — exception while reading geometry.");
                okHeight = false;

                okElevation = false;
                baseElevationFeet = 0f;

                footprintCells = 0;
                footprintWidthCells = 0;
                footprintDepthCells = 0;
                footprintAcres = 0f;
            }

            if (!okHeight)
            {
                _log.Info(
                    $"{nameof(BuildingHeightAndFootprintSystem)} — could not derive geometry-based height; " +
                    "keeping Height/Floors at 0.");
                heightFeet = 0f;
                floors = 0;
            }

            // Any *building* gets HasData = true.
            CurrentStats = new BuildingStats
            {
                Entity = selected,
                HeightFeet = heightFeet,
                Floors = floors,
                FootprintCells = footprintCells,
                FootprintWidthCells = footprintWidthCells,
                FootprintDepthCells = footprintDepthCells,
                FootprintAcres = footprintAcres,
                BaseElevationFeet = baseElevationFeet,
                HasData = true
            };
        }

        protected override void OnDestroy()
        {
            GeometryReflectionCache.Clear();
            base.OnDestroy();
            _toolSystem = null;
            _lastSelected = Entity.Null;
            CurrentLayoutKind = string.Empty;
            CurrentStats = default;
        }

        // --------------------------------------------------------------------
        // Simple UI helpers — NO UnityEngine.UI, just plain text.
        // --------------------------------------------------------------------

        public static string FormatStats(BuildingStats stats)
        {
            // If there is no valid building selected, say nothing.
            if (!stats.HasData || stats.Entity == Entity.Null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            // Height unit choice (default to Feet if settings unavailable)
            var unit = Mod.Settings?.HeightUnit ?? ModSettings.HeightUnitKind.Feet;

            float heightValue;
            string heightUnitLabel;

            if (unit == ModSettings.HeightUnitKind.Meters)
            {
                heightValue = stats.HeightFeet / Units.MetersToFeet;
                heightUnitLabel = " m";
            }
            else
            {
                heightValue = stats.HeightFeet;
                heightUnitLabel = " ft";
            }

            // Height and floors are always shown
            sb.AppendLine($"Height: {heightValue:F1}{heightUnitLabel}");
            sb.AppendLine($"Estimated Floors: {stats.Floors}");

            // Footprint grid as W x D if we have both dimensions
            if (stats.FootprintWidthCells > 0 && stats.FootprintDepthCells > 0)
            {
                sb.AppendLine($"Zoning Footprint: {stats.FootprintWidthCells} x {stats.FootprintDepthCells} cells");
            }

            // Area: acres in imperial, hectares in metric; 2 decimals
            if (stats.FootprintAcres > 0f)
            {
                float areaValue;
                string areaLabel;

                if (unit == ModSettings.HeightUnitKind.Meters)
                {
                    // Convert acres → hectares (1 acre ≈ 0.404685642 ha)
                    const float acresToHectares = 0.40468564224f;
                    areaValue = stats.FootprintAcres * acresToHectares;
                    areaLabel = "hectares";
                }
                else
                {
                    areaValue = stats.FootprintAcres;
                    areaLabel = "acres";
                }

                sb.AppendLine($"{char.ToUpper(areaLabel[0])}{areaLabel.Substring(1)} Footprint: {areaValue:F2} {areaLabel}");
            }

            // Elevation shown when present (we always have a number; if derivation failed it's 0)
            float elevValue;
            string elevUnitLabel;

            if (unit == ModSettings.HeightUnitKind.Meters)
            {
                elevValue = stats.BaseElevationFeet / Units.MetersToFeet;
                elevUnitLabel = " m";
            }
            else
            {
                elevValue = stats.BaseElevationFeet;
                elevUnitLabel = " ft";
            }

            sb.AppendLine($"Elevation: {elevValue:F1}{elevUnitLabel}");

            return sb.ToString();
        }

        public static string FormatCurrentStatsForUi()
        {
            var stats = CurrentStats;
            string result = FormatStats(stats);
            return result;
        }
    }
}
