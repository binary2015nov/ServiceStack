#if !NETSTANDARD1_6

namespace ServiceStack
{
    public partial class PlatformNet : Platform
    {
        public PlatformNet()
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
