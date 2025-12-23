using BuildingHeightAndFootprint.Systems;
using Game.Prefabs;
using System;
using System.Reflection;

namespace BuildingHeightAndFootprint
{
    public partial class BuildingHeightAndFootprintSystem
    {
        private bool TryGetFootprint(
            ObjectGeometryData geometry,
            out int footprintCells,
            out int footprintWidthCells,
            out int footprintDepthCells,
            out float footprintAcres)
        {
            footprintCells = 0;
            footprintWidthCells = 0;
            footprintDepthCells = 0;
            footprintAcres = 0f;

            try
            {
                var geoType = geometry.GetType();
                var cache = GeometryReflectionCache.GetOrAdd(geoType);

                if (cache.BoundsField == null || cache.MinField == null || cache.MaxField == null ||
                    cache.VecXField == null || cache.VecZField == null)
                {
                    return false;
                }

                var boundsObj = cache.BoundsField.GetValue(geometry);
                if (boundsObj == null)
                    return false;

                var minObj = cache.MinField.GetValue(boundsObj);
                var maxObj = cache.MaxField.GetValue(boundsObj);
                if (minObj == null || maxObj == null)
                    return false;

                float minX = Convert.ToSingle(cache.VecXField.GetValue(minObj));
                float maxX = Convert.ToSingle(cache.VecXField.GetValue(maxObj));
                float minZ = Convert.ToSingle(cache.VecZField.GetValue(minObj));
                float maxZ = Convert.ToSingle(cache.VecZField.GetValue(maxObj));

                float widthMeters = Math.Abs(maxX - minX);
                float depthMeters = Math.Abs(maxZ - minZ);
                float areaMeters2 = widthMeters * depthMeters;

                if (areaMeters2 <= 0f)
                    return false;

                footprintWidthCells = Math.Max(1, (int)Math.Round(widthMeters / Units.ZoningCellMeters));
                footprintDepthCells = Math.Max(1, (int)Math.Round(depthMeters / Units.ZoningCellMeters));

                footprintCells = footprintWidthCells * footprintDepthCells;

                footprintAcres = areaMeters2 / Units.SquareMetersPerAcre;

                return true;
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"{nameof(BuildingHeightAndFootprintSystem)} — geometry parse error (footprint).");
                return false;
            }
        }
    }
}
