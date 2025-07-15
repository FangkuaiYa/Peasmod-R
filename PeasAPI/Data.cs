using System.Collections.Generic;
using UnityEngine;

namespace PeasAPI
{
    public class Data
    {
        public readonly struct CustomIntroScreen
        {
            public readonly bool OverrideTeam;
            public readonly string Team;
            public readonly string TeamDescription;
            public readonly Color TeamColor;
            public readonly List<byte> TeamMembers;
            public readonly bool OverrideRole;
            public readonly string Role;
            public readonly string RoleDescription;
            public readonly Color RoleColor;

            public CustomIntroScreen(bool overrideTeam = false, string team = null, string teamDescription = null, Color? teamColor = null, List<byte> teamMembers = null, bool overrideRole = false, string role = null, string roleDescription= null, Color? roleColor = null)
            {
                OverrideTeam = overrideTeam;
                Team = team;
                TeamColor = teamColor.GetValueOrDefault();
                TeamDescription = teamDescription;
                TeamMembers = teamMembers;
                OverrideRole = overrideRole;
                Role = role;
                RoleDescription = roleDescription;
                RoleColor = roleColor.GetValueOrDefault();
            }
        }
    }
}