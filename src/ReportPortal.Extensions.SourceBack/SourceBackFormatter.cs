using ReportPortal.Client.Requests;
using ReportPortal.Extensions.SourceBack.Pdb;
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
                    var lineWithoutMarkdown = line.Replace("`", @"\`").Replace("__", @"\__");

                    var match = Regex.Match(line, @"\s+\w+\s.*\s\w+\s(.*):\w+\s(\d+)");

                    if (match.Success)
                    {
                        var sourcePath = match.Groups[1].Value;
                        var lineIndex = int.Parse(match.Groups[2].Value) - 1;

                        var sectionBuilder = new StringBuilder();

                        try
                        {
                            lock (_pdbsLock)
                            {
                                if (_pdbs == null)
                                {
                                    _pdbs = new List<PdbFileInfo>();

                                    var currentDirectory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;

                                    var pdbFilePaths = DirectoryScanner.FindPdbPaths(currentDirectory);

                                    foreach (var pdbFilePath in pdbFilePaths)
                                    {
                                        var pdbFileInfo = new PdbFileInfo(pdbFilePath);

                                        try
                                        {
                                            pdbFileInfo.LoadSourceLinks();
                                        }
                                        catch (NotSupportedException)
                                        {
                                            sectionBuilder.AppendLine($"`{pdbFilePath} format is not supported. Try to change it to 'portable' or 'embedded'.`");
                                        }

                                        _pdbs.Add(pdbFileInfo);
                                    }
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
                                    var contentLines = content.Replace("\r\n", "\n").Split(new string[] { "\n" }, StringSplitOptions.None);

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

                                    sectionBuilder.AppendLine($"```{Environment.NewLine}{frameContent}{Environment.NewLine}```");
                                }
                            }
                        }
                        catch (Exception exp)
                        {
                            sectionBuilder.AppendLine($"```{Environment.NewLine}SourceBack error: {exp}{Environment.NewLine}```");
                        }

                        handled = true;

                        if (!string.IsNullOrEmpty(sectionBuilder.ToString()))
                        {
                            var sourceFileName = Path.GetFileName(sourcePath);
                            var lineWithEditLink = lineWithoutMarkdown.Replace("\\" + sourceFileName, $"\\\\**{sourceFileName}**");
                            lineWithEditLink = lineWithEditLink.Remove(lineWithEditLink.Length - match.Groups[2].Value.Length) + $"[{match.Groups[2].Value}](vscode://file/{sourcePath.Replace("\\", "/")}:{lineIndex + 1})";


                            fullMessageBuilder.AppendLine($"{lineWithEditLink}{Environment.NewLine}{sectionBuilder}");
                        }
                        else
                        {
                            fullMessageBuilder.AppendLine(lineWithoutMarkdown);
                        }
                    }
                    else
                    {
                        fullMessageBuilder.AppendLine(lineWithoutMarkdown);
                    }
                }
            }

            if (handled)
            {
                logRequest.Text = fullMessageBuilder.ToString();
            }

            return handled;
        }

        private static readonly object _pdbsLock = new object();
        private static List<PdbFileInfo> _pdbs;
    }
}
