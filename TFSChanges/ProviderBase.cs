using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TFSChanges.Models;
using TFSChanges.TFSProxy;

namespace TFSChanges
{
	public abstract class ProviderBase
	{
		/// <summary>
		/// Gets the prefs.
		/// </summary>
		/// <value>The prefs.</value>
		protected Preferences Prefs { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProviderBase"/> class.
		/// </summary>
		/// <param name="prefs">The prefs.</param>
		protected ProviderBase(Preferences prefs)
		{
			Prefs = prefs;
		}

		/// <summary>
		/// Occurs when [complete].
		/// </summary>
		public event EventHandler<EventArgs> Complete;

		/// <summary>
		/// Called when [complete].
		/// </summary>
		protected virtual void OnComplete()
		{
			var handler = Complete;
			if (handler != null) handler(this, EventArgs.Empty);
		}

		/// <summary>
		/// Occurs when [found work].
		/// </summary>
		public event EventHandler<EventArgs> FoundWork;

		/// <summary>
		/// Called when [found work].
		/// </summary>
		protected virtual void OnFoundWork()
		{
			var handler = FoundWork;
			if (handler != null) handler(this, EventArgs.Empty);
		}

		/// <summary>
		/// post as an asynchronous operation.
		/// </summary>
		/// <param name="args">The arguments.</param>
		/// <returns>Task{System.Boolean}.</returns>
		public async Task<bool> ExecuteAsync()
		{
			try
			{
				// initialize the service reference context
				var uri = new Uri(Prefs.TfsApiUri);
				var context = new TFSData(uri);
				// set the basic auth credentials
				var creds = new NetworkCredential(Prefs.TfsUser, Prefs.TfsPassword);
				var cache = new CredentialCache { { uri, "Basic", creds } };
				context.Credentials = cache;
				// check each configured project
				var doItDoug = new List<Task<string>>();
				foreach (var project in JsonConvert.DeserializeObject<List<TfsProject>>(Prefs.Projects))
				{
					// check for new changesets since the last run
					doItDoug.AddRange(GetChangesets(context, Prefs.LastChecked, project).Select(change => PostAsync(change, project)));
					// change for new builds since the last run
					doItDoug.AddRange(GetBuilds(context, Prefs.LastChecked, project).Select(build => PostAsync(build, project)));
				}

				// tell the invoker that work was found
				if (doItDoug.Any())
					OnFoundWork();

				// wait for all the requests to complete
				await Task.WhenAll(doItDoug);

				OnComplete();
				return true;
			}
			catch (Exception e)
			{
				Trace.TraceError(e.Message, Prefs);
				throw;
			}
		}

		/// <summary>
		/// Posts the data asynchronous.
		/// </summary>
		/// <param name="tfsResult">The TFS result.</param>
		/// <param name="project">The project.</param>
		/// <returns>Task{System.Boolean}.</returns>
		protected abstract Task<string> PostAsync(TfsResult tfsResult, TfsProject project);

		/// <summary>
		/// Get latest changesets from TFS
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="lastChecked">The last checked.</param>
		/// <param name="project">The project.</param>
		/// <returns>collection of formatted strings with select change information</returns>
		protected IEnumerable<TfsResult> GetChangesets(TFSData context, DateTime lastChecked, TfsProject project)
		{
			var query = context.CreateQuery<Changeset>("Projects('" + project.Name + "')/Changesets");
			var changesets = query.Execute();

			var builder = new StringBuilder();
			var writer = new StringWriter(builder);
			var issueRegex = new Regex(Prefs.AlmTaskExpression);

			// only grab the most recent changesets for the given project
			var results = changesets.Where(c => c.CreationDate > lastChecked).ToList();

			// some verbose logging for when we just need to know
			if (results.Any())
				Trace.WriteLineIf(Program.Ts.TraceVerbose, string.Format("New changesets found for {0} since {1}", project.Name, lastChecked));
			
			foreach (var changeset in results)
			{
				var result = new TfsResult { From = "TFS Changeset", TFSType = TfsType.Changeset };
				var comment = issueRegex.Replace(changeset.Comment, (m) => string.Format(@"<a href=""" + Prefs.AlmTaskUri + @"{0}"">{0}</a>", m.Captures[0].Value));
				writer.WriteLine(string.Format("<a href='{0}'>Changeset {1}</a> (<a href='{2}DefaultCollection/{3}'>{3}</a>)", changeset.WebEditorUrl, changeset.Id, Prefs.TfsUri, project.Name));
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
		protected IEnumerable<TfsResult> GetBuilds(TFSData context, DateTime lastChecked, TfsProject project)
		{
			var builds = context.Builds.Where(b => b.BuildFinished && b.FinishTime > lastChecked && b.Project == project.Name).ToList();

			var builder = new StringBuilder();
			var writer = new StringWriter(builder);

			// some verbose logging for when we just need to know
			if (builds.Any())
				Trace.WriteLineIf(Program.Ts.TraceVerbose, string.Format("New builds found for {0} since {1}", project.Name, lastChecked));

			foreach (var build in builds)
			{
				var result = new TfsResult { From = "TFS Build", TFSType = TfsType.Build };
				var elapsed = (build.FinishTime - build.StartTime);
				writer.WriteLine(string.Format("<strong><a href='{5}'>{0}</a></strong> ({4}) by {1} for <i>{2}</i> completed {3}", build.Definition, build.RequestedBy, build.RequestedFor, build.FinishTime, project.Name,
					Regex.IsMatch(build.Definition.ToLower(), @"stg|staging") ? project.StagingUri ?? Prefs.TfsUri : project.ProductionUri ?? Prefs.TfsUri));
				writer.WriteLine(string.Format("<br/><a href='" + Prefs.TfsUri + "DefaultCollection/{0}/_build'>{1}</a> Total build time: {2} minutes {3} seconds", build.Project, build.Status, elapsed.Minutes, elapsed.Seconds));
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
