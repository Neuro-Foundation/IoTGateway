﻿namespace Waher.Networking.XMPP.Contracts.Search
{
	/// <summary>
	/// Abstract base class for search filters relating to the namespace of the machine-readable content of contracts.
	/// </summary>
	public abstract class FilterMachineContentNamespace : SearchFilter
	{
		/// <summary>
		/// Abstract base class for search filters relating to the namespace of the machine-readable content of contracts.
		/// </summary>
		/// <param name="Operands">Operands</param>
		public FilterMachineContentNamespace(params SearchFilterOperand[] Operands)
			: base(Operands)
		{
		}

		/// <summary>
		/// Local XML element name of filter.
		/// </summary>
		public override string ElementName => "namespace";

		/// <summary>
		/// Sort order
		/// </summary>
		internal override int Order => 2;

		/// <summary>
		/// Maximum number of occurrences in a search.
		/// </summary>
		internal override int MaxOccurs => 1;
	}
}
