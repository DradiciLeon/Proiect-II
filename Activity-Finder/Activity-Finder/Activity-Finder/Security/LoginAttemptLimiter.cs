using System;
using System.Collections.Generic;

namespace Activity_Finder.Security
{
    public static class LoginAttemptLimiter
    {
        private const int MaxAttempts = 5;
        private static readonly TimeSpan LockoutTime = TimeSpan.FromMinutes(10);
        private static readonly Dictionary<string, LoginState> Attempts = new(StringComparer.OrdinalIgnoreCase);

        public static bool IsLocked(string username, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;

            if (!Attempts.TryGetValue(username, out LoginState? state) || state.LockoutEnd == null)
                return false;

            if (state.LockoutEnd.Value <= DateTime.Now)
            {
                Attempts.Remove(username);
                return false;
            }

            remaining = state.LockoutEnd.Value - DateTime.Now;
            return true;
        }

        public static void RegisterFailedAttempt(string username)
        {
            if (!Attempts.TryGetValue(username, out LoginState? state))
            {
                state = new LoginState();
                Attempts[username] = state;
            }

            state.FailedAttempts++;

            if (state.FailedAttempts >= MaxAttempts)
            {
                state.FailedAttempts = 0;
                state.LockoutEnd = DateTime.Now.Add(LockoutTime);
            }
        }

        public static void Reset(string username)
        {
            Attempts.Remove(username);
        }

        private class LoginState
        {
            public int FailedAttempts { get; set; }
            public DateTime? LockoutEnd { get; set; }
        }
    }
}