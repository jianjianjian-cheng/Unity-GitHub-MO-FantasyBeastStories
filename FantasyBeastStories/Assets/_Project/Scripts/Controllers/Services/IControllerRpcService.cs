using NetworkTarget = Controllers.Network.NetworkTarget;

namespace Controllers.Services
{
    /// <summary>
    /// Domain 层 RPC 调用服务接口
    /// 由 Infrastructure 层的 ControllerRpcBridge 实现
    /// 用途：允许 Domain 层发送 RPC 而不直接依赖 Infrastructure 层的具体类型
    /// </summary>
    public interface IControllerRpcService
    {
        void InvokeRPC(string methodName, NetworkTarget target, params object[] parameters);
    }
}