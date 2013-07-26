using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using TFSChanges.Properties;
using TFSChanges.TFSProxy;

namespace TFSChanges
{
	/// <summary>
	/// Class Program
	/// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			// initialize the service reference context
			var uri = new Uri(Settings.Default.TFSUri);
			var context = new TFSData(uri);
			// set the basic auth credentials
			var creds = new NetworkCredential(Settings.Default.TFSUser, Settings.Default.TFSPassword);
			var cache = new CredentialCache {{uri, "Basic", creds}};
			context.Credentials = cache;

			// get a new http client for the hipchat api calls
			var client = new HttpClient();

			// load the preferences from table storage
			var prefs = new Preferences();
			prefs.Load();

			// check for new changesets since the last run
			foreach (var change in GetChangesets(context, prefs.LastChecked))
				Console.WriteLine(PostToHipChat(client, "TFS Changeset", change));

			// change for new builds since the last run
			foreach (var build in GetBuilds(context, prefs.LastChecked))
				Console.WriteLine(PostToHipChat(client, "TFS Build", build));

			// save the preferences
			prefs.LastChecked = DateTime.UtcNow;
			prefs.Save();
		}

		/// <summary>
		/// Post message update to hipchat room
		/// </summary>
		/// <param name="client"></param>
		/// <param name="from"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private static string PostToHipChat(HttpClient client, string from, string message)
		{
			var hipchat = new Uri("http://api.hipchat.com/v1/rooms/message?format=json&auth_token=" + Settings.Default.HipChatAuthToken);
			var content = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("room_id", Settings.Default.HipChatRoom), new KeyValuePair<string, string>("from", @from), new KeyValuePair<string, string>("message", message) };
			var formContent = new FormUrlEncodedContent(content);
			// send the request
			var result = client.PostAsync(hipchat, formContent).Result;
			return result.Content.ReadAsStringAsync().Result;
		}

		/// <summary>
		/// Get latest changesets from TFS
		/// </summary>
		/// <param name="context"></param>
		/// <param name="lastChecked"></param>
		/// <returns>collection of formatted strings with select change information</returns>
		private static IEnumerable<string> GetChangesets(TFSData context, DateTime lastChecked)
		{
			var changesets = context.Changesets.Where(c => c.CreationDate > lastChecked);

			var builder = new StringBuilder();
			var writer = new StringWriter(builder);
			var issueRegex = new Regex(@"[A-Z]+\-[0-9]+");
			foreach (var changeset in changesets)
			{
				var comment = issueRegex.Replace(changeset.Comment, (m) => string.Format(@"<a href=""http://issues.ryantechinc.com:81/issue/{0}"">{0}</a>", m.Captures[0].Value));
				writer.WriteLine(string.Format("<a href='{0}'>Changeset {1}</a>", changeset.WebEditorUrl, changeset.Id));
				writer.WriteLine(comment);
				writer.WriteLine(string.Format("<strong>change by {0} on {1}</strong>", changeset.Committer, changeset.CreationDate));
				writer.Flush();
				yield return builder.ToString();
				builder.Clear();
			}
		}

		/// <summary>
		/// Get latest builds from TFS
		/// </summary>
		/// <param name="context"></param>
		/// <param name="lastChecked"></param>
		/// <returns>collection of formatted strings with select build information</returns>
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
