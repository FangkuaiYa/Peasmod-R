using System.Linq;
using AmongUs.GameOptions;
using PeasAPI.CustomRpc;
using PeasAPI.Options;
using PeasAPI.Roles;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Extensions;
using UnityEngine;

namespace PeasAPI
{
    public static class Extensions
    {
        /// <summary>
        /// Gets a <see cref="PlayerControl"/> from it's id
        /// </summary>
        public static PlayerControl GetPlayer(this byte id)
        {
            return GameData.Instance.GetPlayerById(id).Object;
        }
        
        /// <summary>
        /// Gets a <see cref="NetworkedPlayerInfo"/> from it's id
        /// </summary>
        public static NetworkedPlayerInfo GetPlayerInfo(this byte id)
        {
            return GameData.Instance.GetPlayerById(id);
        }

        public static bool IsLocal(this PlayerControl player)
        {
            return player.PlayerId == PlayerControl.LocalPlayer.PlayerId;
        }

        public static void ToggleOutline(this PlayerControl player, bool active, Color color = default)
        {
            if (active)
            {
                player.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", 1f);
                player.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", color);
                return;
            }
            
            player.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", 0f);
        }
        
        public static Color SetAlpha(this Color original, float alpha)
        {
            return new Color(original.r, original.g, original.b, alpha);
        }

        public static Vector3 SetX(this Vector3 original, float x)
        {
            return new Vector3(x, original.y, original.z);
        }
        
        public static Vector3 SetY(this Vector3 original, float y)
        {
            return new Vector3(original.x, y, original.z);
        }
        
        public static Vector3 SetZ(this Vector3 original, float z)
        {
            return new Vector3(original.x, original.y, z);
        }
        
        /// <summary>
        /// Gets the text color from a <see cref="Color"/>
        /// </summary>
        public static string GetTextColor(this Color color)
        {
            var r = Mathf.RoundToInt(color.r * 255f).ToString("X2");
            
            var g = Mathf.RoundToInt(color.g * 255f).ToString("X2");
            
            var b = Mathf.RoundToInt(color.b * 255f).ToString("X2");
            
            var a = Mathf.RoundToInt(color.a * 255f).ToString("X2");

            return $"<color=#{r}{g}{b}{a}>";
        }

        public static string GetTranslation(this StringNames stringName)
        {
            return TranslationController.Instance.GetString(stringName);
        }

        #region Roles

        /// <summary>
        /// Gets the role of a <see cref="PlayerControl"/>
        /// </summary>
        public static BaseRole GetCustomRole(this PlayerControl player)
        {
            foreach (var _role in Roles.RoleManager.Roles)
            {
                if (_role.Members.Contains(player.PlayerId))
                    return _role;
            }

            return null;
        }

        public static bool IsCustomRole(this PlayerControl player)
        {
            if (player == null)
                return false;

            return GetCustomRole(player) != null;
        }

        /// <summary>
        /// Gets the role of a <see cref="NetworkedPlayerInfo"/>
        /// </summary>
        public static BaseRole GetCustomRole(this NetworkedPlayerInfo player)
        {
            foreach (var _role in Roles.RoleManager.Roles)
            {
                if (_role.Members.Contains(player.PlayerId))
                    return _role;
            }

            return null;
        }

        /// <summary>
        /// Checks if a <see cref="PlayerControl"/> is a certain role
        /// </summary>
        public static bool IsCustomRole(this PlayerControl player, BaseRole role) => player.GetCustomRole() == role;

        #nullable enable
        /// <summary>
        /// Gets the role of a <see cref="PlayerControl"/>
        /// </summary>
        public static T? GetCustomRole<T>(this PlayerControl player) where T : BaseRole
            => player.GetCustomRole() as T;

        /// <summary>
        /// Checks if a <see cref="PlayerControl"/> is a certain role
        /// </summary>
        public static bool IsCustomRole<T>(this PlayerControl player) where T : BaseRole
            => player.GetCustomRole<T>() != null;

        /// <summary>
        /// Sets the role of a <see cref="PlayerControl"/>
        /// </summary>
        public static void SetCustomRole(this PlayerControl player, BaseRole? role)
        {
            var oldRole = Roles.RoleManager.Roles.Where(r => r.Members.Contains(player.PlayerId)).ToList();
            if (oldRole.Count != 0)
                oldRole[0].Members.Remove(player.PlayerId);
            
            if (role != null)
            {
                role.Members.Add(player.PlayerId);
            }
            else if (player.IsLocal())
            {
                var isImpostor = player.Data.Role.IsImpostor;
                var isDead = player.Data.IsDead;
                
                if (oldRole.Count != 0)
                    GameObject.Find(oldRole[0].Name + "Task").Destroy();
                HudManager.Instance.SabotageButton.gameObject.SetActive(isImpostor);
                HudManager.Instance.KillButton.gameObject.SetActive(isImpostor && !isDead);
                HudManager.Instance.ImpostorVentButton.gameObject.SetActive(isImpostor && !isDead);
                
                player.cosmetics.nameText.color = isImpostor ? Palette.ImpostorRed : Color.white;
                player.cosmetics.nameText.text = player.name;
            }
        }
        
        public static void SetVanillaRole(this PlayerControl player, RoleTypes role)
        {
            player.roleAssigned = true;
            if (RoleManager.IsGhostRole(role))
            {
                RoleManager.Instance.SetRole(player, role);
                player.Data.Role.SpawnTaskHeader(player);
                return;
            }
            HudManager.Instance.MapButton.gameObject.SetActive(true);
            HudManager.Instance.ReportButton.gameObject.SetActive(true);
            HudManager.Instance.UseButton.gameObject.SetActive(true);
            PlayerControl.LocalPlayer.RemainingEmergencies = GameOptionsManager.Instance.currentNormalGameOptions.NumEmergencyMeetings;
            RoleManager.Instance.SetRole(player, role);
            player.Data.Role.SpawnTaskHeader(player);
            if (!DestroyableSingleton<TutorialManager>.InstanceExists)
            {
                if (Utility.GetAllPlayers().All(pc => pc.roleAssigned))
                {
                    Utility.GetAllPlayers().ForEach(pc =>
                    {
                        if (pc.Data.Role.TeamType == PlayerControl.LocalPlayer.Data.Role.TeamType)
                        {
                            pc.cosmetics.nameText.color = pc.Data.Role.NameColor;
                        }
                        else
                        {
                            pc.cosmetics.nameText.color = Palette.White;
                        }
                    });
                }
            }
        }

        public static void RpcSetVanillaRole(this PlayerControl player, RoleTypes role)
        {
            Rpc<RpcSetVanillaRole>.Instance.Send(new RpcSetVanillaRole.Data(player, role));
            
            player.SetVanillaRole(role);
        }

        /// <summary>
        /// Sets the role of a <see cref="PlayerControl"/>
        /// </summary>
        public static void RpcSetRole(this PlayerControl player, BaseRole? role)
        {
            Rpc<RpcSetRole>.Instance.Send(new RpcSetRole.Data(player, role));

            player.SetCustomRole(role);
        }

        public static bool IsOnSameTeam(this PlayerControl player, PlayerControl otherPlayer)
        {
            var role = player.GetCustomRole();
            var otherRole = otherPlayer.GetCustomRole();

            if (role != null)
            {
                switch (role.Team)
                {
                    case Team.Alone:
                        return false;
                    case Team.Role:
                        return role.Id == otherRole.Id;
                    case Team.Crewmate:
                        if (otherRole != null)
                            return otherRole.Team == Team.Crewmate;
                        else
                            return !otherPlayer.Data.Role.IsImpostor;
                    case Team.Impostor:
                        if (otherRole != null)
                            return otherRole.Team == Team.Impostor;
                        else
                            return otherPlayer.Data.Role.IsImpostor;
                }
            }
            else if (otherRole == null)
            {
                return player.Data.Role.IsImpostor == otherPlayer.Data.Role.IsImpostor;
            }
            else
            {
                switch (otherRole.Team)
                {
                    case Team.Alone:
                        return false;
                    case Team.Role:
                        return false;
                    case Team.Crewmate:
                        return !otherPlayer.Data.Role.IsImpostor;
                    case Team.Impostor:
                        return otherPlayer.Data.Role.IsImpostor;
                }
            }

            return false;
        }

        public static RoleTypes GetSimpleRoleType(this RoleTypes role)
        {
            switch (role)
            {
                case RoleTypes.Engineer:
                    return RoleTypes.Crewmate;
                case RoleTypes.Scientist:
                    return RoleTypes.Crewmate;
                case RoleTypes.GuardianAngel:
                    return RoleTypes.Crewmate;
                case RoleTypes.Shapeshifter:
                    return RoleTypes.Impostor;
                case RoleTypes.Crewmate:
                    return RoleTypes.Crewmate;
                case RoleTypes.Impostor:
                    return RoleTypes.Impostor;
            }

            return RoleTypes.Crewmate;
        }

        #endregion Roles
                
        #region Position
        public static Vector3 SetX(this Transform transform, float x)
        {
            var currentPosition = transform.position;
            var vector = new Vector3(x, currentPosition.y, currentPosition.z);
            transform.position = vector;
            return vector;
        }
        
        public static Vector3 SetY(this Transform transform, float y)
        {
            var currentPosition = transform.position;
            var vector = new Vector3(currentPosition.x, y, currentPosition.z);
            transform.position = vector;
            return vector;
        }
        
        public static Vector3 SetZ(this Transform transform, float z)
        {
            var currentPosition = transform.position;
            var vector = new Vector3(currentPosition.x, currentPosition.y, z);
            transform.position = vector;
            return vector;
        }
        #endregion
    }
}