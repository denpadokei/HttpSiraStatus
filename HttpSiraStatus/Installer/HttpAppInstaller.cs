using HttpSiraStatus.Models;

namespace HttpSiraStatus.Installer
{
    public class HttpAppInstaller : Zenject.Installer
    {
        public override void InstallBindings()
        {
            _ = this.Container.BindMemoryPool<CutScoreInfoEntity, CutScoreInfoEntity.Pool>().WithInitialSize(16);
            _ = this.Container.BindInterfacesAndSelfTo<GameStatus>().AsSingle();
            _ = this.Container.BindInterfacesAndSelfTo<StatusManager>().AsSingle();
            _ = this.Container.BindInterfacesAndSelfTo<HTTPServer>().AsSingle();
        }
    }
}
