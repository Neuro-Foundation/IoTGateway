﻿using System.Text;
using System.Collections.Generic;
using System.Globalization;
using Waher.Content.Asn1.Exceptions;
using Waher.Content.Asn1.Model;
using Waher.Content.Asn1.Model.Macro;
using Waher.Content.Asn1.Model.Restrictions;
using Waher.Content.Asn1.Model.Sets;
using Waher.Content.Asn1.Model.Types;
using Waher.Content.Asn1.Model.Values;
using System.Threading.Tasks;
using Waher.Runtime.IO;

namespace Waher.Content.Asn1
{
	/// <summary>
	/// Represents an ASN.1 document.
	/// </summary>
	public class Asn1Document
	{
		internal readonly Dictionary<string, Asn1Node> namedNodes = new Dictionary<string, Asn1Node>();
		internal readonly Dictionary<string, Asn1TypeDefinition> aliases = new Dictionary<string, Asn1TypeDefinition>();
		internal readonly Dictionary<string, Asn1FieldValueDefinition> values = new Dictionary<string, Asn1FieldValueDefinition>();
		internal int pos = 0;
		private readonly string text;
		private readonly string location;
		private readonly string[] importFolders;
		private readonly int len;
		private readonly int lenm1;
		private Asn1Definitions root;
		private int unnamedIndex = 1;

		private Asn1Document(string Text, string Location, string[] ImportFolders)
		{
			this.text = Text;
			this.location = Location;
			this.importFolders = ImportFolders;
			this.len = this.text.Length;
			this.lenm1 = this.len - 1;
		}

		/// <summary>
		/// Represents an ASN.1 document.
		/// </summary>
		/// <param name="Text">ASN.1 text to parse.</param>
		/// <param name="Location">Location of file.</param>
		/// <param name="ImportFolders">Import Folders.</param>
		public async Task<Asn1Document> CreateAsync(string Text, string Location, string[] ImportFolders)
		{
			Asn1Document Result = new Asn1Document(Text, Location, ImportFolders)
			{
				root = await this.ParseDefinitions()
			};

			return Result;
		}

		/// <summary>
		/// ASN.1 Root node
		/// </summary>
		public Asn1Definitions Root => this.root;

		/// <summary>
		/// ASN.1 text
		/// </summary>
		public string Text => this.text;

		/// <summary>
		/// Location of document.
		/// </summary>
		public string Location => this.location;

		/// <summary>
		/// Import folders.
		/// </summary>
		public string[] ImportFolders => this.importFolders;

		/// <summary>
		/// Read from file.
		/// </summary>
		/// <param name="FileName">Filename.</param>
		/// <param name="ImportFolders">Import folders.</param>
		/// <returns>ASN.1 document</returns>
		public static async Task<Asn1Document> FromFile(string FileName, string[] ImportFolders)
		{
			string Text = await Files.ReadAllTextAsync(FileName);
			return new Asn1Document(Text, FileName, ImportFolders);
		}

		internal void SkipWhiteSpace()
		{
			char ch;

			while (this.pos < this.len)
			{
				ch = this.text[this.pos];

				if (ch <= ' ' || ch == (char)160)
					this.pos++;
				else if (ch == '-' && this.pos < this.lenm1 && this.text[this.pos + 1] == '-')
				{
					this.pos += 2;

					while (this.pos < this.len)
					{
						ch = this.text[this.pos];
						if (ch == '\r' || ch == '\n')
							break;

						if (ch == '-' && this.pos < this.len - 1 && this.text[this.pos + 1] == '-')
						{
							this.pos += 2;
							break;
						}

						this.pos++;
					}
				}
				else if (ch == '/' && this.pos < this.lenm1 && this.text[this.pos + 1] == '*')
				{
					this.pos += 2;

					while ((this.pos < this.len && this.text[this.pos] != '*') ||
						(this.pos < this.lenm1 && this.text[this.pos + 1] != '/'))
					{
						this.pos++;
					}
				}
				else
					break;
			}
		}

		internal char NextChar()
		{
			if (this.pos < this.len)
				return this.text[this.pos++];
			else
				return (char)0;
		}

		internal char PeekNextChar()
		{
			if (this.pos < this.len)
				return this.text[this.pos];
			else
				return (char)0;
		}

		internal string NextToken()
		{
			this.SkipWhiteSpace();

			char ch = this.NextChar();

			if (char.IsLetter(ch) || char.IsDigit(ch) || ch == '-' || ch == '_')
			{
				int Start = this.pos - 1;

				while (this.pos < this.len && (char.IsLetter(ch = this.text[this.pos]) || char.IsDigit(ch) || ch == '-' || ch == '_'))
					this.pos++;

				return this.text.Substring(Start, this.pos - Start);
			}
			else
			{
				switch (ch)
				{
					case ':':
						switch (this.PeekNextChar())
						{
							case ':':
								this.pos++;
								switch (this.PeekNextChar())
								{
									case '=':
										this.pos++;
										return "::=";

									default:
										return "::";
								}

							default:
								return new string(ch, 1);
						}

					case '.':
						if (this.PeekNextChar() == '.')
						{
							this.pos++;
							if (this.PeekNextChar() == '.')
							{
								this.pos++;
								return "...";
							}
							else
								return "..";
						}
						else
							return ".";

					case '[':
						if (this.PeekNextChar() == '[')
						{
							this.pos++;
							return "[[";
						}
						else
							return "[";

					case ']':
						if (this.PeekNextChar() == ']')
						{
							this.pos++;
							return "]]";
						}
						else
							return "]";

					default:
						return new string(ch, 1);
				}
			}
		}

		internal string PeekNextToken()
		{
			this.SkipWhiteSpace();

			int Bak = this.pos;
			string s = this.NextToken();
			this.pos = Bak;
			return s;
		}

		internal void AssertNextToken(string ExpectedToken)
		{
			string s = this.NextToken();
			if (s != ExpectedToken)
				throw this.SyntaxError(ExpectedToken + " expected.");
		}

		internal Asn1SyntaxException SyntaxError(string Message)
		{
			return new Asn1SyntaxException(Message, this.text, this.pos);
		}

		private async Task<Asn1Definitions> ParseDefinitions()
		{
			string Identifier = this.ParseTypeNameIdentifier();
			string s = this.PeekNextToken();
			Asn1Oid Oid = null;

			if (s == "{")
			{
				Oid = this.ParseOid();
				s = this.PeekNextToken();
			}

			if (s != "DEFINITIONS")
				throw this.SyntaxError("DEFINITIONS expected.");

			this.pos += 11;

			Asn1Tags? Tags = null;
			bool Abstract = false;

			while (true)
			{
				switch (s = this.NextToken())
				{
					case "AUTOMATIC":
					case "IMPLICIT":
					case "EXPLICIT":
						if (Tags.HasValue)
							throw this.SyntaxError("TAGS already specified.");

						switch (s)
						{
							case "AUTOMATIC":
								Tags = Asn1Tags.Automatic;
								break;

							case "IMPLICIT":
								Tags = Asn1Tags.Implicit;
								break;

							case "EXPLICIT":
								Tags = Asn1Tags.Explicit;
								break;
						}

						this.AssertNextToken("TAGS");
						break;

					case "ABSTRACT-SYNTAX":
						Abstract = true;
						break;

					case "::=":
						s = null;
						break;

					default:
						throw this.SyntaxError("::= expected.");
				}

				if (s is null)
					break;
			}

			Asn1Module Body = await this.ParseModule();

			return new Asn1Definitions(Identifier, Oid, Tags, Abstract, Body, this);
		}

		private async Task<Asn1Module> ParseModule()
		{
			string s;

			this.AssertNextToken("BEGIN");

			List<Asn1Import> Imports = null;
			List<string> Exports = null;

			do
			{
				s = this.PeekNextToken();

				if (s == "IMPORTS")
				{
					this.pos += 7;

					if (Imports is null)
						Imports = new List<Asn1Import>();

					List<string> Identifiers = new List<string>();

					do
					{
						string Identifier = this.ParseIdentifier();
						string ModuleRef;

						s = this.PeekNextToken();

						if (s == "FROM")
						{
							this.pos += 4;
							Identifiers.Add(Identifier);
							ModuleRef = this.ParseTypeNameIdentifier();

							Imports.Add(new Asn1Import(Identifiers.ToArray(), ModuleRef, this));
							Identifiers.Clear();

							s = this.PeekNextToken();
							if (s != ";")
								continue;
						}
						else if (s == ".")
						{
							this.pos++;
							ModuleRef = Identifier;
							Identifier = this.ParseIdentifier();
							Imports.Add(new Asn1Import(new string[] { ModuleRef }, Identifier, this));
							s = this.PeekNextToken();
						}
						else
							Identifiers.Add(Identifier);

						if (s == ",")
						{
							this.pos++;
							continue;
						}
						else if (s == ";")
						{
							this.pos++;
							break;
						}
						else
							throw this.SyntaxError("Unexpected token.");
					}
					while (true);

					if (Identifiers.Count > 0)
						Imports.Add(new Asn1Import(Identifiers.ToArray(), string.Empty, this));

					foreach (Asn1Import Import in Imports)
					{
						Asn1Document Doc = await Import.LoadDocument();

						foreach (string Identifier in Import.Identifiers)
						{
							if (!Doc.namedNodes.TryGetValue(Identifier, out Asn1Node ImportedNode))
								throw this.SyntaxError(Identifier + " not found in " + Import.Module);

							this.namedNodes[Identifier] = ImportedNode;

							if (Doc.aliases.TryGetValue(Identifier, out Asn1TypeDefinition TypeDef))
								this.aliases[Identifier] = TypeDef;

							if (Doc.values.TryGetValue(Identifier, out Asn1FieldValueDefinition ValueDef))
								this.values[Identifier] = ValueDef;
						}
					}
				}
				else if (s == "EXPORTS")
				{
					this.pos += 7;

					if (Exports is null)
						Exports = new List<string>();

					do
					{
						string Identifier = this.ParseIdentifier();
						Exports.Add(Identifier);

						s = this.PeekNextToken();

						if (s == ",")
						{
							this.pos++;
							continue;
						}
						else if (s == ";")
						{
							this.pos++;
							break;
						}
						else
							throw this.SyntaxError("; expected");
					}
					while (true);
				}
				else
					break;
			}
			while (true);

			List<Asn1Node> Items = new List<Asn1Node>();

			while ((s = this.PeekNextToken()) != "END")
			{
				if (string.IsNullOrEmpty(s))
					throw this.SyntaxError("END expected.");

				Asn1Node Node = this.ParseStatement();
				Items.Add(Node);

				if (Node is INamedNode NamedNode)
					this.namedNodes[NamedNode.Name] = Node;
			}

			this.pos += 3;

			return new Asn1Module(Imports?.ToArray(), Exports?.ToArray(), Items.ToArray());
		}

		private Asn1Node ParseStatement()
		{
			string s = this.PeekNextToken();
			if (string.IsNullOrEmpty(s))
				throw this.SyntaxError("Unexpected end of file.");

			char ch = s[0];
			string s2;

			if (char.IsLetter(ch))
			{
				int PosBak = this.pos;

				this.pos += s.Length;
				s2 = this.PeekNextToken();

				if (s2 == "::=")
				{
					if (!char.IsUpper(ch))   // !Type = XML notation
						throw this.SyntaxError("XML notation not supported.");

					this.pos += s2.Length;

					s2 = this.PeekNextToken();
				}
				else if (s2 == "MACRO")
				{
					this.pos += 5;
					this.AssertNextToken("::=");
					this.AssertNextToken("BEGIN");
					this.AssertNextToken("TYPE");
					this.AssertNextToken("NOTATION");
					this.AssertNextToken("::=");

					UserDefinedItem TypeNotation = this.ParseUserDefinedOptions("VALUE");

					this.AssertNextToken("VALUE");
					this.AssertNextToken("NOTATION");
					this.AssertNextToken("::=");

					UserDefinedItem ValueNotation = this.ParseUserDefinedOptions("END");
					List<SupportingSyntax> SupportingSyntax = new List<SupportingSyntax>();

					while (this.PeekNextToken() != "END")
					{
						string Name = this.ParseIdentifier();
						this.AssertNextToken("::=");
						SupportingSyntax.Add(new SupportingSyntax(Name, this.ParseUserDefinedOptions("END")));
					}

					this.AssertNextToken("END");

					return new Asn1Macro(s, TypeNotation, ValueNotation, SupportingSyntax.ToArray(), this);
				}
				else if (this.namedNodes.TryGetValue(s2, out Asn1Node Node) &&
					Node is Asn1Macro Macro)
				{
					this.pos += s2.Length;

					Asn1Value Value = Macro.ParseValue(this);
					Asn1FieldValueDefinition ValueDef = new Asn1FieldValueDefinition(s, Macro.GetValueType(), Value, this);

					this.values[s] = ValueDef;

					return ValueDef;
				}
				else
				{
					if (char.IsUpper(ch))   // Type or macro
					{
						s2 = s;
						s = "unnamed" + (this.unnamedIndex++).ToString();
						ch = 'u';
						this.pos = PosBak;
					}
				}

				int? Tag = null;
				TagClass? Class = null;

				if (s2 == "[")
				{
					this.pos++;
					s2 = this.NextToken();

					if (s2 == "TAG")
					{
						this.AssertNextToken(":");
						s2 = this.NextToken();
					}

					switch (s2)
					{
						case "APPLICATION":
							Class = TagClass.Application;
							s2 = this.NextToken();
							break;

						case "PRIVATE":
							Class = TagClass.Private;
							s2 = this.NextToken();
							break;

						case "UNIVERSAL":
							Class = TagClass.Universal;
							s2 = this.NextToken();
							break;
					}

					if (!int.TryParse(s2, out int i))
						throw this.SyntaxError("Tag expected.");

					if (Class.HasValue)
						i |= ((int)Class) << 6;

					Tag = i;

					this.AssertNextToken("]");

					s2 = this.PeekNextToken();
				}

				if (char.IsUpper(ch))   // Type
				{
					Asn1Type Definition;

					if (this.namedNodes.TryGetValue(s2, out Asn1Node Node) &&
						Node is Asn1Macro Macro)
					{
						this.pos += s2.Length;
						Definition = Macro.ParseType(this);
					}
					else
						Definition = this.ParseType(s, true);

					Asn1TypeDefinition TypeDef = new Asn1TypeDefinition(s, Tag, Definition);

					if (!Definition.ConstructedType)
						this.aliases[s] = TypeDef;

					return TypeDef;
				}
				else                    // name
				{
					if (!IsTypeIdentifier(s2))
						throw this.SyntaxError("Type name expected.");

					Asn1Type Type = this.ParseType(s, false);

					if (this.PeekNextToken() == "::=")
					{
						this.pos += 3;
						Asn1Value Value = this.ParseValue();
						Asn1FieldValueDefinition ValueDef = new Asn1FieldValueDefinition(s, Type, Value, this);

						this.values[s] = ValueDef;

						return ValueDef;
					}
					else
						return new Asn1FieldDefinition(s, Tag, Type);
				}
			}
			else if (s == "...")
			{
				this.pos += 3;
				return new Asn1Extension();
			}
			else
				throw this.SyntaxError("Identifier expected.");
		}

		private UserDefinedItem ParseUserDefinedOptions(string EndKeyWord)
		{
			UserDefinedItem Item = this.ParseUserDefinedOption(EndKeyWord);
			List<UserDefinedItem> Options = null;

			while (this.PeekNextToken() == "|")
			{
				this.pos++;

				if (Options is null)
					Options = new List<UserDefinedItem>();

				Options.Add(Item);
				Item = this.ParseUserDefinedOption(EndKeyWord);
			}

			if (Options is null)
				return Item;
			else
			{
				Options.Add(Item);
				return new UserDefinedOptions(Options.ToArray());
			}
		}

		private UserDefinedItem ParseUserDefinedOption(string EndKeyWord)
		{
			UserDefinedItem Item = null;
			List<UserDefinedItem> Items = new List<UserDefinedItem>();
			string s;
			int PosBak = this.pos;

			while ((s = this.PeekNextToken()) != EndKeyWord && s != "::=" && s != "|")
			{
				if (!(Item is null))
				{
					if (Items is null)
						Items = new List<UserDefinedItem>();

					Items.Add(Item);
				}

				PosBak = this.pos;
				Item = this.ParseUserDefinedItem();
			}

			if (Item is null)
				throw this.SyntaxError("Items expected.");

			if (s == "::=")
			{
				if (Items is null)
					throw this.SyntaxError("Items expected.");

				this.pos = PosBak;
				if (Items.Count == 1)
					return Items[0];
			}
			else if (Items is null)
				return Item;
			else
				Items.Add(Item);

			return new UserDefinedOption(Items.ToArray());
		}

		private UserDefinedItem ParseUserDefinedItem()
		{
			string s = this.PeekNextToken();

			if (s == "\"")
			{
				if (!(this.ParseValue() is Asn1StringValue Label))
					throw this.SyntaxError("String label expected.");

				return new UserDefinedLiteral(Label.Value);
			}

			if (!IsIdentifier(s))
				throw this.SyntaxError("Identifier or literal expected.");

			this.pos += s.Length;

			if (this.PeekNextToken() == "(")
			{
				this.pos++;

				string Name = this.ParseIdentifier();
				if (this.PeekNextToken() == ")")
				{
					this.pos++;
					return new UserDefinedSpecifiedPart(s, string.Empty, new Asn1TypeReference(Name, this));
				}
				else
				{
					Asn1Type Type = this.ParseType(Name, false);

					this.AssertNextToken(")");

					return new UserDefinedSpecifiedPart(s, Name, Type);
				}
			}
			else
				return new UserDefinedPart(s);
		}

		private Asn1Restriction ParseRestriction()
		{
			this.AssertNextToken("(");
			Asn1Restriction Result = this.ParseOrs();
			this.AssertNextToken(")");

			return Result;
		}

		private Asn1Restriction ParseOrs()
		{
			Asn1Restriction Result = this.ParseAnds();

			string s = this.PeekNextToken();

			while (s == "|")
			{
				this.pos++;
				Result = new Asn1Or(Result, this.ParseAnds());
				s = this.PeekNextToken();
			}

			return Result;
		}

		private Asn1Restriction ParseAnds()
		{
			Asn1Restriction Result = this.ParseRestrictionRule();

			string s = this.PeekNextToken();

			while (s == "^")
			{
				this.pos++;
				Result = new Asn1And(Result, this.ParseRestrictionRule());
				s = this.PeekNextToken();
			}

			return Result;
		}

		private Asn1Restriction ParseRestrictionRule()
		{
			string s = this.PeekNextToken();

			switch (s)
			{
				case "(":
					this.pos++;

					Asn1Restriction Result = this.ParseOrs();
					this.AssertNextToken(")");

					return Result;

				case "SIZE":
					this.pos += 4;
					return new Asn1Size(this.ParseSet());

				case "PATTERN":
					this.pos += 7;
					return new Asn1Pattern(this.ParseValue());

				case "FROM":
					this.pos += 4;
					return new Asn1From(this.ParseSet());

				case "CONTAINING":
					this.pos += 10;
					return new Asn1Containing(this.ParseValue());

				case "ENCODED":
					this.pos += 7;
					this.AssertNextToken("BY");

					return new Asn1EncodedBy(this.ParseValue());

				case "WITH":
					this.pos += 4;
					this.AssertNextToken("COMPONENTS");

					return new Asn1WithComponents(this.ParseValue());

				default:
					return new Asn1InSet(this.ParseUnions());
			}
		}

		private Asn1Values ParseSet()
		{
			this.AssertNextToken("(");
			Asn1Values Result = this.ParseUnions();
			this.AssertNextToken(")");

			return Result;
		}

		private Asn1Values ParseUnions()
		{
			Asn1Values Result = this.ParseIntersections();

			string s = this.PeekNextToken();

			while (s == "|" || s == "UNION")
			{
				this.pos += s.Length;
				Result = new Asn1Union(Result, this.ParseIntersections());
				s = this.PeekNextToken();
			}

			return Result;
		}

		private Asn1Values ParseIntersections()
		{
			Asn1Values Result = this.ParseIntervals();

			string s = this.PeekNextToken();

			while (s == "^" || s == "INTERSECTION")
			{
				this.pos += s.Length;
				Result = new Asn1Union(Result, this.ParseIntervals());
				s = this.PeekNextToken();
			}

			return Result;
		}

		private Asn1Values ParseIntervals()
		{
			string s = this.PeekNextToken();

			if (s == "(")
			{
				this.pos++;

				Asn1Values Result = this.ParseUnions();
				this.AssertNextToken(")");

				return Result;
			}
			else if (s == "ALL")
			{
				this.pos += 3;
				return new Asn1All();
			}
			else
			{
				Asn1Value Value = this.ParseValue();

				if (this.PeekNextToken() == "..")
				{
					this.pos += 2;
					Asn1Value Value2 = this.ParseValue();

					return new Asn1Interval(Value, Value2);
				}
				else
					return new Asn1Element(Value);
			}
		}

		private string ParseIdentifier()
		{
			string s = this.NextToken();

			if (!IsIdentifier(s))
				throw this.SyntaxError("Identifier expected.");

			return s;
		}

		private string ParseTypeNameIdentifier()
		{
			string s = this.NextToken();

			if (!IsTypeIdentifier(s))
				throw this.SyntaxError("Type name identifier expected.");

			return s;
		}

		private static bool IsIdentifier(string s)
		{
			return !string.IsNullOrEmpty(s) && char.IsLetter(s[0]);
		}

		private static bool IsTypeIdentifier(string s)
		{
			char ch;
			return !string.IsNullOrEmpty(s) && char.IsLetter(ch = s[0]) && char.IsUpper(ch);
		}

		private static bool IsFieldIdentifier(string s)
		{
			char ch;
			return !string.IsNullOrEmpty(s) && char.IsLetter(ch = s[0]) && char.IsLower(ch);
		}

		private Asn1Oid ParseOid()
		{
			Asn1Node[] Values = this.ParseValues();
			return new Asn1Oid(Values);
		}

		internal Asn1Type ParseType(string Name, bool TypeDef)
		{
			bool Implicit = false;

			switch (this.PeekNextToken())
			{
				case "IMPLICIT":
					this.pos += 8;
					Implicit = true;
					break;
			}

			Asn1Type Result = this.ParseDataType(Name, TypeDef);
			Result.Implicit = Implicit;

			string s2;

			while (true)
			{
				s2 = this.PeekNextToken();

				switch (s2)
				{
					case "(":
						Result.Restriction = this.ParseRestriction();
						break;

					case "{":
						this.pos++;

						List<Asn1NamedValue> NamedOptions = new List<Asn1NamedValue>();

						while (true)
						{
							s2 = this.NextToken();
							if (!IsFieldIdentifier(s2))
								throw this.SyntaxError("Value name expected.");

							if (this.PeekNextToken() == "(")
							{
								this.pos++;

								NamedOptions.Add(new Asn1NamedValue(s2, this.ParseValue(), this));

								if (this.NextToken() != ")")
									throw this.SyntaxError(") expected");
							}
							else
								NamedOptions.Add(new Asn1NamedValue(s2, null, this));

							s2 = this.NextToken();

							if (s2 == ",")
								continue;
							else if (s2 == "}")
							{
								Result.NamedOptions = NamedOptions.ToArray();
								break;
							}
							else
								throw this.SyntaxError("Unexpected token.");
						}
						break;

					case "OPTIONAL":
						this.pos += 8;
						Result.Optional = true;
						break;

					case "PRESENT":
						this.pos += 7;
						Result.Present = true;
						break;

					case "ABSENT":
						this.pos += 6;
						Result.Absent = true;
						break;

					case "DEFAULT":
						this.pos += 7;
						Result.Optional = true;
						Result.Default = this.ParseValue();
						break;

					case "UNIQUE":
						this.pos += 6;
						Result.Unique = true;
						break;

					default:
						return Result;
				}
			}
		}

		private Asn1Type ParseDataType(string Name, bool TypeDef)
		{
			string s = this.NextToken();
			if (string.IsNullOrEmpty(s))
				throw this.SyntaxError("Unexpected end of file.");

			switch (s)
			{
				case "ANY":
					return new Asn1Any();

				case "BIT":
					this.AssertNextToken("STRING");
					return new Asn1BitString();

				case "BMPString":
					return new Asn1BmpString();

				case "BOOLEAN":
					return new Asn1Boolean();

				case "CHARACTER":
					return new Asn1Character();

				case "CHOICE":
					s = this.PeekNextToken();

					if (s == "{")
					{
						Asn1Node[] Nodes = this.ParseList();

						foreach (Asn1Node Node in Nodes)
						{
							if (Node is Asn1FieldDefinition FieldDef && !this.namedNodes.ContainsKey(FieldDef.Name))
								this.namedNodes[FieldDef.Name] = FieldDef;
						}

						return new Asn1Choice(Name, TypeDef, Nodes);
					}
					else
						throw this.SyntaxError("{ expected.");

				case "DATE":
					return new Asn1Date();

				case "DATE-TIME":
					return new Asn1DateTime();

				case "DURATION":
					return new Asn1Duration();

				case "ENUMERATED":
					s = this.PeekNextToken();

					if (s == "{")
					{
						Asn1Node[] Nodes = this.ParseValues();
						return new Asn1Enumeration(Name, TypeDef, Nodes);
					}
					else
						throw this.SyntaxError("{ expected.");

				case "GeneralizedTime":
					return new Asn1GeneralizedTime();

				case "GeneralString":
					return new Asn1GeneralString();

				case "GraphicString":
					return new Asn1GraphicString();

				case "IA5String":
					return new Asn1Ia5String();

				case "INTEGER":
					return new Asn1Integer();

				case "ISO646String":
					return new Asn1Iso646String();

				case "NULL":
					return new Asn1Null();

				case "NumericString":
					return new Asn1NumericString();

				case "OBJECT":
					this.AssertNextToken("IDENTIFIER");
					return new Asn1ObjectIdentifier(false);

				case "OCTET":
					this.AssertNextToken("STRING");
					return new Asn1OctetString();

				case "PrintableString":
					return new Asn1PrintableString();

				case "REAL":
					return new Asn1Real();

				case "RELATIVE":
					this.AssertNextToken("OID");
					return new Asn1ObjectIdentifier(true);

				case "RELATIVE-OID":
					return new Asn1ObjectIdentifier(true);

				case "SET":
					s = this.PeekNextToken();

					if (s == "{")
					{
						Asn1Node[] Nodes = this.ParseList();
						return new Asn1Set(Name, TypeDef, Nodes);
					}
					else if (s == "(")
					{
						this.pos++;

						Asn1Values Size = null;

						while (true)
						{
							s = this.PeekNextToken();

							if (s == "SIZE")
							{
								if (!(Size is null))
									throw this.SyntaxError("SIZE already specified.");

								this.pos += 4;
								Size = this.ParseSet();
							}
							else if (s == ")")
							{
								this.pos++;
								break;
							}
							else
								throw this.SyntaxError("Unexpected token.");
						}

						this.AssertNextToken("OF");

						if (Size is null)
							throw this.SyntaxError("SIZE expected.");

						s = this.ParseTypeNameIdentifier();

						return new Asn1SetOf(Size, s);
					}
					else
						throw this.SyntaxError("{ expected.");

				case "SEQUENCE":
					s = this.PeekNextToken();

					if (s == "{")
					{
						Asn1Node[] Nodes = this.ParseList();
						return new Asn1Sequence(Name, TypeDef, Nodes);
					}
					else
					{
						Asn1Values Size = null;

						if (s == "(")
						{
							this.pos++;

							while (true)
							{
								s = this.PeekNextToken();

								if (s == "SIZE")
								{
									if (!(Size is null))
										throw this.SyntaxError("SIZE already specified.");

									this.pos += 4;
									Size = this.ParseSet();
								}
								else if (s == ")")
								{
									this.pos++;
									break;
								}
								else
									throw this.SyntaxError("Unexpected token.");
							}
						}

						if (this.NextToken() != "OF")
							throw this.SyntaxError("Unexpected token.");

						return new Asn1SequenceOf(Name, TypeDef, Size, this.ParseType(Name, TypeDef));
					}

				case "T61String":
					return new Asn1T61String();

				case "TeletexString":
					return new Asn1TeletexString();

				case "TIME-OF-DAY":
					return new Asn1TimeOfDay();

				case "UniversalString":
					return new Asn1UniversalString();

				case "UTCTime":
					return new Asn1UtcTime();

				case "UTF8String":
					return new Asn1Utf8String();

				case "VideotexString":
					return new Asn1VideotexString();

				case "VisibleString":
					return new Asn1VisibleString();

				case "ObjectDescriptor":
				case "EXTERNAL":
				case "EMBEDDED":
				case "CLASS":
				case "COMPONENTS":
				case "INSTANCE":
				case "OID-IRI":
					throw this.SyntaxError("Token not implemented.");

				default:
					if (char.IsUpper(s[0]))
						return new Asn1TypeReference(s, this);
					else
						throw this.SyntaxError("Type name expected.");
			}
		}

		private Asn1Node[] ParseList()
		{
			this.AssertNextToken("{");

			List<Asn1Node> Items = new List<Asn1Node>();

			while (true)
			{
				Items.Add(this.ParseStatement());

				switch (this.PeekNextToken())
				{
					case ",":
						this.pos++;
						continue;

					case "}":
						this.pos++;
						return Items.ToArray();

					default:
						throw this.SyntaxError(", or } expected.");
				}
			}
		}

		private Asn1Node[] ParseValues()
		{
			this.AssertNextToken("{");

			List<Asn1Node> Items = new List<Asn1Node>();

			while (true)
			{
				Items.Add(this.ParseValue());

				switch (this.PeekNextToken())
				{
					case ",":
						this.pos++;
						continue;

					case "}":
						this.pos++;
						return Items.ToArray();

					case "(":
						this.pos++;
						Asn1Value Value = this.ParseValue();
						this.AssertNextToken(")");

						int LastIndex = Items.Count - 1;

						if (Items[LastIndex] is Asn1ValueReference Ref)
							Items[LastIndex] = new Asn1NamedValue(Ref.Identifier, Value, this);
						else
							throw this.SyntaxError("Invalid value reference.");
						break;

					default:
						throw this.SyntaxError(", or } expected.");
				}
			}
		}

		internal Asn1Value ParseValue()
		{
			return this.ParseValue(true);
		}

		private Asn1Value ParseValue(bool AllowNamed)
		{
			string s = this.PeekNextToken();
			if (string.IsNullOrEmpty(s))
				throw this.SyntaxError("Expected value.");

			switch (s)
			{
				case "FALSE":
					this.pos += 5;
					return new Asn1BooleanValue(false);

				case "MAX":
					this.pos += 3;
					return new Asn1Max();

				case "MIN":
					this.pos += 3;
					return new Asn1Min();

				case "TRUE":
					this.pos += 4;
					return new Asn1BooleanValue(true);

				case "INF":
					this.pos += 3;
					return new Asn1FloatingPointValue(double.PositiveInfinity);

				case "NaN":
					this.pos += 3;
					return new Asn1FloatingPointValue(double.NaN);

				case "...":
					this.pos += 3;
					return new Asn1Extension();

				case "\"":
					int Start = ++this.pos;
					char ch;

					while (this.pos < this.len && this.text[this.pos] != '"')
						this.pos++;

					if (this.pos >= this.len)
						throw this.SyntaxError("\" expected.");

					s = this.text.Substring(Start, this.pos - Start);
					this.pos++;

					return new Asn1StringValue(s);

				case "'":
					Start = ++this.pos;

					while (this.pos < this.len && this.text[this.pos] != '\'')
						this.pos++;

					if (this.pos >= this.len)
						throw this.SyntaxError("' expected.");

					s = this.text.Substring(Start, this.pos - Start);
					this.pos++;

					switch (this.NextToken())
					{
						case "H":
						case "h":
							this.pos++;
							if (long.TryParse(s, NumberStyles.HexNumber, null, out long l))
								return new Asn1IntegerValue(l);
							else
								throw this.SyntaxError("Invalid hexadecimal string.");

						case "D":
						case "d":
							this.pos++;
							if (long.TryParse(s, out l))
								return new Asn1IntegerValue(l);
							else
								throw this.SyntaxError("Invalid decimal string.");

						case "B":
						case "b":
							this.pos++;
							if (TryParseBinary(s, out l))
								return new Asn1IntegerValue(l);
							else
								throw this.SyntaxError("Invalid binary string.");

						case "O":
						case "o":
							this.pos++;
							if (TryParseOctal(s, out l))
								return new Asn1IntegerValue(l);
							else
								throw this.SyntaxError("Invalid octal string.");

						default:
							throw this.SyntaxError("Unexpected token.");
					}

				case "{":
					this.pos++;

					List<Asn1Value> Items = new List<Asn1Value>();
					bool Oid = false;

					while (true)
					{
						Asn1Value Value = this.ParseValue();

						s = this.PeekNextToken();

						switch (s)
						{
							case ",":
								this.pos++;
								Items.Add(Value);
								break;

							case "}":
								this.pos++;
								Items.Add(Value);

								if (Oid)
									return new Asn1Oid(Items.ToArray());
								else
									return new Asn1Array(Items.ToArray());

							default:
								if (Items.Count == 0)
									Oid = true;

								if (Oid)
									Items.Add(Value);
								else
									throw this.SyntaxError("Unexpected token.");
								break;
						}
					}

				default:
					if (char.IsLetter(s[0]))
					{
						this.pos += s.Length;

						switch (this.PeekNextToken())
						{
							case ":":
								if (!AllowNamed)
									throw this.SyntaxError("Value expected.");

								this.pos++;
								Asn1Value Value = this.ParseValue(false);
								return new Asn1NamedValue(s, Value, this);

							case "(":
								Asn1Restriction Restriction = this.ParseRestriction();

								if (Restriction is Asn1InSet Set && Set.Set is Asn1Element Element)
									return new Asn1NamedValue(s, Element.Element, this);
								else
									return new Asn1RestrictedValueReference(s, Restriction, this);

							default:
								if (!char.IsUpper(s[0]))
									return new Asn1ValueReference(s, this);
								else
									throw this.SyntaxError("Type references not permitted here.");
						}
					}
					else
					{
						Start = this.pos;

						bool Sign = s.StartsWith("-");
						if (Sign)
						{
							s = s.Substring(1);
							this.pos++;
						}

						if (ulong.TryParse(s, out ulong l))
						{
							this.pos += s.Length;

							ch = this.PeekNextChar();

							if ((ch == '.' && this.pos < this.lenm1 && this.text[this.pos + 1] != '.') ||
								ch == 'e' || ch == 'E')
							{
								int? DecPos = null;

								if (ch == '.')
								{
									DecPos = this.pos++;
									while (this.pos < this.len && char.IsDigit(ch = this.text[this.pos]))
										this.pos++;
								}

								if (ch == 'e' || ch == 'E')
								{
									this.pos++;

									if ((ch = this.PeekNextChar()) == '-' || ch == '+')
										this.pos++;

									while (this.pos < this.len && char.IsDigit(this.text[this.pos]))
										this.pos++;
								}

								s = this.text.Substring(Start, this.pos - Start);

								string s2 = NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
								if (DecPos.HasValue && s2 != ".")
									s = s.Replace(".", s2);

								if (!double.TryParse(s, out double d))
									throw this.SyntaxError("Invalid floating-point number.");

								return new Asn1FloatingPointValue(d);
							}

							if (l <= long.MaxValue)
								return new Asn1IntegerValue(Sign ? -(long)l : (long)l);
							else if (Sign)
								throw this.SyntaxError("Number does not fit into a 64-bit signed integer.");
							else
								return new Asn1UnsignedIntegerValue(l);
						}
					}
					break;
			}

			throw this.SyntaxError("Value expected.");
		}

		private static bool TryParseBinary(string s, out long l)
		{
			l = 0;

			foreach (char ch in s)
			{
				if (ch < '0' || ch > '1')
					return false;

				long l2 = l;
				l <<= 1;
				if (l < l2)
					return false;

				l |= (byte)(ch - '0');
			}

			return true;
		}

		private static bool TryParseOctal(string s, out long l)
		{
			l = 0;

			foreach (char ch in s)
			{
				if (ch < '0' || ch > '7')
					return false;

				long l2 = l;
				l <<= 3;
				if (l < l2)
					return false;

				l |= (byte)(ch - '0');
			}

			return true;
		}

		/// <summary>
		/// Exports ASN.1 schemas to C#
		/// </summary>
		/// <param name="Settings">C# export settings.</param>
		/// <returns>C# code</returns>
		public string ExportCSharp(CSharpExportSettings Settings)
		{
			StringBuilder Output = new StringBuilder();
			this.ExportCSharp(Output, Settings);
			return Output.ToString();
		}

		/// <summary>
		/// Exports ASN.1 schemas to C#
		/// </summary>
		/// <param name="Output">C# Output.</param>
		/// <param name="Settings">C# export settings.</param>
		public void ExportCSharp(StringBuilder Output, CSharpExportSettings Settings)
		{
			CSharpExportState State = new CSharpExportState(Settings);

			Output.AppendLine("using System;");
			Output.AppendLine("using System.Text;");
			Output.AppendLine("using System.Collections.Generic;");
			Output.AppendLine("using Waher.Content;");
			Output.AppendLine("using Waher.Content.Asn1;");

			this.root?.ExportCSharp(Output, State, 0, CSharpExportPass.Explicit);
		}
	}
}
