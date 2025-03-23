using Unity.Entities;
using ProjectM;
using PrisonerExchange.Data;
using PrisonerExchange.Extensions;

using Stunlock.Core;

namespace PrisonerExchange.Models;

public class PrisonerModel
{
	private readonly EntityManager EM = Core.EntityManager;
	private readonly Entity prisonerEntity;

	public PrisonerModel(Entity prisoner)
	{
		prisonerEntity = prisoner;
	}

	public PrisonerInformation Info
	{
		get
		{
			if (!prisonerEntity.Exists())
				return null;

			string unitType = "Unknown";
			string bloodType = "Unknown";
			string bloodQuality = "Unknown";

			if (prisonerEntity.Has<PrefabGUID>())
			{
				var prefab = prisonerEntity.Read<PrefabGUID>();
				unitType = Prefabs.UnitTypes.TryGetValue(prefab, out var knownUnitType) ? knownUnitType : "Unknown";
			}

			if (prisonerEntity.Has<BloodConsumeSource>())
			{
				var blood = prisonerEntity.Read<BloodConsumeSource>();
				bloodType = Prefabs.BloodTypes.TryGetValue(blood.UnitBloodType, out var knownBloodType) ? knownBloodType : "Unknown";
				bloodQuality = blood.BloodQuality.ToString("F0");
			}

			return new PrisonerInformation
			{
				UnitType = unitType,
				BloodType = bloodType,
				BloodQuality = bloodQuality
			};
		}
	}

	public class PrisonerInformation
	{
		public string UnitType { get; init; }
		public string BloodType { get; init; }
		public string BloodQuality { get; init; }
	}
}