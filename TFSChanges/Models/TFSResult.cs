namespace TFSChanges.Models
{
	public enum TFSType
	{
		Changeset,
		Build
	}
	
	public class TFSResult
	{
		public string FormattedMessage { get; set; }
		public string From { get; set; }
		public bool Success { get; set; }
		public TFSType TFSType { get; set; }

		public TFSResult()
		{
			Success = true;
		}
	}
}
