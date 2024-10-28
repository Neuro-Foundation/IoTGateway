﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;

namespace Waher.Persistence.Files.Searching
{
	/// <summary>
	/// Provides a cursor that enumerates ranges of values using an index.
	/// </summary>
	/// <typeparam name="T">Class defining how to deserialize objects found.</typeparam>
	internal class RangesCursor<T> : ICursor<T>
	{
		private readonly RangeInfo[] ranges;
		private readonly IndexBTreeFile index;
		private readonly IApplicableFilter[] additionalfilters;
		private RangeInfo[] currentLimits;
		private ICursor<T> currentRange;
		private KeyValuePair<string, IApplicableFilter>[] startRangeFilters;
		private KeyValuePair<string, IApplicableFilter>[] endRangeFilters;
		private readonly FilesProvider provider;
		private readonly int nrRanges;
		private int limitsUpdatedAt;
		private readonly bool firstAscending;
		private readonly bool[] ascending;

		/// <summary>
		/// Provides a cursor that joins results from multiple cursors. It only returns an object once, regardless of how many times
		/// it appears in the different child cursors.
		/// </summary>
		/// <param name="Index">Index.</param>
		/// <param name="Ranges">Ranges to enumerate.</param>
		/// <param name="AdditionalFilters">Additional filters.</param>
		/// <param name="Provider">Files provider.</param>
		public RangesCursor(IndexBTreeFile Index, RangeInfo[] Ranges, IApplicableFilter[] AdditionalFilters, FilesProvider Provider)
		{
			this.index = Index;
			this.ranges = Ranges;
			this.additionalfilters = AdditionalFilters;
			this.currentRange = null;
			this.ascending = Index.Ascending;
			this.firstAscending = this.ascending[0];
			this.nrRanges = this.ranges.Length;
			this.provider = Provider;

			this.Reset();
		}

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public T Current => this.CurrentCursor.Current;

		private ICursor<T> CurrentCursor
		{
			get
			{
				if (this.currentRange is null)
					throw new InvalidOperationException("Enumeration not started or has already ended.");
				else
					return this.currentRange;
			}
		}

		/// <summary>
		/// Serializer used to deserialize <see cref="Current"/>.
		/// </summary>
		public IObjectSerializer CurrentSerializer => this.CurrentCursor.CurrentSerializer;

		/// <summary>
		/// If the curent object is type compatible with <typeparamref name="T"/> or not. If not compatible, <see cref="Current"/> 
		/// will be null, even if there exists an object at the current position.
		/// </summary>
		public bool CurrentTypeCompatible => this.CurrentCursor.CurrentTypeCompatible;

		/// <summary>
		/// Gets the Object ID of the current object.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		public Guid CurrentObjectId => this.CurrentCursor.CurrentObjectId;

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.currentRange = null;
		}

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// Note: Enumerator only works if object is locked.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		Task<bool> IAsyncEnumerator.MoveNextAsync() => this.MoveNextAsyncLocked();

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the enumeration has not started. 
		/// Call <see cref="MoveNextAsyncLocked()"/> to start the enumeration after creating or resetting it.</exception>
		object IEnumerator.Current => this.Current;

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// Note: Enumerator only works if object is locked.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public bool MoveNext() => this.MoveNextAsyncLocked().Result;

		/// <summary>
		/// Resets the enumerator.
		/// </summary>
		public void Reset()
		{
			int i;

			this.currentLimits = new RangeInfo[this.nrRanges];
			this.currentRange = null;

			for (i = 0; i < this.nrRanges; i++)
				this.currentLimits[i] = this.ranges[i].Copy();
		}

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if
		/// the enumerator has passed the end of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public async Task<bool> MoveNextAsyncLocked()
		{
			int i;

			while (true)
			{
				if (this.currentRange is null)
				{
					List<KeyValuePair<string, object>> SearchParameters = new List<KeyValuePair<string, object>>();
					List<KeyValuePair<string, IApplicableFilter>> StartFilters = null;
					List<KeyValuePair<string, IApplicableFilter>> EndFilters = null;
					RangeInfo Range;
					object Value;

					for (i = 0; i < this.nrRanges; i++)
					{
						Range = this.currentLimits[i];

						if (Range.IsPoint)
						{
							if (EndFilters is null)
								EndFilters = new List<KeyValuePair<string, IApplicableFilter>>();

							SearchParameters.Add(new KeyValuePair<string, object>(Range.FieldName, Range.Point));
							EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldEqualTo(Range.FieldName, Range.Point)));
						}
						else
						{
							if (Range.HasMin)
							{
								Value = Range.Min;

								if (this.ascending[i])
								{
									if (StartFilters is null)
										StartFilters = new List<KeyValuePair<string, IApplicableFilter>>();

									if (Range.MinInclusive)
										StartFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldGreaterOrEqualTo(Range.FieldName, Value)));
									else
									{
										StartFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldGreaterThan(Range.FieldName, Value)));

										if (!Comparison.Increment(ref Value))
											return false;
									}

									SearchParameters.Add(new KeyValuePair<string, object>(Range.FieldName, Value));
								}
								else
								{
									if (EndFilters is null)
										EndFilters = new List<KeyValuePair<string, IApplicableFilter>>();

									if (Range.MinInclusive)
										EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldGreaterOrEqualTo(Range.FieldName, Value)));
									else
										EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldGreaterThan(Range.FieldName, Value)));
								}
							}

							if (Range.HasMax)
							{
								Value = Range.Max;

								if (!this.ascending[i])
								{
									if (StartFilters is null)
										StartFilters = new List<KeyValuePair<string, IApplicableFilter>>();

									if (Range.MaxInclusive)
										StartFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldLesserOrEqualTo(Range.FieldName, Value)));
									else
									{
										StartFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldLesserThan(Range.FieldName, Value)));

										if (!Comparison.Decrement(ref Value))
											return false;
									}

									SearchParameters.Add(new KeyValuePair<string, object>(Range.FieldName, Value));
								}
								else
								{
									if (EndFilters is null)
										EndFilters = new List<KeyValuePair<string, IApplicableFilter>>();

									if (Range.MaxInclusive)
										EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldLesserOrEqualTo(Range.FieldName, Value)));
									else
										EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldLesserThan(Range.FieldName, Value)));
								}
							}
						}
					}

					if (this.firstAscending)
						this.currentRange = await this.index.FindFirstGreaterOrEqualToLocked<T>(SearchParameters.ToArray());
					else
						this.currentRange = await this.index.FindLastLesserOrEqualToLocked<T>(SearchParameters.ToArray());

					this.startRangeFilters = StartFilters?.ToArray();
					this.endRangeFilters = EndFilters?.ToArray();
					this.limitsUpdatedAt = this.nrRanges;
				}

				if (!await this.currentRange.MoveNextAsyncLocked())
				{
					this.currentRange = null;

					if (this.limitsUpdatedAt >= this.nrRanges)
						return false;

					continue;
				}

				if (!this.currentRange.CurrentTypeCompatible)
					continue;

				object CurrentValue = this.currentRange.Current;
				IObjectSerializer CurrentSerializer = this.currentRange.CurrentSerializer;
				string OutOfStartRangeField = null;
				string OutOfEndRangeField = null;
				bool Ok = true;
				bool Smaller;

				if (!(this.additionalfilters is null))
				{
					foreach (IApplicableFilter Filter in this.additionalfilters)
					{
						if (!await Filter.AppliesTo(CurrentValue, CurrentSerializer, this.provider))
						{
							Ok = false;
							break;
						}
					}
				}

				if (!(this.startRangeFilters is null))
				{
					foreach (KeyValuePair<string, IApplicableFilter> Filter in this.startRangeFilters)
					{
						if (!await Filter.Value.AppliesTo(CurrentValue, CurrentSerializer, this.provider))
						{
							OutOfStartRangeField = Filter.Key;
							Ok = false;
							break;
						}
					}
				}

				if (!(this.endRangeFilters is null) && OutOfStartRangeField is null)
				{
					foreach (KeyValuePair<string, IApplicableFilter> Filter in this.endRangeFilters)
					{
						if (!await Filter.Value.AppliesTo(CurrentValue, CurrentSerializer, this.provider))
						{
							OutOfEndRangeField = Filter.Key;
							Ok = false;
							break;
						}
					}
				}

				for (i = 0; i < this.limitsUpdatedAt; i++)
				{
					object FieldValue = await CurrentSerializer.TryGetFieldValue(this.ranges[i].FieldName, CurrentValue);

					if (!(FieldValue is null))
					{
						if (this.ascending[i])
						{
							if (this.currentLimits[i].SetMin(FieldValue, !(OutOfStartRangeField is null), out Smaller) && Smaller)
							{
								i++;
								this.limitsUpdatedAt = i;

								while (i < this.nrRanges)
								{
									this.ranges[i].CopyTo(this.currentLimits[i]);
									i++;
								}
							}
						}
						else
						{
							if (this.currentLimits[i].SetMax(FieldValue, !(OutOfStartRangeField is null), out Smaller) && Smaller)
							{
								i++;
								this.limitsUpdatedAt = i;

								while (i < this.nrRanges)
								{
									this.ranges[i].CopyTo(this.currentLimits[i]);
									i++;
								}
							}
						}
					}
				}

				if (Ok)
					return true;
				else if (!(OutOfStartRangeField is null) || !(OutOfEndRangeField is null))
				{
					this.currentRange = null;

					if (this.limitsUpdatedAt >= this.nrRanges)
						return false;
				}
			}
		}

		/// <summary>
		/// Advances the enumerator to the previous element of the collection.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced to the previous element; false if
		/// the enumerator has passed the beginning of the collection.</returns>
		/// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
		public async Task<bool> MovePreviousAsyncLocked()
		{
			int i;

			while (true)
			{
				if (this.currentRange is null)
				{
					List<KeyValuePair<string, object>> SearchParameters = new List<KeyValuePair<string, object>>();
					List<KeyValuePair<string, IApplicableFilter>> StartFilters = null;
					List<KeyValuePair<string, IApplicableFilter>> EndFilters = null;
					RangeInfo Range;
					object Value;

					for (i = 0; i < this.nrRanges; i++)
					{
						Range = this.currentLimits[i];

						if (Range.IsPoint)
						{
							if (EndFilters is null)
								EndFilters = new List<KeyValuePair<string, IApplicableFilter>>();

							SearchParameters.Add(new KeyValuePair<string, object>(Range.FieldName, Range.Point));
							EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldEqualTo(Range.FieldName, Range.Point)));
						}
						else
						{
							if (Range.HasMin)
							{
								Value = Range.Min;

								if (this.ascending[i])
								{
									if (EndFilters is null)
										EndFilters = new List<KeyValuePair<string, IApplicableFilter>>();

									if (Range.MinInclusive)
										EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldGreaterOrEqualTo(Range.FieldName, Value)));
									else
										EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldGreaterThan(Range.FieldName, Value)));
								}
								else
								{
									if (StartFilters is null)
										StartFilters = new List<KeyValuePair<string, IApplicableFilter>>();

									if (Range.MinInclusive)
										StartFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldGreaterOrEqualTo(Range.FieldName, Value)));
									else
									{
										StartFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldGreaterThan(Range.FieldName, Value)));

										if (!Comparison.Increment(ref Value))
											return false;
									}

									SearchParameters.Add(new KeyValuePair<string, object>(Range.FieldName, Value));
								}
							}

							if (Range.HasMax)
							{
								Value = Range.Max;

								if (this.ascending[i])
								{
									if (StartFilters is null)
										StartFilters = new List<KeyValuePair<string, IApplicableFilter>>();

									if (Range.MaxInclusive)
										StartFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldLesserOrEqualTo(Range.FieldName, Value)));
									else
									{
										StartFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldLesserThan(Range.FieldName, Value)));

										if (!Comparison.Decrement(ref Value))
											return false;
									}

									SearchParameters.Add(new KeyValuePair<string, object>(Range.FieldName, Value));
								}
								else
								{
									if (EndFilters is null)
										EndFilters = new List<KeyValuePair<string, IApplicableFilter>>();

									if (Range.MaxInclusive)
										EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldLesserOrEqualTo(Range.FieldName, Value)));
									else
										EndFilters.Add(new KeyValuePair<string, IApplicableFilter>(Range.FieldName, new FilterFieldLesserThan(Range.FieldName, Value)));
								}
							}
						}
					}

					if (this.firstAscending)
						this.currentRange = await this.index.FindLastLesserOrEqualToLocked<T>(SearchParameters.ToArray());
					else
						this.currentRange = await this.index.FindFirstGreaterOrEqualToLocked<T>(SearchParameters.ToArray());

					this.startRangeFilters = StartFilters?.ToArray();
					this.endRangeFilters = EndFilters?.ToArray();
					this.limitsUpdatedAt = this.nrRanges;
				}

				if (!await this.currentRange.MovePreviousAsyncLocked())
				{
					this.currentRange = null;

					if (this.limitsUpdatedAt >= this.nrRanges)
						return false;

					continue;
				}

				if (!this.currentRange.CurrentTypeCompatible)
					continue;

				object CurrentValue = this.currentRange.Current;
				IObjectSerializer CurrentSerializer = this.currentRange.CurrentSerializer;
				string OutOfStartRangeField = null;
				string OutOfEndRangeField = null;
				bool Ok = true;
				bool Smaller;

				if (!(this.additionalfilters is null))
				{
					foreach (IApplicableFilter Filter in this.additionalfilters)
					{
						if (!await Filter.AppliesTo(CurrentValue, CurrentSerializer, this.provider))
						{
							Ok = false;
							break;
						}
					}
				}

				if (!(this.startRangeFilters is null))
				{
					foreach (KeyValuePair<string, IApplicableFilter> Filter in this.startRangeFilters)
					{
						if (!await Filter.Value.AppliesTo(CurrentValue, CurrentSerializer, this.provider))
						{
							OutOfStartRangeField = Filter.Key;
							Ok = false;
							break;
						}
					}
				}

				if (!(this.endRangeFilters is null) && OutOfStartRangeField is null)
				{
					foreach (KeyValuePair<string, IApplicableFilter> Filter in this.endRangeFilters)
					{
						if (!await Filter.Value.AppliesTo(CurrentValue, CurrentSerializer, this.provider))
						{
							OutOfEndRangeField = Filter.Key;
							Ok = false;
							break;
						}
					}
				}

				for (i = 0; i < this.limitsUpdatedAt; i++)
				{
					object FieldValue = await CurrentSerializer.TryGetFieldValue(this.ranges[i].FieldName, CurrentValue);

					if (!(FieldValue is null))
					{
						if (this.ascending[i])
						{
							if (this.currentLimits[i].SetMax(FieldValue, !(OutOfStartRangeField is null), out Smaller) && Smaller)
							{
								i++;
								this.limitsUpdatedAt = i;

								while (i < this.nrRanges)
								{
									this.ranges[i].CopyTo(this.currentLimits[i]);
									i++;
								}
							}
						}
						else
						{
							if (this.currentLimits[i].SetMin(FieldValue, !(OutOfStartRangeField is null), out Smaller) && Smaller)
							{
								i++;
								this.limitsUpdatedAt = i;

								while (i < this.nrRanges)
								{
									this.ranges[i].CopyTo(this.currentLimits[i]);
									i++;
								}
							}
						}
					}
				}

				if (Ok)
					return true;
				else if (!(OutOfStartRangeField is null) || !(OutOfEndRangeField is null))
				{
					this.currentRange = null;

					if (this.limitsUpdatedAt >= this.nrRanges)
						return false;
				}
			}
		}

		/// <summary>
		/// If the index ordering corresponds to a given sort order.
		/// </summary>
		/// <param name="ConstantFields">Optional array of names of fields that will be constant during the enumeration.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>If the index matches the sort order. (The index ordering is allowed to be more specific.)</returns>
		public bool SameSortOrder(string[] ConstantFields, string[] SortOrder)
		{
			return this.index.SameSortOrder(ConstantFields, SortOrder);
		}

		/// <summary>
		/// If the index ordering is a reversion of a given sort order.
		/// </summary>
		/// <param name="ConstantFields">Optional array of names of fields that will be constant during the enumeration.</param>
		/// <param name="SortOrder">Sort order. Each string represents a field name. By default, sort order is ascending.
		/// If descending sort order is desired, prefix the field name by a hyphen (minus) sign.</param>
		/// <returns>If the index matches the sort order. (The index ordering is allowed to be more specific.)</returns>
		public bool ReverseSortOrder(string[] ConstantFields, string[] SortOrder)
		{
			return this.index.ReverseSortOrder(ConstantFields, SortOrder);
		}

		/// <summary>
		/// Continues operating after a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public Task ContinueAfterLocked(T LastItem)
		{
			throw new NotSupportedException("Paginated search is not supported for queries with multiple ranges.");
		}

		/// <summary>
		/// Continues operating before a given item.
		/// </summary>
		/// <param name="LastItem">Last item in a previous process.</param>
		public Task ContinueBeforeLocked(T LastItem)
		{
			throw new NotSupportedException("Paginated search is not supported for queries with multiple ranges.");
		}
	}
}
