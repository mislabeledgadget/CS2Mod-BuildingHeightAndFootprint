using BuildingHeightAndFootprint.Systems;
using Game.Prefabs;
using System;
using System.Reflection;

namespace BuildingHeightAndFootprint
{
    public partial class BuildingHeightAndFootprintSystem
    {
        /// <summary>
        /// Extract height from ObjectGeometryData and estimate floors.
        /// Reflection heavy; stays private to the system.
        /// </summary>
        private bool TryGetHeightFeetAndFloors(
            ObjectGeometryData geometry,
            out float heightFeet,
            out int approxFloors)
        {
            heightFeet = 0f;
            approxFloors = 0;

            try
            {
                var geoType = geometry.GetType();
                var cache = GeometryReflectionCache.GetOrAdd(geoType);

                if (cache.BoundsField == null || cache.MinField == null || cache.MaxField == null || cache.VecYField == null)
                    return false;

                var boundsObj = cache.BoundsField.GetValue(geometry);
                if (boundsObj == null)
                    return false;

                var minObj = cache.MinField.GetValue(boundsObj);
                var maxObj = cache.MaxField.GetValue(boundsObj);
                if (minObj == null || maxObj == null)
                    return false;

                float minY = Convert.ToSingle(cache.VecYField.GetValue(minObj));
                float maxY = Convert.ToSingle(cache.VecYField.GetValue(maxObj));

                float heightMeters = maxY - minY;
                if (heightMeters <= 0f)
                    return false;

                heightFeet = heightMeters * Units.MetersToFeet;

                approxFloors = EstimateFloors(heightFeet);

                return true;
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"{nameof(BuildingHeightAndFootprintSystem)} — geometry parse error (height/floors).");
                return false;
            }
        }


        /// <summary>
        /// Pure floor estimation heuristic from height.
        /// Stays here because it's only meaningful inside this system.
        /// </summary>
        private int EstimateFloors(float heightFeet)
        {
            if (heightFeet <= 0f)
                return 0;

            // -------------------------
            // 1) Tiny objects
            // -------------------------
            if (heightFeet < 8f)
                return 1;

            // -------------------------
            // 2) Low-rise: houses / very small commercial
            // -------------------------
            if (heightFeet < 40f)
            {
                if (heightFeet <= 14f)
                {
                    // 8–14 ft → 1 floor (typical single-story)
                    return 1;
                }

                if (heightFeet <= 26f)
                {
                    // ~14–26 ft → 2 floors (2-story house / small commercial)
                    return 2;
                }

                // ~26–40 ft → 3 floors (triple-decker / small mixed-use)
                return 3;
            }

            // -------------------------
            // 3) Mid-rise: 40–200 ft
            //    Slightly conservative average floor height.
            // -------------------------
            if (heightFeet < 200f)
            {
                // Allow some roof/mechanical / parapet space
                float roofCap = System.Math.Min(20f, heightFeet * 0.10f);
                float coreHeight = System.Math.Max(0f, heightFeet - roofCap);

                const float avgFloorFeet = 12.5f;

                int floors = (int)System.Math.Round(coreHeight / avgFloorFeet);
                return System.Math.Max(3, floors);
            }

            // -------------------------
            // 4) Tall high-rise: 200 ft+
            //
            // Model:
            //   - One taller podium / ground / lobby section.
            //   - Many regular upper floors.
            //
            // This matches:
            //   • tall ground floor (often 1.5–2.5× typical floor)
            //   • denser upper floors (office/residential)
            // -------------------------
            const float podiumHeightFeet = 25f;   // "fat" ground floor + some mech
            const float upperAvgFloorFeet = 13.5f;

            float remaining = heightFeet - podiumHeightFeet;
            if (remaining <= 0f)
            {
                // Extremely weird, but ensure at least 2 floors.
                return 2;
            }

            int upperFloors = (int)System.Math.Round(remaining / upperAvgFloorFeet);

            // 1 podium floor + upper floors
            int totalFloors = 1 + upperFloors;

            // For anything this tall, don't go below 5 floors.
            return System.Math.Max(5, totalFloors);
        }

    }
}
