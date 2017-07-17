using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;
#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class TemplatePage
    {
        public IVirtualFile File { get; }
        public StringSegment FileContents { get; private set; }
        public StringSegment BodyContents { get; private set; }
        public Dictionary<string, object> Args { get; private set; }
        public TemplatePage LayoutPage { get; set; }
        public List<PageFragment> PageFragments { get; set; }
        public DateTime LastModified { get; set; }
        public bool HasInit { get; private set; }

        public TemplateContext Context { get; }
        public PageFormat Format { get; }
        private readonly object semaphore = new object();

        public bool IsTempFile => File.Directory.VirtualPath == TemplateConstants.TempFilePath;
        public string VirtualPath => IsTempFile ? "{temp file}" : File.VirtualPath;

        public TemplatePage(TemplateContext context, IVirtualFile file, PageFormat format=null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            File = file ?? throw new ArgumentNullException(nameof(file));
            
            Format = format ?? Context.GetFormat(File.Extension);
            if (Format == null)
                throw new ArgumentException($"File with extension '{File.Extension}' is not a registered PageFormat in Context.PageFormats", nameof(file));
        }

        public async Task<TemplatePage> Init()
        {
            if (HasInit)
            {
                if (!Context.DebugMode && !Context.CheckForModifiedPages)
                    return this;

                File.Refresh();
                if (File.LastModified == LastModified)
                    return this;
            }
            
            return await Load();
        }

        public async Task<TemplatePage> Load()
        {
            string contents;
            using (var stream = File.OpenRead())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                contents = await reader.ReadToEndAsync();
            }

            var lastModified = File.LastModified;
            var fileContents = contents.ToStringSegment();
            var pageVars = new Dictionary<string, object>();

            var pos = 0;
            var bodyContents = fileContents;
            fileContents.AdvancePastWhitespace().TryReadLine(out StringSegment line, ref pos);
            if (line.StartsWith(Format.ArgsPrefix))
            {
                while (fileContents.TryReadLine(out line, ref pos))
                {
                    if (line.Trim().Length == 0)
                        continue;


                    if (line.StartsWith(Format.ArgsSuffix))
                        break;

                    var kvp = line.SplitOnFirst(':');
                    pageVars[kvp[0].Trim().ToString()] = kvp.Length > 1 ? kvp[1].Trim().ToString() : "";
                }
                
                //When page has variables body starts from first non whitespace after variable's end  
                bodyContents = fileContents.SafeSubsegment(pos).AdvancePastWhitespace();
            }

            var pageFragments = TemplatePageUtils.ParseTemplatePage(bodyContents);

            lock (semaphore)
            {
                LastModified = lastModified;
                FileContents = fileContents;
                Args = pageVars;
                BodyContents = bodyContents;
                PageFragments = pageFragments;

                HasInit = true;
                LayoutPage = Format.ResolveLayout(this);
            }

            if (LayoutPage != null)
            {
                if (!LayoutPage.HasInit)
                {
                    await LayoutPage.Load();
                }
                else if (Context.CheckForModifiedPages || Context.DebugMode)
                {
                    LayoutPage.File.Refresh();
                    if (LayoutPage.File.LastModified != LayoutPage.LastModified)
                        await LayoutPage.Load();
                }
            }

            return this;
        }
    }
}