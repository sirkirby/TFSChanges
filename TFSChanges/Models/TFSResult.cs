namespace TFSChanges.Models
{
	public enum TfsType
	{
		/// <summary>
		/// The changeset
		/// </summary>
		Changeset,
		/// <summary>
		/// The build
		/// </summary>
		Build
	}
	
	public class TfsResult
	{
		public string FormattedMessage { get; set; }
		public string From { get; set; }
		public bool Success { get; set; }
		public TfsType TFSType { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TfsResult"/> class.
		/// </summary>
		public TfsResult()
		{
			Success = true;
		}
	}
}
