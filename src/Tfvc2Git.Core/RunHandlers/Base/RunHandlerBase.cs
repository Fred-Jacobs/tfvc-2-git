using System;
using Serilog;
using Tfvc2Git.Core.Configuration;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.RunHandlers.Base
{
    public abstract class RunHandlerBase<T> : IRunHandler<T> where T : OptionsBase
    {
        #region Properties
        protected T Options => _config ?? (_config = (T) Config.Options);
        protected Tfvc2GitConfig Config { get; }
        protected ILogger Log { get; }
        public Type OptionsType => typeof(T);
        #endregion

        #region Fields
        private readonly ConfigBuilder _configBuilder;
        private T _config;
        #endregion

        private RunHandlerBase()
        {
            Log = Serilog.Log.Logger.ForContext(GetType());
        }

        protected RunHandlerBase(ConfigBuilder configBuilder, Tfvc2GitConfig config) : this()
        {
            _configBuilder = configBuilder;
            Config = config;
        }

        protected virtual void PreProcess()
        {
        }

        protected abstract void Process();

        #region
        public void Execute()
        {
            _configBuilder.Hydrate();
            Log.Information("{RunHandler}.Execute()", GetType().Name);
            PreProcess();
            Process();
        }
        #endregion
    }
}