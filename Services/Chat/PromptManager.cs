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

    private static readonly Dictionary<ulong, PromptData> StoredPrompts = new();

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

        StoredPrompts[userId] = new PromptData
        {
            OnInput = onInput,
            OnTimeout = onTimeout,
            Timeout = timer
        };
    }

    public static bool TryHandleInput(ulong userId, string message)
    {
        if (!StoredPrompts.TryGetValue(userId, out var prompt))
        {
            return false;
        }

        prompt.Timeout.Stop();
        StoredPrompts.Remove(userId);
        prompt.OnInput?.Invoke(message);
        return true;
    }

    public static void CancelPrompt(ulong userId)
    {
        if (StoredPrompts.TryGetValue(userId, out var prompt))
        {
            prompt.Timeout.Stop();
            StoredPrompts.Remove(userId);
        }
    }

    public static bool IsWaiting(ulong userId) => StoredPrompts.ContainsKey(userId);
}