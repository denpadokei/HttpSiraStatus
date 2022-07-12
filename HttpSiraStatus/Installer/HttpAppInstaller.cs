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
            this.Container.BindMemoryPool<V2BeatmapEventInfomation, V2BeatmapEventInfomation.Pool>().WithInitialSize(16);
            this.Container.BindMemoryPool<V3BeatmapEventInfomation, V3BeatmapEventInfomation.Pool>().WithInitialSize(16);
        }
    }
}
