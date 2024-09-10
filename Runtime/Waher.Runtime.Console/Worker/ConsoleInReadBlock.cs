﻿using System;
using System.Threading.Tasks;

namespace Waher.Runtime.Console.Worker
{
	/// <summary>
	/// Reads a block of characters from the console.
	/// </summary>
	public class ConsoleInReadBlock : WorkItem
	{
		private readonly TaskCompletionSource<int> result;
		private readonly char[] buffer;
		private readonly int index;
		private readonly int count;

		/// <summary>
		/// Reads a block of characters from the console.
		/// </summary>
		/// <param name="Buffer">
		/// When this method returns, contains the specified character array with the values
		/// between index and (index + count - 1) replaced by the characters read from the
		/// current source.
		/// </param>
		/// <param name="Index">The position in buffer at which to begin writing.</param>
		/// <param name="Count">The maximum number of characters to read. If the end of the text is reached before
		/// the specified number of characters is read into the buffer, the current method
		/// returns.</param>
		/// <param name="Result">Where the result will be returned.</param>
		public ConsoleInReadBlock(char[] Buffer, int Index, int Count, TaskCompletionSource<int> Result)
		{
			this.buffer = Buffer;
			this.index = Index;
			this.count = Count;
			this.result = Result;
		}

		/// <summary>
		/// Executes the console operation.
		/// </summary>
		public override async Task Execute()
		{
			try
			{
				int Result = await System.Console.In.ReadBlockAsync(this.buffer, this.index, this.count);
				this.result.TrySetResult(Result);
			}
			catch (Exception ex)
			{
				this.result.TrySetException(ex);
			}
		}
	}
}
