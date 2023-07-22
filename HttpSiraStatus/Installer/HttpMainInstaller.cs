using HttpSiraStatus.Models;

namespace HttpSiraStatus.Installer
{
    internal class HttpMainInstaller : Zenject.Installer
    {
        public override void InstallBindings()
        {
            _ = this.Container.BindInterfacesAndSelfTo<ModeSelectGetter>().AsCached().NonLazy();
        }
    }
}
