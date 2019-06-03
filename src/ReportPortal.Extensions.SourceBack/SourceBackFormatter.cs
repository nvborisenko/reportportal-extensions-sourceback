using ReportPortal.Client.Requests;
using ReportPortal.Shared.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ReportPortal.Extensions.SourceBack
{
    public class SourceBackFormatter : ILogFormatter
    {
        public int Order => 10;

        public bool FormatLog(ref AddLogItemRequest logRequest)
        {
            var handled = false;

            var fullMessageBuilder = new StringBuilder("!!!MARKDOWN_MODE!!!");

            if (logRequest.Level == Client.Models.LogLevel.Error || logRequest.Level == Client.Models.LogLevel.Fatal)
            {
                foreach (var line in logRequest.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    var match = Regex.Match(line, @"\s+\w+\s.*\s\w+\s(.*):\w+\s(\d+)");

                    if (match.Success)
                    {
                        var sourcePath = match.Groups[1].Value;
                        var lineIndex = int.Parse(match.Groups[2].Value) - 1;

                        var sectionBuilder = new StringBuilder();

                        try
                        {
                            if (_pdbs == null)
                            {
                                var currentDirectory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;

                                var pdbFilePaths = Pdb.DirectoryScanner.FindPdbPaths(currentDirectory);

                                _pdbs = new List<Pdb.PdbFileInfo>();

                                foreach (var pdbFilePath in pdbFilePaths)
                                {
                                    var pdbFileInfo = new Pdb.PdbFileInfo(pdbFilePath);

                                    pdbFileInfo.LoadSourceLinks();

                                    _pdbs.Add(pdbFileInfo);
                                }
                            }

                            var pdb = _pdbs.FirstOrDefault(p => p.SourceLinks.ContainsKey(sourcePath));

                            // if defined
                            if (pdb != null)
                            {
                                var content = pdb.GetSourceLinkContent(sourcePath);

                                // if available
                                if (content != null)
                                {

                                    var contentLines = content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);

                                    // above
                                    var takeFromIndex = lineIndex - 4;
                                    if (takeFromIndex < 0) takeFromIndex = 0;

                                    // bottom
                                    var takeToIndex = lineIndex + 2;
                                    if (takeToIndex > contentLines.Length - 1) takeToIndex = contentLines.Length - 1;

                                    // and add whitespace to replace it with ►
                                    var frameContentLines = contentLines.Skip(takeFromIndex + 1).Take(takeToIndex - takeFromIndex).Select(l => " " + l).ToList();
                                    // TODO: calculate new index line
                                    frameContentLines[3] = "►" + frameContentLines[3].Remove(0, 1);
                                    var frameContent = string.Join(Environment.NewLine, frameContentLines);

                                    sectionBuilder.AppendLine($"{Environment.NewLine}```{Environment.NewLine}{frameContent}{Environment.NewLine}```");
                                    sectionBuilder.AppendLine($"[Open in VisualStudioCode](vscode://file/{sourcePath.Replace("\\", "/")}:{lineIndex + 1})");
                                }
                            }
                        }
                        catch (Exception exp)
                        {
                            sectionBuilder.AppendLine($"```{Environment.NewLine}SourceBack error: {exp}{Environment.NewLine}```");
                        }

                        handled = true;

                        fullMessageBuilder.AppendLine($"{line}{Environment.NewLine}{sectionBuilder}");
                    }
                    else
                    {
                        fullMessageBuilder.Append(line + Environment.NewLine);
                    }
                }
            }

            if (handled)
            {
                logRequest.Text = fullMessageBuilder.ToString();
            }

            return handled;
        }

        private List<Pdb.PdbFileInfo> _pdbs;
    }
}
