﻿using System.IO;
using Waher.Networking.DNS.Enumerations;

namespace Waher.Networking.DNS.ResourceRecords
{
	/// <summary>
	/// Canonical NAME
	/// </summary>
	public class CNAME : ResourceNameRecord
	{
		/// <summary>
		/// Canonical NAME
		/// </summary>
		public CNAME()
			: base()
		{
		}

		/// <summary>
		/// Canonical NAME
		/// </summary>
		/// <param name="Name">Name</param>
		/// <param name="Type">Resource Record Type</param>
		/// <param name="Class">Resource Record Class</param>
		/// <param name="Ttl">Time to live</param>
		/// <param name="Data">RR-specific binary data.</param>
		public CNAME(string Name, TYPE Type, CLASS Class, uint Ttl, Stream Data)
			: base(Name, Type, Class, Ttl, Data)
		{
		}
	}
}
