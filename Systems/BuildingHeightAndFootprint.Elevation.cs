using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;

namespace BuildingHeightAndFootprint
{
    public partial class BuildingHeightAndFootprintSystem
    {
        // Named uniquely to avoid partial-class duplicate constant collisions.
        private const float FeetPerMeter = 3.28084f;

        /// <summary>
        /// Computes base elevation ABOVE USER-DEFINED SEA LEVEL in feet.
        ///
        /// The user supplies SeaLevelBaselineMeters (world Y in meters that should read as 0 ft).
        ///
        /// IMPORTANT:
        /// - INSTANCE geometry bounds are treated as WORLD space.
        /// - PREFAB geometry bounds are treated as LOCAL space (apply Transform offset).
        /// </summary>
        internal bool TryGetBaseElevationAboveSeaFeet(
            Entity entity,
            ObjectGeometryData geo,
            bool boundsAreLocalSpace,
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

            float baselineMeters = Mod.Settings?.SeaLevelBaselineMeters ?? 0f;

            float aboveSeaMeters = worldBaseMeters - baselineMeters;
            baseElevationFeet = aboveSeaMeters * FeetPerMeter;
            return true;
        }
    }
}
