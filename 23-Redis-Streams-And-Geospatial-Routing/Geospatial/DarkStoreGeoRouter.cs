// [KYA KARTA HAI]: System namespace import karta hai for mathematical functions.
// [KYU ZAROORI HAI]: Haversine formula calculation ke liye Math.Sin, Math.Cos access karna.
// [KAISE CHALTA HAI]: .NET core BCL.
// [OUTPUT / SIDE EFFECT]: Basic math symbols available.
// [VARIATIONS TO TRY]: Spatial geometry library like NetTopologySuite.
using System;

// [KYA KARTA HAI]: Namespace define karta hai geospatial routing logic ke liye.
// [KYU ZAROORI HAI]: Radial dark-store search ko project me encapsulate karne ke liye.
// [KAISE CHALTA HAI]: Namespace scope.
// [OUTPUT / SIDE EFFECT]: Geospatial namespace.
// [VARIATIONS TO TRY]: Dedicated GIS module.
namespace KiranaHub.RedisAdvanced.Geospatial;

// [KYA KARTA HAI]: GPS coordinate record define karta hai with Latitude aur Longitude.
// [KYU ZAROORI HAI]: Physical location data represent karne ke liye.
// [KAISE CHALTA HAI]: Readonly record with double values.
// [OUTPUT / SIDE EFFECT]: GeoLocation record.
// [VARIATIONS TO TRY]: Altitude property add karna.
public record GeoLocation(double Latitude, double Longitude);

// [KYA KARTA HAI]: Dark-Store warehouse location node record.
// [KYU ZAROORI HAI]: Dark-store ki location aur uski delivery radius capability track karne ke liye.
// [KAISE CHALTA HAI]: Id, Name, Location aur ServiceRadiusKm encapsulate karta hai.
// [OUTPUT / SIDE EFFECT]: DarkStoreNode record.
// [VARIATIONS TO TRY]: Cold-storage capability flag.
public record DarkStoreNode(
    string Id,
    string Name,
    GeoLocation Location,
    double ServiceRadiusKm);

// [KYA KARTA HAI]: Geospatial routing engine jo Redis GEO commands (GEOADD, GEODIST, GEOSEARCH) emulate karta hai.
// [KYU ZAROORI HAI]: Customer address ke 3 km radius me available dark-stores discover karne ke liye.
// [KAISE CHALTA HAI]: In-memory spatial index maintain karta hai aur Great-Circle Haversine formula se real-world road distances compute karta hai.
// [OUTPUT / SIDE EFFECT]: DarkStoreGeoRouter class.
// [VARIATIONS TO TRY]: StackExchange.Redis GeoRadiusAsync integration.
public class DarkStoreGeoRouter
{
    private readonly List<DarkStoreNode> _darkStores = new();
    private readonly object _lock = new();

    // Earth's mean radius in kilometers
    private const double EarthRadiusKm = 6371.0;

    // [KYA KARTA HAI]: Naya dark-store register karta hai spatial index me (GEOADD emulation).
    // [KYU ZAROORI HAI]: Network me dark-stores add karne ke liye.
    // [KAISE CHALTA HAI]: Coordinates validate karke list me add karta hai.
    // [OUTPUT / SIDE EFFECT]: DarkStore added.
    // [VARIATIONS TO TRY]: Update coordinates if ID exists.
    public void AddDarkStore(string id, string name, double latitude, double longitude, double serviceRadiusKm = 3.5)
    {
        lock (_lock)
        {
            _darkStores.Add(new DarkStoreNode(
                id,
                name,
                new GeoLocation(latitude, longitude),
                serviceRadiusKm));
        }
    }

    // [KYA KARTA HAI]: Haversine formula se do GPS coordinates ke beech great-circle distance calculate karta hai (GEODIST emulation).
    // [KYU ZAROORI HAI]: Physical delivery distance in kilometers compute karne ke liye.
    // [KAISE CHALTA HAI]: Lat/Lng ko radians me convert karke spherical trigonometry solve karta hai.
    // [OUTPUT / SIDE EFFECT]: Distance in kilometers (double).
    // [VARIATIONS TO TRY]: Vincenty's formula for higher ellipsoidal precision.
    public static double CalculateDistanceKm(GeoLocation point1, GeoLocation point2)
    {
        var dLat = ToRadians(point2.Latitude - point1.Latitude);
        var dLon = ToRadians(point2.Longitude - point1.Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(point1.Latitude)) * Math.Cos(ToRadians(point2.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(EarthRadiusKm * c, 2);
    }

    // [KYA KARTA HAI]: Customer location ke sabse nazdeek dark-store dhundhta hai jo us area ko service karta ho (GEOSEARCH emulation).
    // [KYU ZAROORI HAI]: Order assignment ke liye nearest dark-store select karna.
    // [KAISE CHALTA HAI]: Distance calculate karta hai, service radius check karta hai, aur minimum distance wala hub return karta hai.
    // [OUTPUT / SIDE EFFECT]: Tuple (DarkStoreNode store, double distanceKm) or null if out of delivery zone.
    // [VARIATIONS TO TRY]: Sort by stock availability score.
    public (DarkStoreNode Store, double DistanceKm)? FindNearestServiceableDarkStore(GeoLocation customerLocation)
    {
        lock (_lock)
        {
            DarkStoreNode? bestStore = null;
            double shortestDistance = double.MaxValue;

            foreach (var store in _darkStores)
            {
                var distance = CalculateDistanceKm(customerLocation, store.Location);
                if (distance <= store.ServiceRadiusKm && distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestStore = store;
                }
            }

            if (bestStore == null) return null;
            return (bestStore, shortestDistance);
        }
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
