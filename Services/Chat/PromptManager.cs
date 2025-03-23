using System;
using System.Collections.Generic;
using System.Timers;

namespace PrisonerExchange.Services.Chat;

public static class PromptManager
{
	private class PromptData
	{
		public Action<string> OnInput;
		public Action OnTimeout;
		public Timer Timeout;
	}

	private static readonly Dictionary<ulong, PromptData> storedPrompts = new();

	public static void RequestInput(ulong userId, Action<string> onInput, Action onTimeout, int timeoutSeconds = 30)
	{
		CancelPrompt(userId);

		var timer = new Timer(timeoutSeconds * 1000);
		timer.Elapsed += (_, _) =>
		{
			CancelPrompt(userId);
			onTimeout?.Invoke();
		};
		timer.AutoReset = false;
		timer.Start();

		storedPrompts[userId] = new PromptData
		{
			OnInput = onInput,
			OnTimeout = onTimeout,
			Timeout = timer
		};
	}

	public static bool TryHandleInput(ulong userId, string message)
	{
		if (!storedPrompts.TryGetValue(userId, out var prompt))
		{
			return false;
		}

		prompt.Timeout.Stop();
		storedPrompts.Remove(userId);
		prompt.OnInput?.Invoke(message);
		return true;
	}

	public static void CancelPrompt(ulong userId)
	{
		if (storedPrompts.TryGetValue(userId, out var prompt))
		{
			prompt.Timeout.Stop();
			storedPrompts.Remove(userId);
		}
	}

	public static bool IsWaiting(ulong userId) => storedPrompts.ContainsKey(userId);
}