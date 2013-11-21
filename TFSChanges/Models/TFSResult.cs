namespace TFSChanges.Models
{
	public enum TfsType
	{
		Changeset,
		Build
	}
	
	public class TfsResult
	{
		public string FormattedMessage { get; set; }
		public string From { get; set; }
		public bool Success { get; set; }
		public TfsType TFSType { get; set; }

		public TfsResult()
		{
			Success = true;
		}
	}
}
