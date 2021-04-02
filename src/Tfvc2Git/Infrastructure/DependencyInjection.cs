using SimpleInjector;
using Tfvc2Git.Core.Configuration;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Infrastructure;
using Tfvc2Git.Core.Mappers;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;
using Tfvc2Git.Core.RunHandlers.Base;

namespace Tfvc2Git.Infrastructure
{
    public static class DependencyInjectionExtensions
    {
        public static Container ConfigureContainer(this OptionsBase options, Tfvc2GitConfig config)
        {
            var c = new Container();

            c.Options.DefaultLifestyle = Lifestyle.Singleton;

            c.RegisterSingleton(() => config);
            c.RegisterSingleton(() => options);
            c.RegisterSingleton<FileSystem>();
            c.RegisterSingleton<AuthorsMap>();
            c.RegisterSingleton<Json>();

            c.Register<TfvcRepository>();
            c.Register<Tfvc2GitRepository>();
            c.Register<ConfigBuilder>();

            c.Register(typeof(IRunHandler<>), typeof(IRunHandler<>).Assembly);
            c.Collection.Register(typeof(IRunHandler), typeof(IRunHandler<>).Assembly);

            return c;
        }
    }
}