using System;

using HarmonyLib;

using PrisonerExchange.Extensions;
using PrisonerExchange.Services.Chat;

using ProjectM;
using ProjectM.Network;

using Unity.Collections;
using Unity.Entities;

namespace PrisonerExchange.Patches;

/// <summary>
/// This patch is only necessary to enable our PromptHelper.
/// See the .pe swap command for reference on how we use prompt-helper to get secondary
/// input from the user when prompted for it.
/// </summary>
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
						var promptCanceledMessage = new FixedString512Bytes("Prompt cancelled!");
						ServerChatUtils.SendSystemMessageToClient(em, userObject, ref promptCanceledMessage);
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
