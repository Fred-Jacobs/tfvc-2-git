using System;
using Tfvc2Git.Infrastructure;

namespace Tfvc2Git
{
    internal class Program
    {
        #region Statics & Constants
        private static CompositionRoot _root;
        #endregion

        static Program()
        {
            AppDomain.CurrentDomain.UnhandledException += CompositionRoot.OnUnhandledException;
        }

        private static void Main(string[] args)
        {
            _root = CompositionRoot
                .Bootstrap(args)
                .Validate();

            _root.Run.Execute();
        }
    }
}