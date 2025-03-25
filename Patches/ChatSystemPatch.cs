using System;

using HarmonyLib;

using PrisonerExchange.Extensions;
using PrisonerExchange.Utility.Chat;

using ProjectM;
using ProjectM.Network;

using Unity.Collections;
using Unity.Entities;

namespace PrisonerExchange.Patches;

[HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
[HarmonyPriority(400)]
public static class ChatSystemPromptPatch
{
	public static bool Prefix(ChatMessageSystem __instance)
	{
		var em = __instance.EntityManager;
		var query = __instance.__query_661171423_0;

		var chatEntities = query.ToEntityArray(Allocator.Temp);

		try
		{
			foreach (var entity in chatEntities)
			{
				var from = em.GetComponentData<FromCharacter>(entity);
				var chat = em.GetComponentData<ChatMessageEvent>(entity);

				Entity userEntity = from.User;
				User userObject = userEntity.Read<User>();

				ulong userId = userObject.PlatformId;

				string message = chat.MessageText.ToString().Trim();

				if (PromptManager.IsWaiting(userId))
				{
					if (message.Equals("!s", StringComparison.OrdinalIgnoreCase))
					{
						PromptManager.CancelPrompt(userId);
						ServerChatUtils.SendSystemMessageToClient(em, userObject, "Prompt cancelled!");
					}
					else
					{
						PromptManager.TryHandleInput(userId, message);
					}

					em.DestroyEntity(entity); // Prevent VCF processing
					return false;
				}
			}
		}
		finally
		{
			chatEntities.Dispose();
		}

		return true;
	}
}