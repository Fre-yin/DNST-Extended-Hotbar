using System.Collections.Generic;

namespace ExtendedHotbar.Helper
{
    // Optional profile, independent of modloader and installation. Never used at startup.
    internal static class HotbarKeyProfile
    {
        internal const string Action = "Hotbar-Tastenprofil anwenden";
        internal static bool Affected(int type)
        {
            return Profiles.Character(type) || type >= 34 && type <= 37 || type == 60
                || type >= 12005 && type <= 12010 || type == 12101 || type == 12102;
        }
        internal static string Bindings()
        {
            var rows = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                int digit = i == 9 ? 48 : 49 + i, skill = i < 4 ? 34 + i : 12001 + i;
                rows.Add(Profiles.Row(skill, 0, digit, 0)); rows.Add(Profiles.Row(skill, 1, 0, 0));
                rows.Add(Profiles.Row(4 + i, 0, digit, 304)); rows.Add(Profiles.Row(4 + i, 1, 0, 0));
                rows.Add(Profiles.Row(14 + i, 0, 0, 0)); rows.Add(Profiles.Row(14 + i, 1, 0, 0));
            }
            var items = new[] { 60, 12101, 12102 }; var keys = new[] { 113, 101, 114 };
            for (int i = 0; i < items.Length; i++)
            {
                rows.Add(Profiles.Row(items[i], 0, keys[i], 0)); rows.Add(Profiles.Row(items[i], 1, 0, 0));
            }
            return "[" + string.Join(",", rows) + "]";
        }
    }
}
