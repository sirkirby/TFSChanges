using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TFSChanges.Properties;
using TFSChanges.TFSProxy;

namespace TFSChanges
{
	class Program
	{
		static void Main(string[] args)
		{
			var uri = new Uri(Settings.Default.TFSUri);

			var context = new TFSData(uri);
			var creds = new NetworkCredential(Settings.Default.TFSUser, Settings.Default.TFSPassword);
			var cache = new CredentialCache {{uri, "Basic", creds}};
			context.Credentials = cache;

			var client = new HttpClient();

			var prefs = new Preferences();

			foreach (var change in GetChangesets(context, prefs.LastChecked))
				Console.WriteLine(PostToHipChat(client, "TFS Changeset", change));

			foreach (var build in GetBuilds(context, prefs.LastChecked))
				Console.WriteLine(PostToHipChat(client, "TFS Build", build));

			prefs.LastChecked = DateTime.UtcNow;
			prefs.Save();
		}

		private static string PostToHipChat(HttpClient client, string from, string message)
		{
			var hipchat = new Uri("http://api.hipchat.com/v1/rooms/message?format=json&auth_token=" + Settings.Default.HipChatAuthToken);
			var content = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("room_id", Settings.Default.HipChatRoom), new KeyValuePair<string, string>("from", @from), new KeyValuePair<string, string>("message", message) };
			var formContent = new FormUrlEncodedContent(content);
			// send the request
			var result = client.PostAsync(hipchat, formContent).Result;
			return result.Content.ReadAsStringAsync().Result;
		}

		private static IEnumerable<string> GetChangesets(TFSData context, DateTime lastChecked)
		{
			var changesets = context.Changesets.Where(c => c.CreationDate > lastChecked);

			var builder = new StringBuilder();
			var writer = new StringWriter(builder);

			foreach (var changeset in changesets)
			{
				writer.WriteLine(string.Format("<a href='{0}'>Changeset {1}</a>", changeset.WebEditorUrl, changeset.Id));
				writer.WriteLine(changeset.Comment);
				writer.WriteLine(string.Format("<strong>change by {0} on {1}</strong>", changeset.Committer, changeset.CreationDate));
				writer.Flush();
				yield return builder.ToString();
				builder.Clear();
			}
		}

		private static IEnumerable<string> GetBuilds(TFSData context, DateTime lastChecked)
		{
			var builds = context.Builds.Where(b => b.BuildFinished && b.FinishTime > lastChecked);

			var builder = new StringBuilder();
			var writer = new StringWriter(builder);
			
			foreach (var build in builds)
			{
				writer.WriteLine(string.Format("<strong>{0}</strong> by {1} for {2} completed {3}", build.Definition, build.RequestedBy, build.RequestedFor, build.FinishTime));
				writer.WriteLine(string.Format("<a href='https://ryantech.visualstudio.com/DefaultCollection/{0}/_build'>{1}</a>", build.Project, build.Status));
				if (build.Status == "Failed")
					writer.WriteLine(build.Errors);
				writer.Flush();
				yield return builder.ToString();
				builder.Clear();
			}
		}
	}
}
