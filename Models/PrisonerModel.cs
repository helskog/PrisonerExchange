using Unity.Entities;
using ProjectM;
using PrisonerExchange.Data;
using PrisonerExchange.Extensions;

using Stunlock.Core;

namespace PrisonerExchange.Models;

public class PrisonerModel
{
	private readonly EntityManager EM = Core.EntityManager;
	private readonly Entity entity;

	public PrisonerModel(Entity prisoner)
	{
		entity = prisoner;
	}

	public Entity PrisonerEntity => entity;

	public PrisonerInformation Info
	{
		get
		{
			if (!entity.Exists())
				return null;

			PrefabGUID prefabGUID = PrefabGUID.Empty;
			BloodConsumeSource bloodInfo = new BloodConsumeSource();
			string unitType = "Unknown";
			string bloodType = "Unknown";
			string bloodQuality = "Unknown";

			if (entity.Has<PrefabGUID>())
			{
				prefabGUID = entity.Read<PrefabGUID>();
				unitType = Prefabs.UnitTypes.TryGetValue(prefabGUID, out var knownUnitType) ? knownUnitType : "Unknown";
			}

			if (entity.Has<BloodConsumeSource>())
			{
				bloodInfo = entity.Read<BloodConsumeSource>();
				bloodType = Prefabs.BloodTypes.TryGetValue(bloodInfo.UnitBloodType, out var knownBloodType) ? knownBloodType : "Unknown";
				bloodQuality = bloodInfo.BloodQuality.ToString("F0");
			}

			return new PrisonerInformation
			{
				PrefabGUID = prefabGUID,
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