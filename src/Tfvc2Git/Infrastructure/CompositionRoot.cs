using System;
using System.Linq;
using Serilog;
using Serilog.Core;
using SimpleInjector;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.RunHandlers.Base;

namespace Tfvc2Git.Infrastructure
{
    internal class CompositionRoot
    {
        #region Properties
        public OptionsBase Options { get; }

        public Tfvc2GitConfig RunConfig { get; }

        public IRunHandler Run =>
            _container.GetAllInstances<IRunHandler>()
                .Where(x => x.OptionsType != null)
                .SingleOrDefault(x => x.OptionsType == Options.GetType());
        #endregion

        #region Fields
        private readonly Container _container;
        #endregion

        private CompositionRoot()
        {
        }

        internal CompositionRoot(OptionsBase options, Tfvc2GitConfig config, Container container)
        {
            Options = options;
            RunConfig = config;
            _container = container;
        }

        public static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Log.Logger == Logger.None)
                Console.WriteLine(e.ExceptionObject.ToString());

            Log.Fatal((Exception) e.ExceptionObject, "Fatal unhandled exception..");
            Environment.Exit(1);
        }

        internal static CompositionRoot Bootstrap(string[] args) => Bootstrap(new Tfvc2GitConfig(), args);

        internal static CompositionRoot Bootstrap(Tfvc2GitConfig config, string[] args)
        {
            var options = args.ParseArguments().ConfigureLogging();
            var container = options.ConfigureContainer(config);
            var root = new CompositionRoot(options, config, container);

            return root;
        }

        internal CompositionRoot Validate()
        {
#if DEBUG
            _container.Verify(VerificationOption.VerifyAndDiagnose);
#else
            _container.Verify(VerificationOption.VerifyOnly);
#endif

            return this;
        }
    }
}