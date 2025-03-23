using System;

using VampireCommandFramework;

namespace PrisonerExchange.Services.Chat;

public static class PromptHelper
{
	public static void UserInput(ChatCommandContext ctx, Action<string> onInput, string instruction = "Please enter a value:", int timeout = 30)
	{
		ulong userId = ctx.Event.User.PlatformId;

		if (!string.IsNullOrEmpty(instruction))
		{
			ctx.Reply(instruction);
		}

		PromptManager.RequestInput(userId,
				response =>
				{
					onInput(response);
				},
				() =>
				{
					ctx.Reply("Selection has timed out.");
				},
				timeout
		);
	}
}