using BuildingHeightAndFootprint.Systems; // Units
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;      // WaterSystem
using Unity.Entities;

namespace BuildingHeightAndFootprint
{
    public partial class BuildingHeightAndFootprintSystem
    {
        private bool TryGetSeaLevelMeters(out float seaLevelMeters)
        {
            seaLevelMeters = 0f;

            var waterSystem = World.GetExistingSystemManaged<WaterSystem>();
            if (waterSystem == null)
                return false;

            seaLevelMeters = waterSystem.SeaLevel;
            return true;
        }

        internal bool TryGetBaseElevationAboveSeaFeet(
            Entity entity,
            ObjectGeometryData geo,
            bool boundsAreLocalSpace,
            float seaLevelOffsetMeters,
            out float baseElevationFeet)
        {
            baseElevationFeet = 0f;

            var em = EntityManager;
            if (!em.HasComponent<Transform>(entity))
                return false;

            var tr = em.GetComponentData<Transform>(entity);

            Bounds3 bounds = geo.m_Bounds;
            float minY = bounds.min.y;

            float worldBaseMeters = boundsAreLocalSpace
                ? (tr.m_Position.y + minY)
                : minY;

            float elevationMeters;

            if (TryGetSeaLevelMeters(out float seaLevelMeters))
            {
                // Elevation above true sea level, then apply user correction.
                elevationMeters = (worldBaseMeters - seaLevelMeters) + seaLevelOffsetMeters;
            }
            else
            {
                // Absolute 0 baseline, then apply user correction.
                elevationMeters = worldBaseMeters + seaLevelOffsetMeters;
            }

            baseElevationFeet = elevationMeters * Units.MetersToFeet;
            return true;
        }
    }
}
