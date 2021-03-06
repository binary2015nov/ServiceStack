using System.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Formats
{
    public class CsvFormat : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            //Register the 'text/csv' content-type and serializers (format is inferred from the last part of the content-type)
            appHost.ContentTypes.Register(MimeTypes.Csv,
                SerializeToStream, CsvSerializer.DeserializeFromStream);

            //Add a response filter to add a 'Content-Disposition' header so browsers treat it natively as a .csv file
            appHost.GlobalResponseFilters.Add((req, res, dto) =>
            {
                if (req.ResponseContentType == MimeTypes.Csv)
                {
                    res.AddHeader(HttpHeaders.ContentDisposition, $"attachment;filename={req.OperationName}.csv");
                }
            });

            appHost.GetPlugin<MetadataFeature>()
                ?.AddLink(MetadataFeature.AvailableFeatures, "http://docs.servicestack.net/csv-format", "CSV Format");
            appHost.GetPlugin<MetadataFeature>()
                ?.AddLink(MetadataFeature.AvailableFeatures, "http://docs.servicestack.net/jsv-format", "JSV Format");
        }

        public void SerializeToStream(IRequest request, object requestDto, Stream stream)
        {
            CsvSerializer.SerializeToStream(requestDto, stream);
        }
    }
}