﻿using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Queue;

namespace Waher.Runtime.Console.Worker
{
	/// <summary>
	/// Used to signal end of queue has been reached.
	/// </summary>
	public class ConsoleFlush : WorkItem
	{
		private readonly TaskCompletionSource<bool> result;
		private readonly bool terminate;

		/// <summary>
		/// Used to signal end of queue has been reached.
		/// </summary>
		/// <param name="Terminate">If console serialization should be terminated.</param>
		/// <param name="Result">Where the result will be returned.</param>
		public ConsoleFlush(bool Terminate, TaskCompletionSource<bool> Result)
		{
			this.terminate = Terminate;
			this.result = Result;
		}

		/// <summary>
		/// Executes the console operation.
		/// </summary>
		/// <param name="Cancel">Cancellation token.</param>
		public override async Task Execute(CancellationToken Cancel)
		{
			await System.Console.Out.FlushAsync();

			if (this.terminate)
				_ = Task.Run(async () => await ConsoleWorker.Terminate());

			this.result.TrySetResult(true);
		}
	}
}
