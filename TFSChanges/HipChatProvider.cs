using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using TFSChanges.Models;

namespace TFSChanges
{
	[Provider("HipChat")]
	public class HipChatProvider : ProviderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProviderBase" /> class.
		/// </summary>
		/// <param name="prefs">The prefs.</param>
		public HipChatProvider(Preferences prefs) : base(prefs) {}

		/// <summary>
		/// post data as an asynchronous operation.
		/// </summary>
		/// <param name="tfsResult">The TFS result.</param>
		/// <param name="project">The project.</param>
		/// <returns>Task{System.Boolean}.</returns>
		protected override async Task<string> PostAsync(TfsResult tfsResult, TfsProject project)
		{
			var client = new HttpClient();
			var hipchat = new Uri("http://api.hipchat.com/v1/rooms/message?format=json&auth_token=" + Prefs.HipChatAuthToken);
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
				case TfsType.Build:
					content.Add(new KeyValuePair<string, string>("color", tfsResult.Success ? !string.IsNullOrEmpty(project.HipChatBuildColor) ? project.HipChatBuildColor : "green" : !string.IsNullOrEmpty(project.HipChatBuildFailedColor) ? project.HipChatBuildFailedColor : "red"));
					break;
				case TfsType.Changeset:
					content.Add(new KeyValuePair<string, string>("color", !string.IsNullOrEmpty(project.HipChatChangesetColor) ? project.HipChatChangesetColor : "purple"));
					break;
			}
			var formContent = new FormUrlEncodedContent(content);
			// send the request
			var result = await client.PostAsync(hipchat, formContent);
			var responseBody = await result.Content.ReadAsStringAsync();
			Trace.WriteLine(responseBody);
			return responseBody;
		}
	}
}
