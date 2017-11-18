// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace FirefoxSessionManagerExport
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json.Linq;

    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                return;
            }

            var file = args[0];
            var text = File.ReadAllText(file);
            var backup = JObject.Parse(text);

            var root = backup["windows"].First;

            var tabGroupsText = root["extData"]["tabview-group"].Value<string>();
            var tabGroupsData = JObject.Parse(tabGroupsText);
            var tabGroups = tabGroupsData.Children().ToDictionary(
                x => x.First["id"].Value<string>(),
                y => y.First["title"].Value<string>());

            var tabsData = root["tabs"];
            var tabs = tabsData
                .Select(t =>
                {
                    var tabText = t["extData"]?["tabview-tab"].Value<string>();
                    var groupId = tabGroups.First().Key;
                    if (!string.IsNullOrWhiteSpace(tabText) && tabText != "null")
                    {
                        var tabData = JObject.Parse(tabText);
                        groupId = tabData["groupID"].Value<string>();
                    }

                    var isPinned = t["pinned"]?.Value<bool>() ?? false;

                    var entries = t["entries"];
                    var urls = entries.Select(e => new
                    {
                        Title = e["title"].Value<string>(),
                        Url = e["url"].Value<string>()
                    });
                    return new {GroupId = groupId, IsPinned = isPinned, Urls = urls.ToList()};
                }).OrderBy(x => Convert.ToInt32(x.GroupId)).ToList();

            var tabsByGroup = tabs.GroupBy(t => t.GroupId);

            var htmlText = new StringBuilder();
            htmlText.Append("<html><body>");

            foreach (var tabGroup in tabsByGroup)
            {
                htmlText.Append($"<h1>{tabGroups[tabGroup.Key]}</h1><ul>");

                foreach (var tab in tabGroup)
                {
                    htmlText.Append("<li>");
                    if (tab.Urls.Count > 1)
                    {
                        htmlText.Append(tab.Urls.First().Title);
                        if (tab.IsPinned)
                        {
                            htmlText.Append(" (pinned)");
                        }

                        htmlText.Append("<ul>");
                        tab.Urls.ForEach(u => htmlText.Append($"<li><a href=\"{u.Url}\">{u.Title}</a></li>"));
                        htmlText.Append("</ul>");
                    }
                    else
                    {
                        tab.Urls.ForEach(u => htmlText.Append($"<a href=\"{u.Url}\">{u.Title}</a>"));
                    }

                    htmlText.Append("</li>");
                }

                htmlText.Append("</ul>");
            }

            htmlText.Append("</body></html>");

            var outputFile = file + ".html";
            File.WriteAllText(outputFile, htmlText.ToString());
        }
    }
}