using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace BuildingHeightAndFootprint.Systems
{
    internal static class GeometryReflectionCache
    {
        internal sealed class CacheEntry
        {
            public FieldInfo BoundsField;
            public Func<object, object> BoundsGetter; // cached delegate that returns the bounds object for an instance
            public FieldInfo MinField;
            public FieldInfo MaxField;
            public FieldInfo VecXField;
            public FieldInfo VecYField;
            public FieldInfo VecZField;
        }

        private static readonly ConcurrentDictionary<Type, CacheEntry> _cache = new();

        public static CacheEntry GetOrAdd(Type geoType)
        {
            return _cache.GetOrAdd(geoType, t =>
            {
                var entry = new CacheEntry();

                // Try possible bounds field names used across versions
                entry.BoundsField =
                    t.GetField("m_Bounds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    t.GetField("Bounds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    t.GetField("bounds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (entry.BoundsField != null)
                {
                    var boundsType = entry.BoundsField.FieldType;

                    entry.MinField =
                        boundsType.GetField("m_Min", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                        boundsType.GetField("Min", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                        boundsType.GetField("min", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    entry.MaxField =
                        boundsType.GetField("m_Max", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                        boundsType.GetField("Max", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                        boundsType.GetField("max", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (entry.MinField != null)
                    {
                        var vecType = entry.MinField.FieldType;
                        entry.VecXField = vecType.GetField("x", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        entry.VecYField = vecType.GetField("y", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        entry.VecZField = vecType.GetField("z", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }

                    entry.BoundsGetter = CreateGetter(entry.BoundsField);
                }

                return entry;
            });
        }

        private static Func<object, object> CreateGetter(FieldInfo fi)
        {
            if (fi == null) return _ => null;
            return target => fi.GetValue(target); // simple; you can replace with Expression/DynamicMethod for speed
        }

        public static void Clear() => _cache.Clear();
    }
}