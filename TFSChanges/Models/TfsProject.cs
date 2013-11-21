namespace TFSChanges.Models
{
	/// <summary>
	/// Class TfsProject.
	/// </summary>
	/// <remarks>JSON serialized project data</remarks>
	public class TfsProject
	{
		public string Name { get; set; }
		public string ProductionUri { get; set; }
		public string StagingUri { get; set; }
		public int HipChatRoomId { get; set; }
		public bool HipChatNotify { get; set; }
		public string HipChatChangesetColor { get; set; }
		public string HipChatBuildColor { get; set; }
		public string HipChatBuildFailedColor { get; set; }
		public bool IsActive { get; set; }
	}
}
