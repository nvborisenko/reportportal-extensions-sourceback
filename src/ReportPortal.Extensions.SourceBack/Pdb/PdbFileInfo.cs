using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;

namespace ReportPortal.Extensions.SourceBack.Pdb
{
    class PdbFileInfo
    {
        public PdbFileInfo(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }

        private MetadataReader MetadataReader { get; set; }

        public IDictionary<string, DocumentHandle> SourceLinks { get; set; }

        private IDictionary<string, string> _sourceLinkContents = new ConcurrentDictionary<string, string>();

        public void LoadSourceLinks()
        {
            SourceLinks = new Dictionary<string, DocumentHandle>();

            using (var fileStream = File.OpenRead(FilePath))
            {
                try
                {
                    var metadataReaderProvider = MetadataReaderProvider.FromPortablePdbStream(fileStream);

                    MetadataReader = metadataReaderProvider.GetMetadataReader();

                    foreach (var documentHandle in MetadataReader.Documents)
                    {
                        var document = MetadataReader.GetDocument(documentHandle);

                        var fileLink = MetadataReader.GetString(document.Name);

                        SourceLinks[fileLink] = documentHandle;
                    }
                }
                catch (BadImageFormatException exp)
                {
                    throw new NotSupportedException("The pdb format is not supported.", exp);
                }
            }
        }

        public string GetSourceLinkContent(string link)
        {
            if (_sourceLinkContents.ContainsKey(link))
            {
                return _sourceLinkContents[link];
            }

            var documentHandle = SourceLinks[link];

            string content = null;

            foreach (var cdih in MetadataReader.GetCustomDebugInformation(documentHandle))
            {
                var cdi = MetadataReader.GetCustomDebugInformation(cdih);

                if (MetadataReader.GetGuid(cdi.Kind) == Guid.Parse("0E8A571B-6926-466E-B4AD-8AB04611F5FE"))
                {
                    // embedded content
                    var bytes = MetadataReader.GetBlobBytes(cdi.Value);

                    // decompress content
                    int uncompressedSize = BitConverter.ToInt32(bytes, 0);

                    using (var stream = new MemoryStream(bytes, sizeof(int), bytes.Length - sizeof(int)))
                    {
                        if (uncompressedSize != 0)
                        {
                            using (var decompressed = new MemoryStream(uncompressedSize))
                            {
                                using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
                                {
                                    deflateStream.CopyTo(decompressed);
                                }

                                using (var streamReader = new StreamReader(decompressed))
                                {
                                    decompressed.Position = 0;
                                    content = streamReader.ReadToEnd();
                                }
                            }
                        }
                        else
                        {
                            using (var streamReader = new StreamReader(stream))
                            {
                                stream.Position = 0;
                                content = streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }

            if (content == null)
            {
                // from external link
                if (File.Exists(link))
                {
                    content = File.ReadAllText(link);
                }
            }

            _sourceLinkContents[link] = content;

            return content;
        }
    }
}
