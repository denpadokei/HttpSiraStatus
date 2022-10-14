using HttpSiraStatus.Models;

namespace HttpSiraStatus.Installer
{
    public class HttpAppInstaller : Zenject.Installer
    {
        public override void InstallBindings()
        {
            this.Container.BindMemoryPool<CutScoreInfoEntity, CutScoreInfoEntity.Pool>().WithInitialSize(16);
            this.Container.BindInterfacesAndSelfTo<GameStatus>().AsSingle();
            this.Container.BindInterfacesAndSelfTo<StatusManager>().AsSingle();
            this.Container.BindInterfacesAndSelfTo<HTTPServer>().AsSingle();
        }
    }
}
