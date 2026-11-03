using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Hd2Qd.ResourceNames;
using Xunit;

namespace Hd2Qd.ResourceNames.Tests
{
    /// <summary>The list fetched from a server on this machine (HD2QD_RESOURCE_NAMES_URL) into a
    /// settings folder of the test's own. Each rule has a must-pass and a must-fail.</summary>
    public class ResourceNamesTests : IDisposable
    {
        readonly string home = Path.Combine(Path.GetTempPath(), "hd2qd-resource-names-" + Guid.NewGuid().ToString("N"));
        readonly HttpListener server = new HttpListener();
        string body;
        string archives = Archives(150);
        readonly int port = 8800 + new Random().Next(900);

        public ResourceNamesTests()
        {
            Directory.CreateDirectory(home);
            server.Prefixes.Add("http://127.0.0.1:" + port + "/");
            server.Start();
            new Thread(() =>
            {
                while (server.IsListening)
                {
                    HttpListenerContext c;
                    try { c = server.GetContext(); }
                    catch (HttpListenerException) { return; }
                    var bytes = Encoding.UTF8.GetBytes((c.Request.Url.AbsolutePath.EndsWith("archivehashes.json") ? archives : body) ?? "");
                    c.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    c.Response.Close();
                }
            }) { IsBackground = true }.Start();
            Environment.SetEnvironmentVariable("HD2QD_RESOURCE_NAMES_URL", "http://127.0.0.1:" + port + "/friendlynames.txt");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("HD2QD_RESOURCE_NAMES_URL", null);
            server.Close();
            try { Directory.Delete(home, true); } catch (IOException) { }
        }

        static string Archives(int n) => "{\"Armor\": {" + string.Join(", ", Enumerable.Range(1, n).Select(i => "\"" + (0x1000 + i).ToString("x16") + "\": \"Set " + i + "\"")) + "}}";

        static string List(int n) => string.Join("\n", Enumerable.Range(1, n).Select(i => (1000 + i) + " content/thing_" + i)) + "\n";

        [Fact]
        public void The_list_is_put_where_the_manager_reads_it()
        {
            body = List(150);
            Assert.Null(Names.Here(home));
            Assert.Equal(150, Names.Fetch(home).GetAwaiter().GetResult());
            Assert.Equal(150, Names.Here(home).Value.Count);
            Assert.Equal(Path.Combine(home, "resource_names", "friendlynames.txt"), Names.File(home));
            Assert.StartsWith("1001 content/thing_1", File.ReadAllText(Names.File(home)));
            Assert.Equal(150, Names.ArchivesHere(home));
        }

        /// <summary>The archives' names come with the list; a download of them that is not that
        /// file refuses the whole fetch, and what is here stays.</summary>
        [Fact]
        public void The_archive_names_come_with_it_and_are_checked()
        {
            body = List(150);
            archives = "<html>not found</html>";
            Assert.Throws<InvalidDataException>(() => Names.Fetch(home).GetAwaiter().GetResult());
            Assert.Null(Names.Here(home));
            archives = Archives(150);
            Names.Fetch(home).GetAwaiter().GetResult();
            Assert.Equal(150, Names.ArchivesHere(home));
        }

        [Fact]
        public void What_is_not_the_list_is_refused_and_the_one_here_stays()
        {
            body = List(150);
            Names.Fetch(home).GetAwaiter().GetResult();
            body = "<html>rate limited</html>\n" + List(20);
            Assert.Throws<InvalidDataException>(() => Names.Fetch(home).GetAwaiter().GetResult());
            Assert.Equal(150, Names.Here(home).Value.Count);
        }
    }
}
