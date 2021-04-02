using System;
using Tfvc2Git.Core.Configuration.Options;

namespace Tfvc2Git.Core.RunHandlers.Base
{
    public interface IRunHandler
    {
        #region Properties
        Type OptionsType { get; }
        #endregion

        void Execute();
    }

    public interface IRunHandler<T> : IRunHandler where T : OptionsBase
    {
    }
}