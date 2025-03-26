using PrisonerExchange.Models;
using PrisonerExchange.Utility;
using ProjectM.Behaviours;
using ProjectM;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections.Generic;
using PrisonerExchange.Extensions;
using System.Linq;
using System;

namespace PrisonerExchange.Commands;

public class PrisonerService
{
	/// <summary>
	/// Gather a list of prisoners owned by a clan / user.
	/// </summary>
	public static List<PrisonerModel> GetPrisonerList(UserModel user)
	{
		var characterName = user.CharacterName;
		var clanEntities = EntityUtil.GetEntitiesByComponentType<ClanTeam>().ToArray();
		var matchingClanEntity = clanEntities.FirstOrDefault(e => e.Read<ClanTeam>().Name.ToString().Equals(characterName, StringComparison.OrdinalIgnoreCase));

		var prisonerList = new List<PrisonerModel>();
		int teamValue = 0;

		if (matchingClanEntity != Entity.Null)
		{
			teamValue = matchingClanEntity.Read<ClanTeam>().TeamValue;
		}
		else
		{
			UserModel playerCharacter = UserUtil.GetUserByCharacterName(characterName);

			if (playerCharacter == null)
				return null;

			teamValue = playerCharacter.Entity.Read<Team>().Value;
		}

		if (teamValue == 0)
			return null;

		foreach (Entity prisonCell in EntityUtil.GetEntitiesByComponentType<PrisonCell>())
		{
			if (!Core.EntityManager.TryGetComponentData<PrisonCell>(prisonCell, out var prisonCellData))
				continue;

			if (prisonCellData.ImprisonedEntity._Entity == Entity.Null)
				continue;

			if (!prisonCell.TryGetComponent<Team>(out var cellTeamInfo) || cellTeamInfo.Value != teamValue)
				continue;

			PrisonerModel prisoner = new PrisonerModel(prisonCellData.ImprisonedEntity._Entity);

			if (!prisonerList.Contains(prisoner))
			{
				prisonerList.Add(prisoner);
			}
		}

		return prisonerList;
	}

	/// <summary>
	/// Spawn new NPC and attach it to a prisoncell.
	/// </summary>
	public static void SpawnPrisonerInCell(PrisonerModel.PrisonerInformation prisonerInfo, Entity prisoncellEntity, UserModel initiator)
	{
		if (!Core.EntityManager.TryGetComponentData<PrisonCell>(prisoncellEntity, out var prisonCellData))
			Plugin.Logger.Error("PrisonerService", $"prisoncellEntity does not contain PrisonCell data.");

		Prisonstation prisonStation = prisoncellEntity.Read<Prisonstation>();

		if (prisoncellEntity.TryGetComponent<LocalToWorld>(out var LocalToWorld))
		{
			float3 cellCenterPosition = LocalToWorld.Position;
			Plugin.Logger.LogError("Cell position: " + cellCenterPosition);

			Entity spawnedEntity = Services.UnitSpawnerService.SpawnWithCallback(
			Entity.Null,
			prisonerInfo.PrefabGUID,
			new float2(cellCenterPosition.x, cellCenterPosition.z),
			-1,
			(e) =>
			{
				if (e.Has<BloodConsumeSource>())
				{
					var blood = Core.EntityManager.GetComponentData<BloodConsumeSource>(e);
					blood.UnitBloodType._Value = prisonerInfo.BloodInfo.UnitBloodType;
					blood.BloodQuality = prisonerInfo.BloodInfo.BloodQuality;
					blood.CanBeConsumed = true;
					Core.EntityManager.SetComponentData(e, blood);
				}

				Imprisoned imprisoned = new Imprisoned();
				imprisoned.PrisonCellEntity = prisoncellEntity;

				BehaviourTreeState behaviourTreeState = e.Read<BehaviourTreeState>();
				behaviourTreeState.Value = GenericEnemyState.Imprisoned;
				BehaviourTreeStateMetadata behaviourTreeStateMetadata = e.Read<BehaviourTreeStateMetadata>();
				behaviourTreeStateMetadata.PreviousState = GenericEnemyState.Imprisoned;

				e.Add<Imprisoned>();
				e.Write(imprisoned);
				e.Write(behaviourTreeState);

				prisonCellData.ImprisonedEntity = e;
				prisoncellEntity.Write(prisonCellData);
				prisonStation.HasPrisoner = true;
				prisoncellEntity.Write(prisonStation);

				// ImprisonedBuff
				BuffUtil.BuffNPC(e, initiator.Entity, BuffUtil.ELECTRIC_BUFF, -1);
			},
			cellCenterPosition.y
			);
		}
	}

	/// <summary>
	/// Move one prisoner to an empty cell.
	/// </summary>
	public static void MovePrisoner(PrisonerModel prisoner, Entity prisoncell, UserModel initiator)
	{
		Entity prisonerEntity = prisoner.PrisonerEntity;

		// Gather information about prisoner.
		PrisonerModel.PrisonerInformation prisonerInformation = prisoner.Info;

		// Kill senderentity
		StatChangeUtility.KillEntity(Core.EntityManager, prisonerEntity, Entity.Null, 0.0, StatChangeReason.Default);
		Plugin.Logger.Info("PrisonerService", $"Killing sender prisoner {prisonerEntity}.");

		// Spawn new NPC for receiver
		SpawnPrisonerInCell(prisonerInformation, prisoncell, initiator);
	}

	/// <summary>
	/// Swap two prisoners between different prisoncells.
	/// </summary>
	public static bool SwapPrisoner(PrisonerModel PrisonerA, PrisonerModel PrisonerB, UserModel initiator)
	{
		Entity prisonCellA = GetAttachedPrisonCell(PrisonerA);
		Entity prisonCellB = GetAttachedPrisonCell(PrisonerB);

		if (prisonCellA == Entity.Null || prisonCellB == Entity.Null)
		{
			Plugin.Logger.Error("PrisonerService", $"Could not get either PrisoncellA or PrisoncellB during prisoner swap.");
			return false;
		}

		// Get prisoner information for spawning new ones.
		PrisonerModel.PrisonerInformation prisonerInformationA = PrisonerA.Info;
		PrisonerModel.PrisonerInformation prisonerInformationB = PrisonerB.Info;

		// Kill both prisoners
		StatChangeUtility.KillEntity(Core.EntityManager, PrisonerA.PrisonerEntity, Entity.Null, 0.0, StatChangeReason.Default);
		StatChangeUtility.KillEntity(Core.EntityManager, PrisonerB.PrisonerEntity, Entity.Null, 0.0, StatChangeReason.Default);

		// Spawn new prisoners in opposite cells
		SpawnPrisonerInCell(prisonerInformationA, prisonCellB, initiator);
		SpawnPrisonerInCell(prisonerInformationB, prisonCellA, initiator);

		return true;
	}

	/// <summary>
	/// Get attached prison cell entity of a prisoner
	/// </summary>
	public static Entity GetAttachedPrisonCell(PrisonerModel prisoner)
	{
		if (!prisoner.PrisonerEntity.Has<Imprisoned>())
			return Entity.Null;

		return prisoner.PrisonerEntity.Read<Imprisoned>().PrisonCellEntity;
	}

	/// <summary>
	/// Check if cell already has prisoner.
	/// </summary>
	public static bool HasPrisoner(Entity cellEntity)
	{
		if (!Core.EntityManager.TryGetComponentData<PrisonCell>(cellEntity, out var celldata))
			return false;

		if (celldata.ImprisonedEntity._Entity == Entity.Null)
			return false;

		return true;
	}

	public static bool IsSameTeam(Entity prisoncell, UserModel user)
	{
		var castleTeamId = prisoncell.Read<Team>().Value;
		var userTeamId = user.User.LocalCharacter._Entity.Read<Team>().Value;

		if (castleTeamId == userTeamId)
			return true;

		return false;
	}
}