﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Runtime.Inventory;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Graphs;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Content.Functions.InputOutput
{
	/// <summary>
	/// SaveFile(Object,FileName)
	/// </summary>
	public class SaveFile : FunctionTwoVariables
	{
		/// <summary>
		/// SaveFile(Object,FileName)
		/// </summary>
		/// <param name="Object">Object to encode and save.</param>
		/// <param name="FileName">File name.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public SaveFile(ScriptNode Object, ScriptNode FileName, int Start, int Length, Expression Expression)
			: base(Object, FileName, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName => nameof(SaveFile);

		/// <summary>
		/// Default Argument names
		/// </summary>
		public override string[] DefaultArgumentNames => new string[] { "Object", "FileName" };

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="ScriptNode.EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => true;

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement Argument1, IElement Argument2, Variables Variables)
		{
			return this.EvaluateAsync(Argument1, Argument2, Variables).Result;
		}

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Argument1">Function argument 1.</param>
		/// <param name="Argument2">Function argument 2.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override async Task<IElement> EvaluateAsync(IElement Argument1, IElement Argument2, Variables Variables)
		{
			object Obj = Argument1.AssociatedObjectValue;
			string FileName = Argument2.AssociatedObjectValue.ToString();

			if (Obj is Graph G)
				Obj = G.CreatePixels();

			byte[] Bin = null;

			if (InternetContent.TryGetContentType(Path.GetExtension(FileName), out string ContentType) &&
				InternetContent.Encodes(Obj, out Grade _, out IContentEncoder Encoder, ContentType))
			{
				KeyValuePair<byte[], string> P = await Encoder.EncodeAsync(Obj, System.Text.Encoding.UTF8, ContentType);
				Bin = P.Key;
				ContentType = P.Value;
			}

			if (Bin is null)
			{
				KeyValuePair<byte[], string> P = await InternetContent.EncodeAsync(Obj, System.Text.Encoding.UTF8);
				Bin = P.Key;
				ContentType = P.Value;
			}

			using (FileStream fs = File.Create(FileName))
			{
				await fs.WriteAsync(Bin, 0, Bin.Length);
			}

			return new StringValue(ContentType);
		}
	}
}
