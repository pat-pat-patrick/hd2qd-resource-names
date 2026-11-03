using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Hd2Qd.ResourceNames
{
    /// <summary>HD2SDK's resource name list (hashlists\friendlynames.txt: lines "decimal-id name"),
    /// fetched from HD2SDK's own repository into the manager's settings folder, where the manager
    /// reads it. The list has no licence: nothing of it ships with this plugin, the player fetches
    /// it from its source.</summary>
    public static class Names
    {
        /// <summary>HD2SDK Community Edition, the add-on the modding community keeps up.</summary>
        public const string Repository = "https://github.com/Boxofbiscuits97/HD2SDK-CommunityEdition";

        /// <summary>The list in that repository; HD2QD_RESOURCE_NAMES_URL names another (tests).</summary>
        public static string Url =>
            Environment.GetEnvironmentVariable("HD2QD_RESOURCE_NAMES_URL") is string u && u.Length > 0 ? u
            : "https://raw.githubusercontent.com/Boxofbiscuits97/HD2SDK-CommunityEdition/main/hashlists/friendlynames.txt";

        /// <summary>Fewer lines than this is not the list (a page of an error, a list cut short).</summary>
        public const int Fewest = 100;

        public static string Folder(string home) => Path.Combine(home, "resource_names");

        /// <summary>Where the manager reads it (ResourceNames.Downloaded in the manager).</summary>
        public static string File(string home) => Path.Combine(Folder(home), "friendlynames.txt");

        static string About(string home) => Path.Combine(Folder(home), "source.json");

        /// <summary>What is here now: how many names, when it was fetched (local time), from where;
        /// null when nothing was fetched.</summary>
        public static (int Count, DateTime When, string From)? Here(string home)
        {
            var f = File(home);
            if (!System.IO.File.Exists(f)) return null;
            int count = 0;
            try { count = System.IO.File.ReadLines(f).Count(IsName); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { return null; }
            var when = System.IO.File.GetLastWriteTime(f);
            string from = null;
            try
            {
                if (System.IO.File.Exists(About(home)) &&
                    new JavaScriptSerializer().DeserializeObject(System.IO.File.ReadAllText(About(home))) is Dictionary<string, object> d)
                    from = d.TryGetValue("url", out var u) ? u as string : null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException) { }
            return (count, when, from);
        }

        static bool IsName(string line)
        {
            int sp = line.IndexOf(' ');
            return sp > 0 && line.Length > sp + 1 && ulong.TryParse(line.Substring(0, sp), NumberStyles.None, CultureInfo.InvariantCulture, out _);
        }

        /// <summary>Fetches the list and puts it in place whole (a temporary file first). Returns how
        /// many names it has; refused (InvalidDataException) when it is not the list, and the one
        /// already here stays.</summary>
        public static async Task<int> Fetch(string home)
        {
            string text;
            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd("HD2QD.ResourceNames");
                text = await http.GetStringAsync(Url).ConfigureAwait(false);
            }
            int count = text.Split('\n').Select(l => l.TrimEnd('\r')).Count(IsName);
            if (count < Fewest) throw new InvalidDataException("not the name list: " + count + " names");
            Directory.CreateDirectory(Folder(home));
            var tmp = File(home) + ".tmp";
            System.IO.File.WriteAllText(tmp, text, new System.Text.UTF8Encoding(false));
            if (System.IO.File.Exists(File(home))) System.IO.File.Replace(tmp, File(home), null);
            else System.IO.File.Move(tmp, File(home));
            System.IO.File.WriteAllText(About(home), new JavaScriptSerializer().Serialize(new Dictionary<string, object>
            {
                ["url"] = Url, ["repository"] = Repository, ["fetched"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), ["names"] = count,
            }));
            return count;
        }
    }
}
