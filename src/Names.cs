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
            string text, archives;
            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd("HD2QD.ResourceNames");
                text = await http.GetStringAsync(Url).ConfigureAwait(false);
                archives = await http.GetStringAsync(ArchivesUrl).ConfigureAwait(false);
            }
            int count = text.Split('\n').Select(l => l.TrimEnd('\r')).Count(IsName);
            if (count < Fewest) throw new InvalidDataException("not the name list: " + count + " names");
            int archived = Archives(archives);
            if (archived < Fewest) throw new InvalidDataException("not the archive names: " + archived + " archives");
            Directory.CreateDirectory(Folder(home));
            Put(File(home), text);
            Put(ArchivesFile(home), archives);
            System.IO.File.WriteAllText(About(home), new JavaScriptSerializer().Serialize(new Dictionary<string, object>
            {
                ["url"] = Url, ["archives_url"] = ArchivesUrl, ["repository"] = Repository,
                ["fetched"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), ["names"] = count, ["archives"] = archived,
            }));
            return count;
        }

        /// <summary>A file put in place whole: a temporary file first.</summary>
        static void Put(string path, string text)
        {
            var tmp = path + ".tmp";
            System.IO.File.WriteAllText(tmp, text, new System.Text.UTF8Encoding(false));
            if (System.IO.File.Exists(path)) System.IO.File.Replace(tmp, path, null);
            else System.IO.File.Move(tmp, path);
        }

        /// <summary>HD2SDK's names of the game's archives (hashlists\archivehashes.json: category,
        /// then archive hash, then name: "Armor", "082f22454170efd6", "SR-24 Street Scout"), beside
        /// the name list. A model a mod replaces is named by the game archive that holds it when
        /// the name list has no name for it (the user, 2026-11-03).</summary>
        public static string ArchivesUrl => Url.Substring(0, Url.LastIndexOf('/') + 1) + "archivehashes.json";

        /// <summary>Where the archives' names go.</summary>
        public static string ArchivesFile(string home) => Path.Combine(Folder(home), "archivehashes.json");

        /// <summary>How many archives a file of archivehashes.json's shape names; 0 when it is not one.</summary>
        static int Archives(string json)
        {
            try
            {
                if (!(new JavaScriptSerializer().DeserializeObject(json) is Dictionary<string, object> d)) return 0;
                return d.Values.OfType<Dictionary<string, object>>()
                        .Sum(c => c.Count(kv => kv.Value is string && ulong.TryParse(kv.Key, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)));
            }
            catch (ArgumentException) { return 0; }
            catch (InvalidOperationException) { return 0; }
        }

        /// <summary>How many archives are named here (0: none fetched).</summary>
        public static int ArchivesHere(string home)
        {
            try { return System.IO.File.Exists(ArchivesFile(home)) ? Archives(System.IO.File.ReadAllText(ArchivesFile(home))) : 0; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { return 0; }
        }
    }
}
