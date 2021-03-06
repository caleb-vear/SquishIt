using System.Collections.Generic;
using System.Linq;
using System.Text;
using SquishIt.Framework.Base;
using SquishIt.Framework.Files;
using SquishIt.Framework.Minifiers;
using SquishIt.Framework.Utilities;

namespace SquishIt.Framework.JavaScript
{
    public class JavaScriptBundle : BundleBase<JavaScriptBundle>
    {
        const string JS_TEMPLATE = "<script type=\"text/javascript\" {0}src=\"{1}\" defer></script>";
        const string TAG_FORMAT = "<script type=\"text/javascript\">{0}</script>";

        const string CACHE_PREFIX = "js";

        bool deferred;

        protected override IMinifier<JavaScriptBundle> DefaultMinifier
        {
            get { return Configuration.Instance.DefaultJsMinifier(); }
        }

        protected override IEnumerable<string> allowedExtensions
        {
            get { return bundleState.AllowedExtensions.Union(Bundle.AllowedGlobalExtensions.Union(Bundle.AllowedScriptExtensions)); }
        }

        protected override IEnumerable<string> disallowedExtensions
        {
            get { return Bundle.AllowedStyleExtensions; }
        }

        protected override string defaultExtension
        {
            get { return ".JS"; }
        }

        protected override string tagFormat
        {
            get { return bundleState.Typeless ? TAG_FORMAT.Replace(" type=\"text/javascript\"", "") : TAG_FORMAT; }
        }

        public JavaScriptBundle()
            : base(new FileWriterFactory(new RetryableFileOpener(), 5), new FileReaderFactory(new RetryableFileOpener(), 5), new DebugStatusReader(), new CurrentDirectoryWrapper(), new Hasher(new RetryableFileOpener()), new BundleCache()) { }

        public JavaScriptBundle(IDebugStatusReader debugStatusReader)
            : base(new FileWriterFactory(new RetryableFileOpener(), 5), new FileReaderFactory(new RetryableFileOpener(), 5), debugStatusReader, new CurrentDirectoryWrapper(), new Hasher(new RetryableFileOpener()), new BundleCache()) { }

        public JavaScriptBundle(IDebugStatusReader debugStatusReader, IFileWriterFactory fileWriterFactory, IFileReaderFactory fileReaderFactory, ICurrentDirectoryWrapper currentDirectoryWrapper, IHasher hasher, IBundleCache bundleCache) :
            base(fileWriterFactory, fileReaderFactory, debugStatusReader, currentDirectoryWrapper, hasher, bundleCache) { }

        protected override string Template
        {
            get
            {
                var val = bundleState.Typeless ? JS_TEMPLATE.Replace("type=\"text/javascript\" ", "") : JS_TEMPLATE;
                return deferred ? val : val.Replace(" defer", "");
            }
        }

        protected override string CachePrefix
        {
            get { return CACHE_PREFIX; }
        }

        protected override string ProcessFile(string file, string outputFile)
        {
            var preprocessors = FindPreprocessors(file);
            if(preprocessors != null)
            {
                return PreprocessFile(file, preprocessors);
            }
            return ReadFile(file);
        }

        protected override void AggregateContent(List<Asset> assets, StringBuilder sb, string outputFile)
        {
            assets.SelectMany(a => a.IsArbitrary
                                       ? new[] { PreprocessArbitrary(a) }.AsEnumerable()
                                       : GetFilesForSingleAsset(a).Select(f => ProcessFile(f, outputFile)))
                .ToList()
                .Distinct()
                .Aggregate(sb, (b, s) =>
                {
                    b.Append(s + "\n");
                    return b;
                });
        }

        public JavaScriptBundle WithDeferredLoad()
        {
            deferred = true;
            return this;
        }
    }
}