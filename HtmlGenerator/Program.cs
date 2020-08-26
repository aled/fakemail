using HtmlGenerator;

using MimeKit;
using MimeKit.Cryptography;

using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Security;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fakemail.HtmlGenerator
{
    static class StringExtensions
    {
        public static string Truncate(this string s, int maxLen)
        {
            string ellipsis = "...";

            if (maxLen < 10)
                throw new ArgumentOutOfRangeException(nameof(maxLen));

            if (s.Length <= maxLen)
                return s;

            return s.Substring(0, maxLen - ellipsis.Length) + ellipsis;
        }
    }

    class Program
    {
        string UNKNOWN = "[UNKNOWN]";

        static async Task Main(string[] args)
        {
            Console.WriteLine("HtmlGenerator starting...");

            // temporary storage is at c:\temp\fakemail (windows) or /tmp/fakemail (linux)
            var tempDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:\\temp\\fakemail" : "/tmp/fakemail";

            Directory.CreateDirectory(tempDir);

            var incomingDir = Path.Combine(tempDir, "new");
            var curDir = Path.Combine(tempDir, "cur");
            var htmlRoot = Path.Combine(tempDir, "html");
            var fullMailDir = "mail";
            var summaryMailDir = "summary";
            var summaryFile = "index.html";
            var tempIndexFile = "index.html.tmp";
//#else
//            var incomingDir = "/var/mail/vhosts/fakemail.stream/new";
//            var curDir = "/var/mail/vhosts/fakemail.stream/cur";
//            var htmlRoot = "/var/www/html";
//            var fullMailDir = "mail";
//            var summaryMailDir = "summary";
//            var summaryFile = "index.html";
//            var tempDir = "/tmp";
//            var tempIndexFile = "index.html.tmp";
//#endif           

            await new Program().Run(incomingDir, curDir, htmlRoot, summaryMailDir, fullMailDir, summaryFile, tempDir, tempIndexFile);
        }

        private async Task Run(string incomingDir, string curDir, string htmlRoot, string summaryMailDir, string fullMailDir, string indexFile, string tempDir, string tempIndexFile)
        {
            // Run in a loop. On each iteration:
            // - Delete all processed mail older than 15 mins
            // - Process all new mails
            // - Regenerate the html index
            while (true)
            {
                // Delete all processed mail older than 24 hours. Use file 'last-modified' timestamp for this for simplicity
                var now = DateTime.UtcNow;
                DeleteFilesOlderThan(now.Subtract(TimeSpan.FromHours(24)), Path.Combine(htmlRoot, summaryMailDir));
                DeleteFilesOlderThan(now.Subtract(TimeSpan.FromMinutes(24)), Path.Combine(htmlRoot, fullMailDir)); // keep the full mails around a bit longer, as the user may be seeing an older summary

                // Delete all processed mail execpt for the most recent 100 messages.
                KeepNewestFilesOnly(500, Path.Combine(htmlRoot, summaryMailDir));
                KeepNewestFilesOnly(500, Path.Combine(htmlRoot, fullMailDir));

                // process all new mails
                await ProcessNewMailAsync(incomingDir, curDir, Path.Combine(htmlRoot, summaryMailDir), htmlRoot, fullMailDir);

                // regenerate the html index
                var tempIndexPath = Path.Combine(tempDir, tempIndexFile);
                var indexPath = Path.Combine(htmlRoot, indexFile);
                await RegenerateHtmlIndexAsync(tempIndexPath, indexPath, htmlRoot, summaryMailDir, fullMailDir);

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private void DeleteFilesOlderThan(DateTime timestamp, string directory)
        {
            try
            {
                foreach (var f in new DirectoryInfo(directory).EnumerateFiles())
                {
                    if (f.LastWriteTimeUtc < timestamp)
                    {
                        try
                        {
                            f.Delete();
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"Failed to delete '{Path.Combine(directory, f.Name)}': {e.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to enumerate files in directory '{directory}': {e.Message}");
            }
        }

        private void KeepNewestFilesOnly(int count, string directory)
        {
            try
            {
                var files = new DirectoryInfo(directory).GetFiles();

                foreach (var f in files.OrderBy(f => f.Name).Reverse().Skip(count))
                {
                    try
                    {
                        f.Delete();
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"Failed to delete '{Path.Combine(directory, f.Name)}': {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to list files in directory '{directory}': {e.Message}");
            }
        }

        private async Task ProcessNewMailAsync(string incomingPath, string curDir, string summaryPath, string htmlRoot, string fullMailDir)
        {
            var di = new DirectoryInfo(incomingPath);
            foreach (var file in di.GetFiles().OrderBy(f => f.Name))
            {
                try
                {
                    if (!long.TryParse(file.Name.Substring(0, 10), out var unixTimestamp))
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"Deleting new mail file - failed to parse timestamp: '{file.Name}'");
                        }
                        continue;
                    }

                    var receivedTimestamp = new DateTime(1970, 01, 01).AddSeconds(unixTimestamp);

                    var message = MimeMessage.Load(Path.Combine(incomingPath, file.Name));

                    var mailId = receivedTimestamp.ToString("yyyyMMdd-HHmmss") + "." + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);
                    var fromName = message.From.FirstOrDefault()?.Name.Truncate(100) ?? UNKNOWN;
                    var to = string.Join(',', message.To)?.Truncate(100) ?? UNKNOWN;
                    var subject = message.Subject?.Truncate(100) ?? UNKNOWN;
                    var body = message.TextBody?.Truncate(1000) ?? UNKNOWN;

                    var htmlTimestamp = WebUtility.HtmlEncode(receivedTimestamp.ToString("MMM dd HH:mm"));
                    var htmlFrom = WebUtility.HtmlEncode(fromName);
                    var htmlTo = WebUtility.HtmlEncode(to.Replace("@fakemail.stream", "", StringComparison.OrdinalIgnoreCase));
                    var htmlSubject = WebUtility.HtmlEncode(subject);

                    var fullMailRelativePath = WebUtility.HtmlEncode(Path.Combine(fullMailDir, mailId) + ".html");

                    var summaryEmail = new SummaryEmail
                    {
                        Id = mailId,
                        From = fromName,
                        To = to,
                        Subject = subject,
                        Timestamp = receivedTimestamp,
                    };

                    await File.WriteAllTextAsync(Path.Combine(summaryPath, mailId) + ".json", JsonSerializer.Serialize(summaryEmail), Encoding.UTF8);

                    var fullHtml = "<html><body>\n"
                                    + "<div class='fm-summary'>\n"
                                    + $"  <div class='fm-ts'>{WebUtility.HtmlEncode(receivedTimestamp.ToString("yyyy-MM-dd HH:mm:ss"))}</div>\n"
                                    + $"  <div class='fm-fr'>{WebUtility.HtmlEncode(fromName)}</div>\n"
                                    + $"  <div class='fm-to'>{WebUtility.HtmlEncode(to)}</div>\n"
                                    + $"  <div class='fm-su'>{WebUtility.HtmlEncode(subject)}</div>\n"
                                    + $"  <div class='fm-bo'>{WebUtility.HtmlEncode(body)}</div>\n"
                                    + "</div>\n"
                                    + "</body></html>";

                    await File.WriteAllTextAsync(Path.Combine(htmlRoot, fullMailDir, mailId) + ".html", fullHtml, Encoding.UTF8);

                    file.MoveTo(Path.Combine(curDir, file.Name), true);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Failed to process mail {file.Name}: {e.Message}");
                }
            }
        }

        private async Task RegenerateHtmlIndexAsync(string tempIndexPath, string indexPath, string htmlRoot, string summaryMailDir, string fullMailDir)
        {
            try
            {
                File.Delete(tempIndexPath);
                using (var s = new FileStream(tempIndexPath, FileMode.CreateNew, FileAccess.Write))
                using (var summaryWriter = new StreamWriter(s))
                {
                    var style = "body { background-color: ivory; font-family: Courier, monospace; }\n"
                        + "table { width: 100%; }\n"
                        + "th { text-align: left; font-size: 18px; }\n"
                        + "tr { text-align: left; }\n"
                        + ".small { text-size: 12px; }\n"
                        + ".row-even { background-color: #fbffd6; }\n"
                        + ".row-odd { background-color: #f3ffd6; }\n"
                        + ".tc { position: relative; height: 16px; }\n"
                        + ".tc-content { position: absolute; max-width: 100%; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }\n"
                        + ".tc-spacer { height: 0; overflow: hidden }\n"
                        + ".fm-su { font-style: italic; }\n"
                        + ".fm-bo { font-style: bold; }\n";

                    summaryWriter.Write("<html><head><style>" + style
                        + "</style>"
                        + "<meta http-equiv=\"refresh\" content=\"10\"/>"
                        + "</head><body><h1>fakemail.stream</h1>"
                        + "<div>Plain text emails sent to public@fakemail.stream will be shown here, for 15 minutes. Attachments will be dropped. Detailed logs may be kept.</div>"
                        + "<hr><table><tr><th>Time</th><th>From</th><th>To</th><th>Subject</th></tr>");

                    var evenRow = true;
                    foreach (var summaryFile in new DirectoryInfo(Path.Combine(htmlRoot, summaryMailDir)).GetFiles().OrderBy(f => f.Name))
                    {
                        if (!summaryFile.Name.EndsWith(".json"))
                            continue;

                        try
                        {
                            var summary = JsonSerializer.Deserialize<SummaryEmail>(File.ReadAllText(summaryFile.FullName));

                            var htmlTimestamp = WebUtility.HtmlEncode(summary.Timestamp.ToString("MMM dd HH:mm"));
                            var htmlFrom = WebUtility.HtmlEncode(summary.From);
                            var htmlTo = WebUtility.HtmlEncode(summary.To.Replace("@fakemail.stream", "", StringComparison.OrdinalIgnoreCase));
                            var htmlSubject = WebUtility.HtmlEncode(summary.Subject);

                            var fullMailRelativePath = WebUtility.HtmlEncode(Path.Combine(fullMailDir, summary.Id) + ".html");

                            var summaryHtml = $"<tr class='fm-summary {(evenRow ? "row-even" : "row-odd")}'>\n"
                                            + $"  <td><div class='fm-ts tc'><div class='tc-content'>{htmlTimestamp}</div><div class='tc-spacer'>{htmlTimestamp}</div><span>&nbsp;</span></div></td>\n"
                                            + $"  <td><div class='fm-fr tc'><div class='tc-content'>{htmlFrom}</div><div class='tc-spacer'>{htmlFrom}</div><span>&nbsp;</span></div></td>\n"
                                            + $"  <td><div class='fm-to tc'><div class='tc-content'>{htmlTo}</div><div class='tc-spacer'>{htmlTo}</div>&nbsp;</span></div></td>\n"
                                            + $"  <td><div class='fm-su tc'><div class='tc-content'><a href='{fullMailRelativePath}'>{htmlSubject}</a></div><div class='tc-spacer'>{htmlSubject}</div><span>&nbsp;</span></div></td>\n"
                                           + "</tr>\n";

                            await summaryWriter.WriteLineAsync(summaryHtml);
                            evenRow = !evenRow;
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"Failed to append summary file to index {summaryFile.Name}: {e.Message}");
                        }
                    }

                    summaryWriter.Write("</table>"
                        + $"<hr><div class='small'>Page generated on {DateTime.UtcNow:MMM dd yyyy HH:mm:ss UTC}</div>"
                        + "<div class='small'>Auto-refresh: on</div><hr>"
                        + "</html></body>");
                }

                File.Move(tempIndexPath, indexPath, true);
                File.Delete(tempIndexPath);
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync(e.Message);
            }
        }
    }
}
