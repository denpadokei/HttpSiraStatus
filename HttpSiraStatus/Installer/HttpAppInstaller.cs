using HttpSiraStatus.Models;

namespace HttpSiraStatus.Installer
{
    public class HttpAppInstaller : Zenject.Installer
    {
        public override void InstallBindings()
        {
            _ = this.Container.BindMemoryPool<CutScoreInfoEntity, CutScoreInfoEntity.Pool>().WithInitialSize(16).AsCached();
            _ = this.Container.BindInterfacesAndSelfTo<GameStatus>().AsCached();
            _ = this.Container.BindInterfacesAndSelfTo<StatusManager>().AsCached();
            _ = this.Container.BindInterfacesAndSelfTo<HTTPServer>().AsCached();
        }
    }
}
