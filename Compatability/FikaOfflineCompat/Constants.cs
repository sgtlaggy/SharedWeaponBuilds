using System.Text.Json;
using SPTarkov.Server.Core.Utils.Json.Converters;


namespace SharedWeaponBuilds.FikaOfflineCompat;

public static class Constants
{
    public static JsonSerializerOptions _jsonOptions = new() { Converters = { new StringToMongoIdConverter() } };
    public static string ProfileDataKey = "SharedWeaponBuildsFikaOfflineCompat";
}
