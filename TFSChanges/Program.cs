using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;

namespace TFSChanges
{
	/// <summary>
	/// Class Program
	/// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			// create a map for preferences
			Mapper.CreateMap<Preferences, Preferences>();

			// use reflection to find available providers
			var assembly = Assembly.GetCallingAssembly();
			var providers = assembly.GetTypes().Where(t => t.CustomAttributes.FirstOrDefault(c => c.AttributeType == typeof(ProviderAttribute)) != null).ToList();

			var instances = new List<Task<bool>>();
			// execute all providers
			providers.ForEach(p =>
			{
				var provider = (ProviderBase)Activator.CreateInstance(p);
				provider.Complete += (sender, eventArgs) => Trace.WriteLine(string.Format("{0} provider execution completed on {1}", ((ProviderAttribute)p.GetCustomAttribute(typeof(ProviderAttribute))).Name, DateTime.UtcNow.ToString()));
				instances.Add(provider.ExecuteAsync());
			});

			var allProviders = Task.WhenAll(instances);

			// keep the process running while providers execute
			while (!allProviders.IsCompleted)
				Thread.Sleep(500);

			Trace.WriteLine("All Done");

#if (DEBUG)
			{
				Console.ReadLine();
			}
#endif
		}
	}
}
