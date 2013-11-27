using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TFSChanges.Models;
using TFSChanges.Properties;

namespace TFSChanges
{
	public class Preferences : TableEntity
	{
		private string _tfsApiUri;

		public string TfsApiUri
		{
			get { return _tfsApiUri ?? "https://tfsodata.visualstudio.com/DefaultCollection/"; }
			set { _tfsApiUri = value; }
		}

		public DateTime LastChecked { get; set; }
		public string TfsUser { get; set; }
		public string TfsPassword { get; set; }
		public string TfsUri { get; set; }
		public string HipChatAuthToken { get; set; }
		public string AlmTaskUri { get; set; }
		public string AlmTaskExpression { get; set; }
		public string Projects { get; set; }
		[IgnoreProperty]
		public string StorageAccount { get; set; }
		[IgnoreProperty]
		public string StorageKey { get; set; }
		[IgnoreProperty]
		public bool Debug { get; set; }

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
		/// <param name="args">The arguments.</param>
		/// <returns>Task{System.Boolean}.</returns>
		public static async Task<Preferences> LoadAsync(Arguments args)
		{
			var storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", args.StorageAccount, args.StorageKey);
			
			var prefs = new Preferences {PartitionKey = args.Debug ? "TFSChanges_Test" : "TFSChanges"};
			// pull the settings for this app version
			var version = Assembly.GetAssembly(typeof(ProviderBase)).GetName().Version;
			prefs.RowKey = version.ToString();

			// Retrieve storage account from connection string
			var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
			// Create the table client
			var tableClient = storageAccount.CreateCloudTableClient();
			//Create the CloudTable that represents the "people" table.
			var table = tableClient.GetTableReference(Settings.Default.StorageTableName);

			var operation = TableOperation.Retrieve<Preferences>(prefs.PartitionKey, prefs.RowKey);
			var results = await table.ExecuteAsync(operation);

			if (results != null)
			{
				prefs = Mapper.Map<Preferences>(results.Result);
				prefs.StorageAccount = args.StorageAccount;
				prefs.StorageKey = args.StorageKey;
				prefs.Debug = args.Debug;
				return prefs;
			}

			return prefs;
		}

		/// <summary>
		/// save the preferences
		/// </summary>
		public static async Task<bool> SaveAsync(Preferences prefs)
		{
			var storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", prefs.StorageAccount, prefs.StorageKey);
			// Retrieve storage account from connection string
			var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
			// Create the table client
			var tableClient = storageAccount.CreateCloudTableClient();
			//Create the CloudTable that represents the "people" table.
			var table = tableClient.GetTableReference(Settings.Default.StorageTableName);
			// save operation
			var operation = TableOperation.InsertOrMerge(prefs);
			var response = await table.ExecuteAsync(operation);
			Trace.WriteLine(response);
			// return success | failure
			return response.HttpStatusCode < 400;
		}
	}
}
