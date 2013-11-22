using System.IO;
using System.Reflection;
using Args;
using Newtonsoft.Json;

namespace TFSChanges.Models
{
	public class Arguments
	{
		[ArgsMemberSwitch("sa", "account", "storageAccount")]
		public string StorageAccount { get; set; }
		[ArgsMemberSwitch("sk", "key", "storageKey")]
		public string StorageKey { get; set; }
		[ArgsMemberSwitch("d", "test", "debug")]
		public bool Debug { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance is valid.
		/// </summary>
		/// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
		public bool IsValid
		{
			get { return !string.IsNullOrEmpty(StorageAccount) && !string.IsNullOrEmpty(StorageKey); }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is valid.
		/// </summary>
		/// <value>The provider expects both values and will not function without them</value>
		public void Defaults()
		{
			var localConfig = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\arguments.json";
			if (File.Exists(localConfig))
			{
				var json = File.ReadAllText(localConfig);
				var args = JsonConvert.DeserializeObject<Arguments>(json);
				StorageAccount = args.StorageAccount;
				StorageKey = args.StorageKey;
				Debug = args.Debug;
			}
		}
	}
}
