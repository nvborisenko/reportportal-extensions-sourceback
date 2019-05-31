using System;
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

        public void LoadSourceLinks()
        {
            SourceLinks = new Dictionary<string, DocumentHandle>();

            using (var fileStream = File.OpenRead(FilePath))
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
        }

        public string GetSourceLinkContent(string link)
        {
            var documentHandle = SourceLinks[link];

            byte[] bytes = null;

            foreach (var cdih in MetadataReader.GetCustomDebugInformation(documentHandle))
            {
                var cdi = MetadataReader.GetCustomDebugInformation(cdih);

                if (MetadataReader.GetGuid(cdi.Kind) == Guid.Parse("0E8A571B-6926-466E-B4AD-8AB04611F5FE"))
                {
                    // embedded content
                    bytes = MetadataReader.GetBlobBytes(cdi.Value);

                    // decompress content
                    int uncompressedSize = BitConverter.ToInt32(bytes, 0);

                    var stream = new MemoryStream(bytes, sizeof(int), bytes.Length - sizeof(int));

                    if (uncompressedSize != 0)
                    {
                        var decompressed = new MemoryStream(uncompressedSize);

                        using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(decompressed);
                        }

                        stream = decompressed;
                    }

                    stream.Position = 0;
                    StreamReader streamReader = new StreamReader(stream);

                    return streamReader.ReadToEnd();
                }
            }

            if (bytes == null)
            {
                // from external link
                if (File.Exists(link))
                {
                    return string.Join(Environment.NewLine, File.ReadAllLines(link));
                }
            }

            return null;
        }
    }
}
