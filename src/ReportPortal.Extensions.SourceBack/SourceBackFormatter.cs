using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Extensions.SourceBack.Pdb;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Internal.Logging;
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
        private readonly ITraceLogger _traceLogger = TraceLogManager.Instance.GetLogger<SourceBackFormatter>();

        public SourceBackFormatter()
        {
            var jsonConfigPath = Path.GetDirectoryName(typeof(SourceBackFormatter).Assembly.Location) + "/ReportPortal.config.json";
            Config = new ConfigurationBuilder().AddJsonFile(jsonConfigPath).AddEnvironmentVariables().Build();
        }

        public int Order => 10;

        private IConfiguration Config { get; }

        public bool FormatLog(CreateLogItemRequest logRequest)
        {
            _traceLogger.Verbose("Received a log request to format.");

            var handled = false;

            var fullMessageBuilder = Config.GetValue("Extensions:SourceBack:WithMarkdownPrefix", false) ? new StringBuilder("!!!MARKDOWN_MODE!!!") : new StringBuilder();

            if (logRequest.Level == LogLevel.Error || logRequest.Level == LogLevel.Fatal)
            {
                _traceLogger.Info($"Parsing exception stacktrace in log message with {logRequest.Level} level...");

                foreach (var line in logRequest.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    var lineWithoutMarkdown = line.Replace("`", @"\`").Replace("__", @"\__");

                    var match = Regex.Match(line, @"\s+\w+\s.*\s\w+\s(.*):\w+\s(\d+)");

                    if (match.Success)
                    {
                        var sourcePath = match.Groups[1].Value;
                        var lineIndex = int.Parse(match.Groups[2].Value) - 1;

                        _traceLogger.Info($"It matches stacktrace. SourcePath: {sourcePath} - LineIndex: {lineIndex}");

                        var sectionBuilder = new StringBuilder();

                        try
                        {
                            lock (_pdbsLock)
                            {
                                if (_pdbs == null)
                                {
                                    _pdbs = new List<PdbFileInfo>();

                                    var currentDirectory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;

                                    _traceLogger.Verbose($"Exploring {currentDirectory} directory for PDB files");

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

                                    // up
                                    var offsetUp = Config.GetValue("Extensions:SourceBack:OffsetUp", 4);
                                    var takeFromIndex = lineIndex - offsetUp;
                                    var missingTopLinesCount = 0;
                                    if (takeFromIndex < 0)
                                    {
                                        missingTopLinesCount = Math.Abs(takeFromIndex);
                                        takeFromIndex = 0;
                                    }

                                    // down
                                    var offsetDown = Config.GetValue("Extensions:SourceBack:OffsetDown", 2);
                                    var takeToIndex = lineIndex + offsetDown;
                                    if (takeToIndex > contentLines.Length - 1) takeToIndex = contentLines.Length - 1;

                                    // and add whitespace to replace it with ►
                                    var frameContentLines = contentLines.Skip(takeFromIndex + 1).Take(takeToIndex - takeFromIndex).Select(l => " " + l).ToList();

                                    var hightlightFrameLineIndex = offsetUp - missingTopLinesCount - 1;
                                    frameContentLines[hightlightFrameLineIndex] = "►" + frameContentLines[hightlightFrameLineIndex].Remove(0, 1);
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
                            lineWithEditLink = lineWithEditLink.Remove(lineWithEditLink.Length - match.Groups[2].Value.Length);

                            var openWith = Config.GetValue("Extensions:SoureBack:OpenWith", "vscode");
                            switch (openWith.ToLowerInvariant())
                            {
                                case "vscode":
                                    lineWithEditLink += $"[{match.Groups[2].Value}](vscode://file/{sourcePath.Replace("\\", "/")}:{lineIndex + 1})";
                                    break;
                            }

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
