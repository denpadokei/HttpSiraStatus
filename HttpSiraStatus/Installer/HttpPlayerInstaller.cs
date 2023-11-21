using HttpSiraStatus.Models;
using Zenject;

namespace HttpSiraStatus.Installer
{
    public class HttpPlayerInstaller : Zenject.Installer
    {
        public override void InstallBindings()
        {
            _ = this.Container.BindMemoryPool<CustomGoodCutScoringElement, CustomGoodCutScoringElement.Pool>().WithInitialSize(16);
            _ = this.Container.BindMemoryPool<CustomBadCutScoringElement, CustomBadCutScoringElement.Pool>().WithInitialSize(16);
            _ = this.Container.BindMemoryPool<NoteDataEntity, NoteDataEntity.Pool>().WithInitialSize(16);
            _ = this.Container.BindMemoryPool<SliderDataEntity, SliderDataEntity.Pool>().WithInitialSize(16);
            _ = this.Container.BindInterfacesAndSelfTo<GamePlayDataManager>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
        }
    }
}
