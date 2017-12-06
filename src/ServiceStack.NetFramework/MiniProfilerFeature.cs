﻿using ServiceStack.MiniProfiler;

namespace ServiceStack
{
    public class MiniProfilerFeature : IPlugin
    {
        public MiniProfilerFeature()
        {
            Profiler.Current = new MiniProfilerAdapter();
        }

        public void Register(IAppHost appHost)
        {
            appHost.RawHttpHandlers.Add(MiniProfiler.UI.MiniProfilerHandler.MatchesRequest);

            appHost.GetPlugin<MetadataFeature>()?
                .AddLink(MetadataFeature.AvailableFeatures, "http://docs.servicestack.net/built-in-profiling", nameof(MiniProfilerFeature));
        }
    }
}