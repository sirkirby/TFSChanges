using System;

namespace TFSChanges
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class ProviderAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProviderAttribute"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public ProviderAttribute(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProviderAttribute"/> class.
		/// </summary>
		public ProviderAttribute() : this ("Provider") {}
	}
}
