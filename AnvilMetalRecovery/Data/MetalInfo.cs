using System.ComponentModel;
using Newtonsoft.Json;

namespace AnvilMetalRecovery;

//Too outdated for...: [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
[JsonObject(MemberSerialization.OptIn)]
public struct MetalInfo
{
	[JsonProperty("code", Required = Required.Always)]
	public readonly string Code;

	[JsonProperty("meltPoint")] [DefaultValue(1000f)]
	public readonly float MeltingPoint;

	[JsonProperty("boilPoint")] [DefaultValue(100f)]
	public readonly float BoilingPoint;

	[JsonIgnore] [DefaultValue(30)] public int MeltingDuration;

	[JsonProperty("density")] [DefaultValue(10000f)]
	public readonly float Density;

	[JsonProperty("specificHeatCapacity")] [DefaultValue(1f)]
	public readonly float SpecificHeatCapacity;

	[JsonProperty("elemental")] public readonly bool Elemental;

	[JsonProperty("tier")] public readonly int Tier;
}