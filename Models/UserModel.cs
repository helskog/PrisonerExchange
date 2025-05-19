using Unity.Entities;
using ProjectM.Network;
using ProjectM;
using PrisonerExchange.Extensions;

namespace PrisonerExchange.Models;

public class UserModel(Entity entity, User userData)
{
	public Entity Entity { get; } = entity;
	public User User { get; } = userData;
	public ulong PlatformId { get; } = userData.PlatformId;
	public string CharacterName { get; } = userData.CharacterName.ToString();
	public bool IsAdmin { get; } = userData.IsAdmin;

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