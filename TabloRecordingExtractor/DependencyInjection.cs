using Ninject.Modules;

namespace TabloRecordingExtractor
{
    public class DependencyInjection : NinjectModule
    {
        public override void Load()
        {
            Bind<IMediaNamingConvention>().To<PlexMediaNaming>();
            Bind<IExtractTabloRecordings>().To<ExtractTabloRecordingsWatchMethod>();
        }
    }
}
