using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace PeasAPI.CustomRpc
{
    [RegisterCustomRpc((uint) CustomRpcCalls.ResetRoles)]
    public class RpcResetRoles : PlayerCustomRpc<PeasAPI>
    {
        public RpcResetRoles(PeasAPI plugin, uint id) : base(plugin, id)
        {
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;
        public override void Handle(PlayerControl innerNetObject)
        {
            Roles.RoleManager.ResetRoles();
        }
    }
}