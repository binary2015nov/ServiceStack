using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{

    public interface IServiceStackHandler

#if !NETSTANDARD2_0
        : System.Web.IHttpHandler
#endif
    {
        string RequestName { get; }

        IRequest Request { get; }

        IResponse Response { get; }

        Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName);

        void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName);
    }
}