using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using PrisonerExchange.Models;
using ProjectM.Network;
using VampireCommandFramework;
using ProjectM;

namespace PrisonerExchange.Utility;

public class UserUtil
{
	public static UserModel[] All
	{
		get
		{
			var userQuery = Core.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());

			var userEntities = userQuery.ToEntityArray(Allocator.Temp);

			var resultList = new List<UserModel>(userEntities.Length);
			foreach (var entity in userEntities)
			{
				var userData = Core.EntityManager.GetComponentData<User>(entity);
				var model = new UserModel(entity, userData);
				resultList.Add(model);
			}

			userEntities.Dispose();

			return [.. resultList];
		}
	}

	public static UserModel GetCurrentUser(ChatCommandContext ctx)
	{
		return UserUtil.GetUserByPlatformId(ctx.User.PlatformId);
	}

	public static UserModel ToUserModel(Entity userEntity)
	{
		User userData = Core.EntityManager.GetComponentData<User>(userEntity);
		return new UserModel(userEntity, userData);
	}

	public static UserModel GetUserByPlatformId(ulong platformId)
	{
		return All.FirstOrDefault(u => u.PlatformId == platformId);
	}

	public static UserModel GetUserByCharacterName(string characterName)
	{
		if (string.IsNullOrEmpty(characterName))
			return null;

		return All.FirstOrDefault(u =>
				u.CharacterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase));
	}
}