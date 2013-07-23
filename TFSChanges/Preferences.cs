using System;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using Newtonsoft.Json;

namespace TFSChanges
{
	[JsonObject]
	public class Preferences
	{
		public string Path
		{
			get
			{
				return System.IO.Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath);
			}
		}

		public DateTime LastChecked { get; set; }

		public Preferences()
		{
			var path = Path;
			if (File.Exists(path))
			{
				var settings = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(path));
				LastChecked = settings.LastChecked;
			}
			else
			{
				LastChecked = DateTime.UtcNow.AddHours(-12);
				Save();
			}
		}

		public void Save()
		{
			File.WriteAllText(Path, JsonConvert.SerializeObject(this));
		}
	}
}
