using Unity.Entities;
using ProjectM.Network;
using ProjectM;
using VampireCommandFramework;
using PrisonerExchange.Extensions;

namespace PrisonerExchange.Models;

public class UserModel
{
	public Entity Entity { get; }
	public User User { get; }
	public ulong PlatformId { get; }
	public string CharacterName { get; }
	public bool IsAdmin { get; }

	public UserModel(Entity entity, User userData)
	{
		Entity = entity;
		User = userData;
		PlatformId = userData.PlatformId;
		CharacterName = userData.CharacterName.ToString();
		IsAdmin = userData.IsAdmin;
	}

	public bool IsClanLeader
	{
		get
		{
			if (Entity.TryGetComponent<ClanRole>(out var role))
			{
				if (role.Value == ClanRoleEnum.Leader)
				{
					return true;
				}
			}

			return false;
		}
	}

	public override bool Equals(object obj)
	{
		if (obj is UserModel other)
		{
			return this.PlatformId == other.PlatformId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return PlatformId.GetHashCode();
	}
}