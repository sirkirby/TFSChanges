using System;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TFSChanges.Properties;

namespace TFSChanges
{
	public class Preferences : TableEntity
	{
		public DateTime LastChecked { get; set; }
		public string TfsUser { get; set; }
		public string TfsPassword { get; set; }
		public string TfsApiUri { get; set; }
		public string TfsUri { get; set; }
		public string HipChatAuthToken { get; set; }
		public string AlmTaskUri { get; set; }
		public string AlmTaskExpression { get; set; }
		public string Projects { get; set; }

		/// <summary>
		/// Init the preferences
		/// </summary>
		public Preferences()
		{
			LastChecked = DateTime.UtcNow.AddHours(-24);
			Projects = "[]";
		}

		/// <summary>
		/// Loads this instance.
		/// </summary>
		/// <returns>Task{System.Boolean}.</returns>
		public static async Task<Preferences> LoadAsync()
		{
			var prefs = new Preferences();
#if (DEBUG)
			{
				prefs.PartitionKey = "TFSChanges_Test";
			}
#else
			{
				prefs.PartitionKey = "TFSChanges";
			}
#endif
			// pull the settings for this app version
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			prefs.RowKey = version.ToString();

			// Retrieve storage account from connection string
			var storageAccount = CloudStorageAccount.Parse(Settings.Default.StorageConnectionString);
			// Create the table client
			var tableClient = storageAccount.CreateCloudTableClient();
			//Create the CloudTable that represents the "people" table.
			var table = tableClient.GetTableReference(Settings.Default.PreferencesTable);

			var operation = TableOperation.Retrieve<Preferences>(prefs.PartitionKey, prefs.RowKey);
			var results = await table.ExecuteAsync(operation);

			if (results != null)
			{
				prefs = Mapper.Map<Preferences>(results.Result);
				return prefs;
			}
			else
			{
				await SaveAsync(prefs);
				return prefs;
			}
		}

		/// <summary>
		/// save the preferences
		/// </summary>
		public static async Task<bool> SaveAsync(Preferences prefs)
		{
			// Retrieve storage account from connection string
			var storageAccount = CloudStorageAccount.Parse(Settings.Default.StorageConnectionString);
			// Create the table client
			var tableClient = storageAccount.CreateCloudTableClient();
			//Create the CloudTable that represents the "people" table.
			var table = tableClient.GetTableReference(Settings.Default.PreferencesTable);
			// save operation
			var operation = TableOperation.InsertOrMerge(prefs);
			var response = await table.ExecuteAsync(operation);
			// return success | failure
			return response.HttpStatusCode == 200;
		}
	}
}
