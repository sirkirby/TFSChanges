using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Args;
using AutoMapper;
using TFSChanges.Models;

namespace TFSChanges
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				// create maps for object compare operations
				Mapper.CreateMap<Preferences, Preferences>();

				// get the args
				var arguments = Configuration.Configure<Arguments>().CreateAndBind(args);

				// check for validity and attempt to load local config defaults
				if (!arguments.IsValid)
					arguments.Defaults();

				// gotta have the required args from this point on
				if (string.IsNullOrEmpty(arguments.StorageAccount))
					throw new ArgumentNullException(arguments.StorageKey, "You must provide the Azure storage account name");
				if (string.IsNullOrEmpty(arguments.StorageKey))
					throw new ArgumentNullException(arguments.StorageKey, "You must provide the Azure storage key");

				// use reflection to find available providers
				var assembly = Assembly.GetAssembly(typeof(ProviderBase));
				var providers = assembly.GetTypes().Where(t => t.CustomAttributes.FirstOrDefault(c => c.AttributeType == typeof(ProviderAttribute)) != null).ToList();

				// load the preferences
				var prefs = Preferences.LoadAsync(arguments).Result;

				// check to make sure we have the settings we need
				if (prefs.Projects.Length <= 2)
					throw new ArgumentNullException(prefs.Projects, "You must provide a list of Projects's to poll");
				if (string.IsNullOrEmpty(prefs.TfsUser) || string.IsNullOrEmpty(prefs.TfsPassword))
					throw new ArgumentNullException(prefs.TfsUser, "You must provider your alternate tfs credentials. TfsUser and TfsPassword");

				var instances = new List<Task<bool>>();
				// execute all providers
				providers.ForEach(p =>
				{
					var provider = (ProviderBase)Activator.CreateInstance(p, new object[] {prefs});
					provider.Complete += (sender, eventArgs) => Trace.WriteLine(string.Format("{0} provider execution completed on {1}", ((ProviderAttribute)p.GetCustomAttribute(typeof(ProviderAttribute))).Name, DateTime.UtcNow.ToString()));
					instances.Add(provider.ExecuteAsync());
				});

				var allProviders = Task.WhenAll(instances);

				// keep the process running while providers execute
				while (!allProviders.IsCompleted)
					Thread.Sleep(500);

				// save the preferences
				prefs.LastChecked = prefs.Debug ? prefs.LastChecked : DateTime.UtcNow;
				Trace.WriteLine(Preferences.SaveAsync(prefs).Result ? "Settings saved" : "Settings save failed!");

				Trace.WriteLine("All Done");

				if (arguments.Debug)
					Console.ReadLine();
			}
			catch (Exception e)
			{
				Trace.TraceError(e.Message + e.StackTrace, e);
				throw;
			}
		}
	}
}
