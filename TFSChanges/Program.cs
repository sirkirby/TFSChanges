using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TFSChanges.Models;
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

			foreach (var project in JsonConvert.DeserializeObject<List<ProjectConfig>>(prefs.Projects))
			{
				// check for new changesets since the last run
				foreach (var change in GetChangesets(context, prefs.LastChecked, project))
					Console.WriteLine(PostToHipChat(client, change, project));

				// change for new builds since the last run
				foreach (var build in GetBuilds(context, prefs.LastChecked, project))
					Console.WriteLine(PostToHipChat(client, build, project));
			}
			// save the preferences
#if DEBUG
			prefs.LastChecked = prefs.LastChecked;
#else
			prefs.LastChecked = DateTime.UtcNow;
#endif
			prefs.Save();
		}

		/// <summary>
		/// Post message update to hipchat room
		/// </summary>
		/// <param name="client">The client.</param>
		/// <param name="tfsResult">The TFS result.</param>
		/// <param name="project">The project.</param>
		/// <returns>System.String.</returns>
		private static string PostToHipChat(HttpClient client, TFSResult tfsResult, ProjectConfig project)
		{
			var hipchat = new Uri("http://api.hipchat.com/v1/rooms/message?format=json&auth_token=" + Settings.Default.HipChatAuthToken);
			var content = new List<KeyValuePair<string, string>>
			              {
				              new KeyValuePair<string, string>("room_id", project.HipChatRoomId.ToString()),
				              new KeyValuePair<string, string>("from", tfsResult.From),
				              new KeyValuePair<string, string>("message", tfsResult.FormattedMessage),
				              new KeyValuePair<string, string>("notify", project.HipChatNotify ? "1" : "0")
			              };
			// customize the room message based on type of tfs message
			switch (tfsResult.TFSType)
			{
				case TFSType.Build:
					content.Add(new KeyValuePair<string, string>("color", tfsResult.Success ? !string.IsNullOrEmpty(project.HipChatBuildColor) ? project.HipChatBuildColor : "green" : !string.IsNullOrEmpty(project.HipChatBuildFailedColor) ? project.HipChatBuildFailedColor : "red"));
					break;
				case TFSType.Changeset:
					content.Add(new KeyValuePair<string, string>("color", !string.IsNullOrEmpty(project.HipChatChangesetColor) ? project.HipChatChangesetColor : "purple"));
					break;
			}
			var formContent = new FormUrlEncodedContent(content);
			// send the request
			var result = client.PostAsync(hipchat, formContent).Result;
			return result.Content.ReadAsStringAsync().Result;
		}

		/// <summary>
		/// Get latest changesets from TFS
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="lastChecked">The last checked.</param>
		/// <param name="project">The project.</param>
		/// <returns>collection of formatted strings with select change information</returns>
		private static IEnumerable<TFSResult> GetChangesets(TFSData context, DateTime lastChecked, ProjectConfig project)
		{
			var query = context.CreateQuery<Changeset>("Projects('" + project.Name + "')/Changesets");
			var changesets = query.Execute();

			var builder = new StringBuilder();
			var writer = new StringWriter(builder);
			var issueRegex = new Regex(@"[A-Z]+\-[0-9]+");

			// only grab the most recent changesets for the given project
			foreach (var changeset in changesets.Where(c => c.CreationDate > lastChecked))
			{
				var result = new TFSResult { From = "TFS Changeset", TFSType = TFSType.Changeset };
				var comment = issueRegex.Replace(changeset.Comment, (m) => string.Format(@"<a href=""http://issues.ryantechinc.com:81/issue/{0}"">{0}</a>", m.Captures[0].Value));
				writer.WriteLine(string.Format("<a href='{0}'>Changeset {1}</a> ({2})", changeset.WebEditorUrl, changeset.Id, project.Name));
				writer.WriteLine(comment);
				writer.WriteLine(string.Format("<br/><strong>change by {0} on {1}</strong>", changeset.Committer, changeset.CreationDate));
				writer.Flush();
				result.FormattedMessage = builder.ToString();
				yield return result;
				builder.Clear();
			}

		}

		/// <summary>
		/// Get latest builds from TFS
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="lastChecked">The last checked.</param>
		/// <param name="project">The project.</param>
		/// <returns>collection of formatted strings with select build information</returns>
		private static IEnumerable<TFSResult> GetBuilds(TFSData context, DateTime lastChecked, ProjectConfig project)
		{
			var builds = context.Builds.Where(b => b.BuildFinished && b.FinishTime > lastChecked && b.Project == project.Name);

			var builder = new StringBuilder();
			var writer = new StringWriter(builder);
			
			foreach (var build in builds)
			{
				var result = new TFSResult {From = "TFS Build", TFSType = TFSType.Build};
				var elapsed = (build.FinishTime - build.StartTime);
				writer.WriteLine(string.Format("<strong>{0}</strong> ({4}) by {1} for <i>{2}</i> completed {3}", build.Definition, build.RequestedBy, build.RequestedFor, build.FinishTime, project.Name));
				writer.WriteLine(string.Format("<br/><a href='https://ryantech.visualstudio.com/DefaultCollection/{0}/_build'>{1}</a> Total build time: {2} minutes {3} seconds", build.Project, build.Status, elapsed.Minutes, elapsed.Seconds));
				if (build.Status == "Failed")
				{
					
					writer.WriteLine("<br/>" + build.Errors.Substring(0, build.Errors.Length > 200 ? 200 : build.Errors.Length) + "...");
					result.Success = false;
				}
				writer.Flush();
				result.FormattedMessage = builder.ToString();
				yield return result;
				builder.Clear();
			}
		}
	}
}
