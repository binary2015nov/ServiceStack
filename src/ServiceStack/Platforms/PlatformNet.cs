#if !NETSTANDARD2_0

namespace ServiceStack
{
    public partial class PlatformNet : Platform
    {
        static PlatformNet()
        {
            //MONO doesn't implement this property
            var pi = typeof(System.Web.HttpRuntime).GetProperty("UsingIntegratedPipeline");
            if (pi != null)
            {
                IsIntegratedPipeline = (bool) pi.GetGetMethod().Invoke(null, TypeConstants.EmptyObjectArray);
            }
        }
    }
}

#endif
