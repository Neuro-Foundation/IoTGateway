﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Waher.Runtime.Collections;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects.VectorSpaces;

namespace Waher.Script.Xml.Model
{
	/// <summary>
	/// XML Script element node.
	/// </summary>
	public class XmlScriptElement : XmlScriptNode
	{
		private XmlScriptAttribute xmlns;
		private readonly XmlScriptAttribute[] attributes;
		private ChunkedList<XmlScriptNode> children = null;
		private readonly string name;
		private readonly int nrAttributes;
		private bool isAsync;

		/// <summary>
		/// XML Script element node.
		/// </summary>
		/// <param name="Name">Element name.</param>
		/// <param name="Xmlns">XML Namespace attribute.</param>
		/// <param name="Attributes">Attributes</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public XmlScriptElement(string Name, XmlScriptAttribute Xmlns, XmlScriptAttribute[] Attributes,
			int Start, int Length, Expression Expression)
			: base(Start, Length, Expression)
		{
			this.name = Name;

			this.xmlns = Xmlns;
			this.xmlns?.SetParent(this);

			this.attributes = Attributes;
			this.attributes?.SetParent(this);

			this.nrAttributes = Attributes.Length;

			this.CalcIsAsync();
		}

		private void CalcIsAsync()
		{
			this.isAsync = this.xmlns?.IsAsynchronous ?? false;
			if (this.isAsync)
				return;

			ChunkNode<XmlScriptNode> Loop;
			int i, c;

			for (i = 0; i < this.nrAttributes; i++)
			{
				if (this.attributes[i]?.IsAsynchronous ?? false)
				{
					this.isAsync = true;
					return;
				}
			}

			Loop = this.children?.FirstChunk;

			while (!(Loop is null))
			{
				for (i = Loop.Start, c = Loop.Pos; i < c; i++)
				{
					if (Loop[i]?.IsAsynchronous ?? false)
					{
						this.isAsync = true;
						return;
					}
				}

				Loop = Loop.Next;
			}
		}

		/// <summary>
		/// If the node (or its decendants) include asynchronous evaluation. Asynchronous nodes should be evaluated using
		/// <see cref="ScriptNode.EvaluateAsync(Variables)"/>.
		/// </summary>
		public override bool IsAsynchronous => this.isAsync;

		/// <summary>
		/// Adds a XML Script node to the element.
		/// </summary>
		/// <param name="Node">Node to add.</param>
		public void Add(XmlScriptNode Node)
		{
			if (this.children is null)
				this.children = new ChunkedList<XmlScriptNode>();

			this.children.Add(Node);
			Node?.SetParent(this);

			this.isAsync |= Node?.IsAsynchronous ?? false;
		}

		/// <summary>
		/// Calls the callback method for all child nodes.
		/// </summary>
		/// <param name="Callback">Callback method to call.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <param name="Order">Order to traverse the nodes.</param>
		/// <returns>If the process was completed.</returns>
		public override bool ForAllChildNodes(ScriptNodeEventHandler Callback, object State, SearchMethod Order)
		{
			ChunkNode<XmlScriptNode> Loop;
			int i, c;

			if (Order == SearchMethod.DepthFirst)
			{
				if (!(this.xmlns?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;

				for (i = 0; i < this.nrAttributes; i++)
				{
					if (!this.attributes[i].ForAllChildNodes(Callback, State, Order))
						return false;
				}

				Loop = this.children?.FirstChunk;

				while (!(Loop is null))
				{
					for (i = Loop.Start, c = Loop.Pos; i < c; i++)
					{
						if (!(Loop[i]?.ForAllChildNodes(Callback, State, Order) ?? false))
							return false;
					}

					Loop = Loop.Next;
				}
			}

			ScriptNode NewNode;
			bool RecalcIsAsync = false;
			bool b;

			if (!(this.xmlns is null))
			{
				b = !Callback(this.xmlns, out NewNode, State);
				if (!(NewNode is null) && NewNode is XmlScriptAttribute NewAttr)
				{
					this.xmlns = NewAttr;
					this.xmlns.SetParent(this);

					RecalcIsAsync = true;
				}

				if (b || (Order == SearchMethod.TreeOrder && !this.xmlns.ForAllChildNodes(Callback, State, Order)))
				{
					if (RecalcIsAsync)
						this.CalcIsAsync();

					return false;
				}
			}

			for (i = 0; i < this.nrAttributes; i++)
			{
				b = !Callback(this.attributes[i], out NewNode, State);
				if (!(NewNode is null) && NewNode is XmlScriptAttribute Attr)
				{
					this.attributes[i] = Attr;
					Attr.SetParent(this);

					RecalcIsAsync = true;
				}

				if (b || (Order == SearchMethod.TreeOrder && !this.attributes[i].ForAllChildNodes(Callback, State, Order)))
				{
					if (RecalcIsAsync)
						this.CalcIsAsync();

					return false;
				}
			}

			Loop = this.children?.FirstChunk;
			while (!(Loop is null))
			{
				for (i = Loop.Start, c = Loop.Pos; i < c; i++)
				{
					b = !Callback(Loop[i], out NewNode, State);
					if (!(NewNode is null) && NewNode is XmlScriptNode Node2)
					{
						Loop[i] = Node2;
						Node2.SetParent(this);

						RecalcIsAsync = true;
					}

					if (b || (Order == SearchMethod.TreeOrder && !Loop[i].ForAllChildNodes(Callback, State, Order)))
					{
						if (RecalcIsAsync)
							this.CalcIsAsync();

						return false;
					}
				}

				Loop = Loop.Next;
			}

			if (RecalcIsAsync)
				this.CalcIsAsync();

			if (Order == SearchMethod.BreadthFirst)
			{
				if (!(this.xmlns?.ForAllChildNodes(Callback, State, Order) ?? true))
					return false;

				for (i = 0; i < this.nrAttributes; i++)
				{
					if (!this.attributes[i].ForAllChildNodes(Callback, State, Order))
						return false;
				}

				Loop = this.children?.FirstChunk;

				while (!(Loop is null))
				{
					for (i = Loop.Start, c = Loop.Pos; i < c; i++)
					{
						if (!(Loop[i]?.ForAllChildNodes(Callback, State, Order) ?? false))
							return false;
					}

					Loop = Loop.Next;
				}
			}

			return true;
		}

		/// <summary>
		/// Builds an XML Document object
		/// </summary>
		/// <param name="Document">Document being built.</param>
		/// <param name="Parent">Parent element.</param>
		/// <param name="Variables">Current set of variables.</param>
		internal override void Build(XmlDocument Document, XmlElement Parent, Variables Variables)
		{
			string ns = this.xmlns?.GetValue(Variables) ?? null;
			XmlElement E;

			if (ns is null)
			{
				if (Parent is null || string.IsNullOrEmpty(ns = Parent.NamespaceURI))
				{
					if (Variables.TryGetVariable(XmlScriptValue.ParentNamespaceVariableName, out Variable v) && v.ValueObject is string s)
						ns = s;
				}

				if (string.IsNullOrEmpty(ns))
					E = Document.CreateElement(this.name);
				else
					E = Document.CreateElement(this.name, ns);
			}
			else
				E = Document.CreateElement(this.name, ns);

			if (Parent is null)
				Document.AppendChild(E);
			else
				Parent.AppendChild(E);

			foreach (XmlScriptAttribute Attr in this.attributes)
				Attr.Build(Document, E, Variables);

			if (!(this.children is null))
			{
				foreach (XmlScriptNode Node in this.children)
					Node.Build(Document, E, Variables);
			}
		}

		/// <summary>
		/// Builds an XML Document object
		/// </summary>
		/// <param name="Document">Document being built.</param>
		/// <param name="Parent">Parent element.</param>
		/// <param name="Variables">Current set of variables.</param>
		internal override async Task BuildAsync(XmlDocument Document, XmlElement Parent, Variables Variables)
		{
			if (!this.isAsync)
			{
				this.Build(Document, Parent, Variables);
				return;
			}

			string ns;
			XmlElement E;

			if (this.xmlns is null)
				ns = null;
			else
				ns = await this.xmlns.GetValueAsync(Variables);

			if (ns is null)
			{
				if (Parent is null || string.IsNullOrEmpty(ns = Parent.NamespaceURI))
				{
					if (Variables.TryGetVariable(XmlScriptValue.ParentNamespaceVariableName, out Variable v) && v.ValueObject is string s)
						ns = s;
				}

				if (string.IsNullOrEmpty(ns))
					E = Document.CreateElement(this.name);
				else
					E = Document.CreateElement(this.name, ns);
			}
			else
				E = Document.CreateElement(this.name, ns);

			if (Parent is null)
				Document.AppendChild(E);
			else
				Parent.AppendChild(E);

			foreach (XmlScriptAttribute Attr in this.attributes)
				await Attr.BuildAsync(Document, E, Variables);

			if (!(this.children is null))
			{
				foreach (XmlScriptNode Node in this.children)
					await Node.BuildAsync(Document, E, Variables);
			}
		}

		/// <summary>
		/// Performs a pattern match operation.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="AlreadyFound">Variables already identified.</param>
		/// <returns>Pattern match result</returns>
		public override PatternMatchResult PatternMatch(XmlNode CheckAgainst, Dictionary<string, IElement> AlreadyFound)
		{
			PatternMatchResult Result;

			if (CheckAgainst is null)
			{
				if (!(this.xmlns is null))
				{
					Result = this.xmlns.PatternMatch(null, AlreadyFound);
					if (Result != PatternMatchResult.Match)
						return Result;
				}

				foreach (XmlScriptAttribute Attr in this.attributes)
				{
					Result = Attr.PatternMatch(null, AlreadyFound);
					if (Result != PatternMatchResult.Match)
						return Result;
				}

				if (!(this.children is null))
				{
					foreach (XmlScriptNode N2 in this.children)
					{
						Result = N2.PatternMatch(null, AlreadyFound);
						if (Result != PatternMatchResult.Match)
							return Result;
					}
				}
			}
			else
			{
				if (!(CheckAgainst is XmlElement E))
					return PatternMatchResult.NoMatch;

				if (E.LocalName != this.name)
					return PatternMatchResult.NoMatch;

				if (!(this.xmlns is null))
				{
					Result = this.xmlns.PatternMatch(CheckAgainst.NamespaceURI, AlreadyFound);
					if (Result != PatternMatchResult.Match)
						return Result;
				}

				bool Wildcard = false;

				foreach (XmlScriptAttribute Attr in this.attributes)
				{
					if (string.IsNullOrEmpty(Attr.Name))
						Wildcard = true;
					else
					{
						XmlAttribute Attr2 = E.Attributes[Attr.Name];

						Result = Attr.PatternMatch(Attr2, AlreadyFound);
						if (Result != PatternMatchResult.Match)
							return Result;
					}
				}

				if (!Wildcard)
				{
					foreach (XmlAttribute Attr in E.Attributes)
					{
						if (Attr.Name == "xmlns")
							continue;

						bool Found = false;

						foreach (XmlScriptAttribute Attr2 in this.attributes)
						{
							if (Attr2.Name == Attr.Name)
							{
								Found = true;
								break;
							}
						}

						if (!Found)
							return PatternMatchResult.NoMatch;
					}
				}

				XmlNode N = E.FirstChild;
				bool Again;

				if (!(this.children is null))
				{
					foreach (XmlScriptNode N2 in this.children)
					{
						if (N2.IsWhitespace)
							continue;

						do
						{
							Again = false;

							if (!(N is null))
							{
								if (N2.IsApplicable(N, null))
								{
									if (N2.IsVector)
									{
										if (N is XmlElement E2)
										{
											ChunkedList<XmlElement> Elements = new ChunkedList<XmlElement>() { E2 };

											while (true)
											{
												N = N.NextSibling;
												if (N is null)
													break;
												else if (N is XmlElement E3)
												{
													if (N2.IsApplicable(N, E2))
														Elements.Add(E3);
													else
														break;
												}
												else if ((N is XmlText && string.IsNullOrWhiteSpace(N.InnerText)) ||
													N is XmlComment)
												{
													continue;
												}
												else
													break;
											}

											Result = N2.PatternMatch(new ObjectVector(Elements.ToArray()), AlreadyFound);
										}
										else if ((N is XmlText && string.IsNullOrWhiteSpace(N.InnerText)) ||
											N is XmlComment)
										{
											N = N.NextSibling;
											Again = true;
											continue;
										}
										else
										{
											Result = N2.PatternMatch(N, AlreadyFound);
											N = N.NextSibling;
										}
									}
									else
									{
										Result = N2.PatternMatch(N, AlreadyFound);
										N = N.NextSibling;
									}
								}
								else if ((N is XmlText && string.IsNullOrWhiteSpace(N.InnerText)) ||
									N is XmlComment)
								{
									N = N.NextSibling;
									Again = true;
									continue;
								}
								else
									Result = N2.PatternMatch(null, AlreadyFound);
							}
							else
								Result = N2.PatternMatch(null, AlreadyFound);

							if (Result != PatternMatchResult.Match)
								return Result;
						}
						while (Again);
					}
				}

				while (!(N is null))
				{
					if (N is XmlWhitespace || N is XmlSignificantWhitespace || N is XmlComment ||
						((N is XmlText || N is XmlCDataSection) && string.IsNullOrWhiteSpace(N.InnerXml)))
					{
						N = N.NextSibling;
					}
					else
						return PatternMatchResult.NoMatch;
				}
			}

			return PatternMatchResult.Match;
		}

		/// <summary>
		/// If the node is applicable in pattern matching against <paramref name="CheckAgainst"/>.
		/// </summary>
		/// <param name="CheckAgainst">Value to check against.</param>
		/// <param name="First">First element</param>
		/// <returns>If the node is applicable for pattern matching.</returns>
		public override bool IsApplicable(XmlNode CheckAgainst, XmlElement First)
		{
			if (!(CheckAgainst is XmlElement E))
				return false;

			if (First is null)
			{
				if (this.name != E.LocalName)
					return false;

				if (this.xmlns is null)
					return true;

				return this.xmlns.IsApplicable(CheckAgainst.NamespaceURI);
			}
			else
			{
				return 
					E.LocalName == First.LocalName &&
					E.NamespaceURI == First.NamespaceURI;
			}
		}
	}
}
