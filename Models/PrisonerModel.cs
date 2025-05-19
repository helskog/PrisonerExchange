using Unity.Entities;
using ProjectM;
using PrisonerExchange.Data;
using PrisonerExchange.Extensions;

using Stunlock.Core;

namespace PrisonerExchange.Models;

public class PrisonerModel(Entity prisoner)
{
	public Entity PrisonerEntity => prisoner;

	public PrisonerInformation Info
	{
		get
		{
			if (!prisoner.Exists())
				return null;

			PrefabGUID prefabGuid = PrefabGUID.Empty;
			BloodConsumeSource bloodInfo = new BloodConsumeSource();
			string unitType = "Unknown";
			string bloodType = "Unknown";
			string bloodQuality = "Unknown";

			if (prisoner.Has<PrefabGUID>())
			{
				prefabGuid = prisoner.Read<PrefabGUID>();
				unitType = Prefabs.UnitTypes.TryGetValue(prefabGuid, out var knownUnitType) ? knownUnitType : "Unknown";
			}

			if (prisoner.Has<BloodConsumeSource>())
			{
				bloodInfo = prisoner.Read<BloodConsumeSource>();
				bloodType = Prefabs.BloodTypes.TryGetValue(bloodInfo.UnitBloodType, out var knownBloodType) ? knownBloodType : "Unknown";
				bloodQuality = bloodInfo.BloodQuality.ToString("F0");
			}

			return new PrisonerInformation
			{
				PrefabGUID = prefabGuid,
				BloodInfo = bloodInfo,
				UnitType = unitType,
				BloodType = bloodType,
				BloodQuality = bloodQuality
			};
		}
	}

	public class PrisonerInformation
	{
		public PrefabGUID PrefabGUID { get; init; }
		public BloodConsumeSource BloodInfo { get; init; }
		public string UnitType { get; init; }
		public string BloodType { get; init; }
		public string BloodQuality { get; init; }
	}
}