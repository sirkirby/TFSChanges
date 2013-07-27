namespace TFSChanges.Models
{
	public class ProjectConfig
	{
		public string Name { get; set; }
		public int HipChatRoomId { get; set; }
		public bool HipChatNotify { get; set; }
		public string HipChatChangesetColor { get; set; }
		public string HipChatBuildColor { get; set; }
		public string HipChatBuildFailedColor { get; set; }
		public bool IsActive { get; set; }
	}
}
