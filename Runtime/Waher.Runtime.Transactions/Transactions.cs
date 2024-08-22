﻿using System;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Runtime.Cache;
using Waher.Runtime.Inventory;

namespace Waher.Runtime.Transactions
{
	/// <summary>
	/// Maintains a collection of active transactions.
	/// </summary>
	/// <typeparam name="T">Type of transaction managed by the class</typeparam>
	public class Transactions<T> : ITransactions
		where T : ITransaction
	{
		private readonly Cache<Guid, T> transactions;

		/// <summary>
		/// Maintains a collection of active transactions.
		/// </summary>
		/// <param name="TransactionTimeout">Maximum time before a transaction needs to complete or fail.</param>
		public Transactions(TimeSpan TransactionTimeout)
		{
			this.transactions = new Cache<Guid, T>(int.MaxValue, TransactionTimeout, TransactionTimeout, true);
			this.transactions.Removed += this.Transactions_Removed;

			TransactionModule.Register(this);
		}

		private async void Transactions_Removed(object Sender, CacheItemEventArgs<Guid, T> e)
		{
			if (e.Reason != RemovedReason.Manual)
			{
				try
				{
					T Transaction = e.Value;

					if (Transaction.State != TransactionState.Committed &&
						Transaction.State != TransactionState.RolledBack)
					{
						await Transaction.Abort();
					}
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}
		}

		/// <summary>
		/// Rolls back any pending transactions and disposes of the object.
		/// </summary>
		public void Dispose()
		{
			TransactionModule.Unregister(this);
		
			this.transactions.Removed -= this.Transactions_Removed;
			this.transactions.Clear();
			this.transactions.Dispose();
		}

		/// <summary>
		/// Creates a new transaction
		/// </summary>
		/// <typeparam name="T2">Type of transaction class to create. Must be
		/// equal to <typeparamref name="T"/> or a descendant.</typeparam>
		/// <param name="Arguments">Constructor arguments.</param>
		/// <returns>New transaction</returns>
		public T2 CreateNew<T2>(params object[] Arguments)
			where T2 : T
		{
			T2 Transaction = Types.Instantiate<T2>(false, Arguments);
			this.transactions.Add(Transaction.Id, Transaction);
			return Transaction;
		}

		/// <summary>
		/// Register a transaction created elsewhere with the collection.
		/// </summary>
		/// <param name="Transaction">Transaction already created.</param>
		public void Register(T Transaction)
		{
			this.transactions.Add(Transaction.Id, Transaction);
		}

		/// <summary>
		/// Unregisters a transaction.
		/// </summary>
		/// <param name="Id">Transaction ID.</param>
		/// <returns>If the transaction was found and removed.</returns>
		public bool Unregister(Guid Id)
		{
			return this.transactions.Remove(Id);
		}

		/// <summary>
		/// Unregisters a transaction.
		/// </summary>
		/// <param name="Transaction">Transaction reference.</param>
		/// <returns>If the transaction was found and removed.</returns>
		public bool Unregister(T Transaction)
		{
			if (this.transactions.TryGetValue(Transaction.Id, out T Transaction2) &&
				Transaction2.Equals(Transaction))
			{
				return this.transactions.Remove(Transaction.Id);
			}
			else
				return false;
		}

		/// <summary>
		/// Tries to get a transaction, given its ID.
		/// </summary>
		/// <param name="Id">Transaction ID.</param>
		/// <param name="Transaction">Transaction, if found.</param>
		/// <returns>If a transaction with the corresponding ID was found.</returns>
		public bool TryGetTransaction(Guid Id, out T Transaction)
		{
			return this.transactions.TryGetValue(Id, out Transaction);
		}

		/// <summary>
		/// Prepares a transaction in the collection.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <returns>If a transaction with the corresponding ID was found, and successfully prepared.</returns>
		public async Task<bool> Prepare(Guid TransactionId)
		{
			try
			{
				if (!this.transactions.TryGetValue(TransactionId, out T Transaction))
					return false;

				return await Transaction.Prepare();
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return false;
			}
		}

		/// <summary>
		/// Executes a transaction in the collection.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <returns>If a transaction with the corresponding ID was found, and successfully executed.</returns>
		public async Task<bool> Execute(Guid TransactionId)
		{
			try
			{
				if (!this.transactions.TryGetValue(TransactionId, out T Transaction))
					return false;

				return await Transaction.Execute();
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return false;
			}
		}

		/// <summary>
		/// Cimmits a transaction in the collection.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <returns>If a transaction with the corresponding ID was found, and successfully committed.</returns>
		public async Task<bool> Commit(Guid TransactionId)
		{
			try
			{
				if (!this.transactions.TryGetValue(TransactionId, out T Transaction))
					return false;

				if (!await Transaction.Commit())
					return false;

				this.transactions.Remove(TransactionId);

				return true;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return false;
			}
		}

		/// <summary>
		/// Prepares a transaction in the collection.
		/// </summary>
		/// <param name="TransactionId">Transaction ID</param>
		/// <returns>If a transaction with the corresponding ID was found, and successfully prepared.</returns>
		public async Task<bool> Rollback(Guid TransactionId)
		{
			try
			{
				if (!this.transactions.TryGetValue(TransactionId, out T Transaction))
					return false;

				if (!await Transaction.Rollback())
					return false;

				this.transactions.Remove(TransactionId);

				return true;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return false;
			}
		}

		/// <summary>
		/// Gets pending transactions.
		/// </summary>
		public Task<ITransaction[]> GetTransactions()
		{
			T[] Transactions = this.transactions.GetValues();
			int i, c = Transactions.Length;
			ITransaction[] Result = new ITransaction[c];

			for (i = 0; i < c; i++)
				Result[i] = (ITransaction)Transactions[i];

			return Task.FromResult<ITransaction[]>(Result);
		}
	}
}
