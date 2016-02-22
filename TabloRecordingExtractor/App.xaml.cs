using System.Windows;
using CommonServiceLocator.NinjectAdapter.Unofficial;
using Microsoft.Practices.ServiceLocation;
using Ninject;

namespace TabloRecordingExtractor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            IKernel kernel = new StandardKernel(new DependencyInjection());

            NinjectServiceLocator locator = new NinjectServiceLocator(kernel);

            ServiceLocator.SetLocatorProvider(() => locator);
        }
    }
}
