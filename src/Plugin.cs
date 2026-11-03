using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hd2Qd.ResourceNames
{
    /// <summary>Resource names, a plugin of HD2 Mod Quick Deploy: one button that fetches HD2SDK's
    /// resource name list from its own repository, so the conflicts name a resource ("Helmet O-3
    /// Free Spirit") instead of giving its number. The manager reads the list from its settings
    /// folder; this plugin only fetches it.</summary>
    public static class Plugin
    {
        public const int Api = 2;
        public const string Kind = "panel";

        public static string Title(string lang) => Words.Get(lang, "title");

        public static string Summary(string lang) => Words.Get(lang, "summary");

        static Brush Brush(string key) => Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;

        /// <summary>The card's body: where the list comes from, what is here, and the button.</summary>
        public static FrameworkElement Panel(Window owner, Dictionary<string, object> host)
        {
            var lang = Words.Lang(host);
            var home = (string)host["home"];
            var box = new StackPanel();
            box.Children.Add(new TextBlock { Text = Words.Get(lang, "source"), TextWrapping = TextWrapping.Wrap, Foreground = Brush("Muted"), FontSize = 12 });
            var repo = new TextBlock { Margin = new Thickness(0, 2, 0, 0), FontSize = 12 };
            var link = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(Names.Repository)) { Foreground = Brush("Blue") };
            link.Click += (s, e) => { try { Process.Start(new ProcessStartInfo(Names.Repository) { UseShellExecute = true }); } catch (Exception) { } };
            repo.Inlines.Add(link);
            box.Children.Add(repo);

            var state = new TextBlock { Margin = new Thickness(0, 12, 0, 0), TextWrapping = TextWrapping.Wrap };
            // a list fetched by 1.0.0 has no game archive names: said, rather than "0 game archive
            // names" (the user, 2026-11-03), and the button fetches them
            void ShowHere()
            {
                var here = Names.Here(home);
                int archives = Names.ArchivesHere(home);
                state.Text = here == null ? Words.Get(lang, "none")
                    : archives == 0 ? Words.F(lang, "have_names_only", here.Value.Count.ToString("N0"), here.Value.When.ToString("yyyy-MM-dd HH:mm"))
                    : Words.F(lang, "have", here.Value.Count.ToString("N0"), here.Value.When.ToString("yyyy-MM-dd HH:mm"), archives.ToString("N0"));
                state.Foreground = Brush(here == null ? "Muted" : archives == 0 ? "Yellow" : "Ink");
            }
            string ButtonWords() => Words.Get(lang, Names.Here(home) == null ? "fetch" : Names.ArchivesHere(home) == 0 ? "fetch_archives" : "fetch_again");
            ShowHere();
            box.Children.Add(state);

            var fetch = new Button
            {
                Content = ButtonWords(),
                Style = Application.Current?.TryFindResource("Primary") as Style, MinHeight = 44, Padding = new Thickness(16, 0, 16, 0),
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0),
            };
            fetch.Click += async (s, e) =>
            {
                fetch.IsEnabled = false;
                fetch.Content = Words.Get(lang, "fetching");
                try
                {
                    int n = await Names.Fetch(home);
                    (host.TryGetValue("foot", out var f) ? f as Action<string, string> : null)?.Invoke(Words.F(lang, "fetched", n.ToString("N0")), "Blue");
                    // the manager reads its state again (the conflicts get their names) and draws this page anew
                    (host.TryGetValue("refresh", out var r) ? r as Action : null)?.Invoke();
                }
                catch (Exception x) when (x is System.Net.Http.HttpRequestException || x is System.Threading.Tasks.TaskCanceledException ||
                                          x is System.IO.IOException || x is UnauthorizedAccessException || x is System.IO.InvalidDataException)
                {
                    (host.TryGetValue("tell", out var t) ? t as Action<string> : null)?.Invoke(Words.F(lang, "failed", x.Message));
                }
                ShowHere();
                fetch.IsEnabled = true;
                fetch.Content = ButtonWords();
            };
            box.Children.Add(fetch);
            return box;
        }

        /// <summary>The command line: resource-names-fetch.</summary>
        public static readonly string[][] Commands =
        {
            new[] { "resource-names-fetch", "fetch HD2SDK's resource name list from its repository, so the conflicts name resources" },
        };

        public static int Run(string command, string[] args, System.IO.TextWriter output, Dictionary<string, object> host)
        {
            int n = Names.Fetch((string)host["home"]).GetAwaiter().GetResult();
            output.WriteLine(n + " names from " + Names.Url);
            return 0;
        }
    }
}
