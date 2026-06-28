namespace Domain.Services
{
    /// <summary>
    /// 生成点查询服务接口
    /// 定义在 Domain 层，由 Application 层的 GameManager 实现
    /// </summary>
    public interface ISpawnPointService
    {
        ISpawnPoint GetSpawnPointById(int id);
    }
}