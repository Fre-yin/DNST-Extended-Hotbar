using System.Collections.Generic;

namespace ExtendedHotbar.Helper
{
    // Optional profile, independent of modloader and installation. Never used at startup.
    internal static class HotbarKeyProfile
    {
        internal const string Action = "Hotbar-Tastenprofil anwenden";
        internal static bool Affected(int type)
        {
            return Profiles.Character(type) || Profiles.Skill(type) || type == Profiles.ItemActionType
                || type >= Profiles.HotbarProfileItemStart && type <= Profiles.HotbarProfileItemEnd
                || type == Profiles.ProfileItemUseType || type == Profiles.ProfileItemUnsetType;
        }
        internal static string Bindings()
        {
            var rows = new List<string>();
            for (int i = 0; i < Profiles.CharacterRowCount; i++)
            {
                int digit = i == 9 ? 48 : 49 + i;
                int skill;
                if (i < Profiles.SkillRowCount) skill = Profiles.SkillStart + i;
                else skill = Profiles.ProfileSkillStart + i;
                rows.Add(Profiles.Row(skill, 0, digit, 0)); rows.Add(Profiles.Row(skill, 1, 0, 0));
                rows.Add(Profiles.Row(Profiles.CharacterStart + i, 0, digit, Profiles.ShiftModifier)); rows.Add(Profiles.Row(Profiles.CharacterStart + i, 1, 0, 0));
                rows.Add(Profiles.Row(Profiles.CharacterAndSkillRowSplit + i, 0, 0, 0)); rows.Add(Profiles.Row(Profiles.CharacterAndSkillRowSplit + i, 1, 0, 0));
            }
            var items = new[] { Profiles.ItemActionType, Profiles.ProfileItemUseType, Profiles.ProfileItemUnsetType };
            var keys = new[] { 113, 101, 114 };
            for (int i = 0; i < items.Length; i++)
            {
                rows.Add(Profiles.Row(items[i], 0, keys[i], 0)); rows.Add(Profiles.Row(items[i], 1, 0, 0));
            }
            return "[" + string.Join(",", rows) + "]";
        }
    }
}
