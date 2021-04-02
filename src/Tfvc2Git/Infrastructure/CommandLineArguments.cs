using System;
using System.Collections.Generic;
using CommandLine;
using Tfvc2Git.Core.Configuration.Options;

namespace Tfvc2Git.Infrastructure
{
    public static class CommandLineArgumentsExtensions
    {
        public static OptionsBase ParseArguments(this IEnumerable<string> args)
        {
            var options = Parser.Default
                .ParseArguments<
                    ConvertOptions,
                    CollectTfvcAuthorsOptions,
                    CollectTfvcWorkitemsOptions,
                    CheckOptions,
                    CheckUpdateOptions,
                    PushToUpstreamOptions,
                    ExtractNoteOptions>(args)
                .MapResult(
                    (OptionsBase o) => o,
                    err =>
                    {
                        Environment.Exit(1);
                        return null;
                    }
                );
            return options;
        }
    }
}