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
                    var bytes = Encoding.UTF8.GetBytes(body ?? "");
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
