﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Waher.Runtime.Threading;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;

namespace Waher.Script.Data.Model
{
	/// <summary>
	/// Manages a Microsoft SQL Server connection
	/// </summary>
	public class MsSqlDatabase : ExternalDatabase
	{
		private readonly Dictionary<string, MsSqlStoredProcedure> procedures = new Dictionary<string, MsSqlStoredProcedure>();
		private readonly MultiReadSingleWriteObject synchObject = new MultiReadSingleWriteObject();
		private SqlConnection connection;

		/// <summary>
		/// Manages a Microsoft SQL Server connection
		/// </summary>
		/// <param name="Connection">Connection</param>
		public MsSqlDatabase(SqlConnection Connection)
		{
			this.connection = Connection;
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public override void Dispose()
		{
			this.connection?.Close();
			this.connection?.Dispose();
			this.connection = null;
		}

		/// <summary>
		/// Executes an SQL Statement on the database.
		/// </summary>
		/// <param name="Statement">SQL Statement.</param>
		/// <returns>Result</returns>
		public override async Task<IElement> ExecuteSqlStatement(string Statement)
		{
			using (SqlCommand Command = this.connection.CreateCommand())
			{
				Command.CommandType = CommandType.Text;
				Command.CommandText = Statement;
				SqlDataReader Reader = await Command.ExecuteReaderAsync();

				return await Reader.ParseAndClose();
			}
		}

		/// <summary>
		/// Gets a Schema table, given its collection name. 
		/// For a list of collections: https://mysqlconnector.net/overview/schema-collections/
		/// </summary>
		/// <param name="Name">Schema collection</param>
		/// <returns>Schema table, as a matrix</returns>
		public override Task<IElement> GetSchema(string Name)
		{
			DataTable Table = this.connection.GetSchema(Name);
			return Task.FromResult<IElement>(Table.ToMatrix());
		}

		/// <summary>
		/// Creates a lambda expression for accessing a stored procedure.
		/// </summary>
		/// <param name="Name">Name of stored procedure.</param>
		/// <returns>Lambda expression.</returns>
		public override async Task<ILambdaExpression> GetProcedure(string Name)
		{
			await this.synchObject.BeginWrite();
			try
			{
				if (this.procedures.TryGetValue(Name, out MsSqlStoredProcedure Result))
					return Result;

				SqlCommand Command = this.connection.CreateCommand();
				Command.CommandType = CommandType.StoredProcedure;
				Command.CommandText = this.connection.Database + "." + Name;
				
				SqlCommandBuilder.DeriveParameters(Command);

				Result = new MsSqlStoredProcedure(Command);
				this.procedures[Name] = Result;

				return Result;
			}
			finally
			{
				await this.synchObject.EndWrite();
			}
		}
	}
}
