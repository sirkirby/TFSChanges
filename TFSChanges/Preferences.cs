using System;
using System.Reflection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TFSChanges.Properties;

namespace TFSChanges
{
	public class Preferences : TableEntity
	{
		private CloudTable _table;

		public DateTime LastChecked { get; set; }
		public string Projects { get; set; }

		/// <summary>
		/// Init the preferences
		/// </summary>
		public Preferences()
		{
			LastChecked = DateTime.UtcNow.AddHours(-24);
			Projects = "[]";
		}

		public void Load()
		{
#if DEBUG
			PartitionKey = "TFSChanges_Test";
#else
			PartitionKey = "TFSChanges";
#endif
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			RowKey = version.ToString();

			// Retrieve storage account from connection string
			var storageAccount = CloudStorageAccount.Parse(Settings.Default.RTDevStorage);
			// Create the table client
			var tableClient = storageAccount.CreateCloudTableClient();
			//Create the CloudTable that represents the "people" table.
			_table = tableClient.GetTableReference(Settings.Default.PreferencesTable);

			var operation = TableOperation.Retrieve<Preferences>(PartitionKey, RowKey);
			var results = _table.Execute(operation);

			if (results != null)
			{
				LastChecked = ((Preferences) results.Result).LastChecked.AddSeconds(-5);
				Projects = ((Preferences)results.Result).Projects;
			}
			else
			{
				LastChecked = DateTime.UtcNow.AddHours(-24);
				Projects = "[]";
				Save();
			}
		}

		/// <summary>
		/// save the preferences
		/// </summary>
		public void Save()
		{
			var operation = TableOperation.InsertOrMerge(this);
			_table.Execute(operation);
		}
	}
}
