﻿using System;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;
using F = Waher.Persistence.Filters;
using Waher.Runtime.Collections;

namespace Waher.Persistence.Files.Searching
{
	/// <summary>
	/// This filter selects objects that conform to all child-filters provided.
	/// </summary>
	public class FilterAnd : F.FilterAnd, IApplicableFilter
	{
		private readonly IApplicableFilter[] applicableFilters;
		private readonly string[] constantFields;

		/// <summary>
		/// This filter selects objects that conform to all child-filters provided.
		/// </summary>
		/// <param name="ApplicableFilters">Applicable filters.</param>
		/// <param name="Filters">Child filters.</param>
		internal FilterAnd(IApplicableFilter[] ApplicableFilters, F.Filter[] Filters)
			: base(Filters)
		{
			this.applicableFilters = ApplicableFilters;

			this.constantFields = null;

			foreach (IApplicableFilter Filter in ApplicableFilters)
				this.constantFields = MergeConstantFields(this.constantFields, Filter.ConstantFields);
		}

		internal static string[] MergeConstantFields(string[] ConstantFields1, string[] ConstantFields2)
		{
			if (ConstantFields1 is null)
				return ConstantFields2;
			else if (ConstantFields2 is null)
				return ConstantFields1;
			else
			{
				ChunkedList<string> Union = null;

				foreach (string s in ConstantFields2)
				{
					if (Array.IndexOf(ConstantFields1, s) >= 0)
						continue;

					if (Union is null)
					{
						Union = new ChunkedList<string>();
						Union.AddRange(ConstantFields1);
					}

					Union.Add(s);
				}

				if (Union is null)
					return ConstantFields1;
				else
					return Union.ToArray();
			}
		}

		/// <summary>
		/// Checks if the filter applies to the object.
		/// </summary>
		/// <param name="Object">Object.</param>
		/// <param name="Serializer">Corresponding object serializer.</param>
		/// <param name="Provider">Files provider.</param>
		/// <returns>If the filter can be applied.</returns>
		public async Task<bool> AppliesTo(object Object, IObjectSerializer Serializer, FilesProvider Provider)
		{
			foreach (IApplicableFilter F in this.applicableFilters)
			{
				if (!await F.AppliesTo(Object, Serializer, Provider))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Gets an array of constant fields. Can return null, if there are no constant fields.
		/// </summary>
		public string[] ConstantFields => this.constantFields;
	}
}
