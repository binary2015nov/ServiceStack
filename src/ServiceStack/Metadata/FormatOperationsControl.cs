using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using ServiceStack.Metadata;
using ServiceStack.Web;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.Metadata
{
    public class FormatOperationsControl : MetadataControl
    {
        public string Format { get; set; }

        protected override void Render(HtmlTextWriter output)
        {
            var links = OperationNames?.ToDictionary(p => new KeyValuePair<string, string>("?op=" + p, p));
            var operationsPart = new ListTemplate
            {
                Title = MetadataFeature.Operations,
                ListItemsMap = links,
                ListItemTemplate = @"<li><a href=""{0}"">{1}</a></li>"
            }.ToString();

            var renderedTemplate = HtmlTemplates.Format(
                HtmlTemplates.GetFormatOperationsTemplate(),
                Title,
                Format,
                operationsPart,
                Env.VersionString
                );

            output.Write(renderedTemplate);
        }
    }
}
