using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SharedWeaponBuilds.Server;

public record SharedWeaponBuildsModMetadata : AbstractModMetadata
{
    // All the way at the end of each typepriority, to make sure we catch all weapons being loaded prior
    public const int SharedWeaponBuildsPriorityOffset = 99999;

    public override string ModGuid { get; init; } = "wtf.archangel.sharedweaponbuilds";
    public override string Name { get; init; } = "Shared Weapon Builds";
    public override string Author { get; init; } = "ArchangelWTF";
    public override List<string>? Contributors { get; init; } = [];
    public override Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0");
    public override List<string>? Incompatibilities { get; init; } = [];
    public override Dictionary<string, Range>? ModDependencies { get; init; } = [];
    public override string? Url { get; init; } = "https://github.com/ArchangelWTF/SharedWeaponBuilds";
    public override bool? IsBundleMod { get; init; } = false;
    public override string License { get; init; } = "MIT";
}
