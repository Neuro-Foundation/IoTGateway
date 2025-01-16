﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Waher.Persistence.MongoDB.Serialization
{
	/// <summary>
	/// Provides a generic object serializer.
	/// </summary>
	public class GenericObjectSerializer : ObjectSerializer
	{
		private readonly bool returnTypedObjects;

		/// <summary>
		/// Provides a generic object serializer.
		/// </summary>
		/// <param name="Provider">Database provider.</param>
		public GenericObjectSerializer(MongoDBProvider Provider)
			: this(Provider, false)
		{
		}

		/// <summary>
		/// Provides a generic object serializer.
		/// </summary>
		/// <param name="Provider">Database provider.</param>
		/// <param name="ReturnTypedObjects">If typed objects are to be returned.</param>
		public GenericObjectSerializer(MongoDBProvider Provider, bool ReturnTypedObjects)
			: base(typeof(GenericObject), Provider, true)
		{
			this.returnTypedObjects = ReturnTypedObjects;
		}

		/// <summary>
		/// Deserializes an object from a binary source.
		/// </summary>
		/// <param name="Reader">Binary deserializer.</param>
		/// <param name="DataType">Optional datatype. If not provided, will be read from the binary source.</param>
		/// <param name="Embedded">If the object is embedded into another.</param>
		/// <returns>Deserialized object.</returns>
		public override object Deserialize(IBsonReader Reader, BsonType? DataType, bool Embedded)
		{
			BsonReaderBookmark Bookmark = Reader.GetBookmark();
			BsonType? DataTypeBak = DataType;

			if (!DataType.HasValue)
				DataType = Reader.ReadBsonType();

			switch (DataType.Value)
			{
				case BsonType.Document:
					break;

				case BsonType.Boolean:
					return Reader.ReadBoolean();

				case BsonType.Int32:
					return Reader.ReadInt32();

				case BsonType.Int64:
					return Reader.ReadInt64();

				case BsonType.Decimal128:
					return (decimal)Reader.ReadDecimal128();

				case BsonType.Double:
					return Reader.ReadDouble();

				case BsonType.DateTime:
					return ObjectSerializer.UnixEpoch.AddMilliseconds(Reader.ReadDateTime());

				case BsonType.String:
				case BsonType.Symbol:
				case BsonType.JavaScript:
				case BsonType.JavaScriptWithScope:
					return Reader.ReadString();

				case BsonType.Binary:
					return Reader.ReadBytes();

				case BsonType.Null:
					Reader.ReadNull();
					return null;

				default:
					throw new Exception("Object or value expected.");
			}

			LinkedList<KeyValuePair<string, object>> Properties = new LinkedList<KeyValuePair<string, object>>();
			LinkedList<KeyValuePair<string, object>> LowerCase = null;
			string TypeName = string.Empty;
			Guid ObjectId = Guid.Empty;
			string CollectionName = string.Empty;
			string FieldName;
			BsonType ValueType;
			object Value;

			Reader.ReadStartDocument();

			while (Reader.State == BsonReaderState.Type)
			{
				ValueType = Reader.ReadBsonType();
				if (ValueType == BsonType.EndOfDocument)
					break;

				FieldName = Reader.ReadName();

				switch (ValueType)
				{
					case BsonType.Array:
						Value = GeneratedObjectSerializerBase.ReadArray(null, this.Provider, Reader, ValueType);
						break;

					case BsonType.Binary:
						Value = Reader.ReadBytes();
						break;

					case BsonType.Boolean:
						Value = Reader.ReadBoolean();
						break;

					case BsonType.DateTime:
						Value = ObjectSerializer.UnixEpoch.AddMilliseconds(Reader.ReadDateTime());
						break;

					case BsonType.Decimal128:
						Value = (decimal)Reader.ReadDecimal128();
						break;

					case BsonType.Document:
						Value = this.Deserialize(Reader, ValueType, true);
						break;

					case BsonType.Double:
						Value = Reader.ReadDouble();
						break;

					case BsonType.Int32:
						Value = Reader.ReadInt32();
						break;

					case BsonType.Int64:
						Value = Reader.ReadInt64();
						break;

					case BsonType.JavaScript:
						Value = Reader.ReadJavaScript();
						break;

					case BsonType.JavaScriptWithScope:
						Value = Reader.ReadJavaScriptWithScope();
						break;

					case BsonType.Null:
						Value = null;
						Reader.ReadNull();
						break;

					case BsonType.ObjectId:
						Value = Reader.ReadObjectId();
						break;

					case BsonType.String:
						Value = Reader.ReadString();
						break;

					case BsonType.Symbol:
						Value = Reader.ReadSymbol();
						break;

					default:
						throw new Exception("Unrecognized data type: " + ValueType.ToString());
				}

				switch (FieldName)
				{
					case "_id":
						if (Value is Guid Guid)
							ObjectId = Guid;
						else if (Value is string s)
							ObjectId = new Guid(s);
						else if (Value is byte[] A)
							ObjectId = new Guid(A);
						else if (Value is ObjectId ObjId)
							ObjectId = GeneratedObjectSerializerBase.ObjectIdToGuid(ObjId);
						else
							throw new Exception("Unrecognized Object ID type: " + Value.GetType().FullName);
						break;

					case "_type":
						TypeName = Value?.ToString();

						if (this.returnTypedObjects && !string.IsNullOrEmpty(TypeName))
						{
							Type DesiredType = Types.GetType(TypeName)
								?? typeof(GenericObject);

							if (DesiredType != typeof(GenericObject))
							{
								IObjectSerializer Serializer2 = this.provider.GetObjectSerializer(DesiredType);
								Reader.ReturnToBookmark(Bookmark);
								return Serializer2.Deserialize(Reader, DataTypeBak, Embedded);
							}
						}
						break;

					case "_collection":
						CollectionName = Value?.ToString();
						break;

					default:
						if (FieldName.EndsWith("_L"))
						{
							string s = FieldName.Substring(0, FieldName.Length - 2);
							bool Ignore = false;

							foreach (KeyValuePair<string, object> P in Properties)
							{
								if (P.Key == s)
								{
									Ignore = true;
									break;
								}
							}

							if (!Ignore)
							{
								if (LowerCase is null)
									LowerCase = new LinkedList<KeyValuePair<string, object>>();

								LowerCase.AddLast(new KeyValuePair<string, object>(s, Value));
							}
						}
						else
							Properties.AddLast(new KeyValuePair<string, object>(FieldName, Value));
						break;
				}
			}

			if (!(LowerCase is null))
			{
				foreach (KeyValuePair<string, object> P in LowerCase)
				{
					bool Ignore = false;

					foreach (KeyValuePair<string, object> P2 in Properties)
					{
						if (P2.Key == P.Key)
						{
							Ignore = true;
							break;
						}
					}

					if (!Ignore)
						Properties.AddLast(new KeyValuePair<string, object>(P.Key + "_L", P.Value));
				}
			}

			Reader.ReadEndDocument();

			return new GenericObject(CollectionName, TypeName, ObjectId, Properties);
		}

		/// <summary>
		/// Serializes an object to a binary destination.
		/// </summary>
		/// <param name="Writer">Binary destination.</param>
		/// <param name="WriteTypeCode">If a type code is to be output.</param>
		/// <param name="Embedded">If the object is embedded into another.</param>
		/// <param name="Value">The actual object to serialize.</param>
		public override void Serialize(IBsonWriter Writer, bool WriteTypeCode, bool Embedded, object Value)
		{
			if (Value is null)
			{
				if (!WriteTypeCode)
					throw new NullReferenceException("Value cannot be null.");

				Writer.WriteNull();
			}
			else if (Value is GenericObject TypedValue)
			{
				IObjectSerializer Serializer;
				object Obj;

				Writer.WriteStartDocument();

				if (!Embedded && TypedValue.ObjectId != Guid.Empty)
				{
					Writer.WriteName("_id");
					Writer.WriteObjectId(GeneratedObjectSerializerBase.GuidToObjectId(TypedValue.ObjectId));
				}

				if (!string.IsNullOrEmpty(TypedValue.TypeName))
				{
					Writer.WriteName("_type");
					Writer.WriteString(TypedValue.TypeName);
				}

				if (Embedded && !string.IsNullOrEmpty(TypedValue.CollectionName))
				{
					Writer.WriteName("_collection");
					Writer.WriteString(TypedValue.CollectionName);
				}

				foreach (KeyValuePair<string, object> Field in TypedValue)
				{
					Writer.WriteName(Field.Key);

					Obj = Field.Value;
					if (Obj is null)
						Writer.WriteNull();
					else
					{
						if (Obj is GenericObject)
							this.Serialize(Writer, true, true, Obj);
						else if (Obj is CaseInsensitiveString cis)
						{
							Writer.WriteString(cis.Value);
							Writer.WriteName(Field.Key + "_L");
							Writer.WriteString(cis.LowerCase);
						}
						else
						{
							Serializer = this.Provider.GetObjectSerializer(Obj.GetType());
							Serializer.Serialize(Writer, true, true, Obj);
						}
					}
				}

				Writer.WriteEndDocument();
			}
			else
			{
				IObjectSerializer Serializer = this.Provider.GetObjectSerializer(Value.GetType());
				Serializer.Serialize(Writer, WriteTypeCode, Embedded, Value);
			}
		}

		/// <summary>
		/// Gets the value of a field or property of an object, given its name.
		/// </summary>
		/// <param name="FieldName">Name of field or property.</param>
		/// <param name="Object">Object.</param>
		/// <param name="Value">Corresponding field or property value, if found, or null otherwise.</param>
		/// <returns>If the corresponding field or property was found.</returns>
		public override bool TryGetFieldValue(string FieldName, object Object, out object Value)
		{
			GenericObject Obj = (GenericObject)Object;
			return Obj.TryGetFieldValue(FieldName, out Value);
		}

		/// <summary>
		/// Gets the type of a field or property of an object, given its name.
		/// </summary>
		/// <param name="FieldName">Name of field or property.</param>
		/// <param name="Object">Object.</param>
		/// <param name="FieldType">Corresponding field or property type, if found, or null otherwise.</param>
		/// <returns>If the corresponding field or property was found.</returns>
		public override bool TryGetFieldType(string FieldName, object Object, out Type FieldType)
		{
			if (Object is GenericObject Obj &&
				Obj.TryGetFieldValue(FieldName, out object Value))
			{
				FieldType = Value?.GetType() ?? typeof(object);
				return true;
			}
			else
			{
				FieldType = null;
				return false;
			}
		}

		/// <summary>
		/// Mamber name of the field or property holding the Object ID, if any. If there are no such member, this property returns null.
		/// </summary>
		public override string ObjectIdMemberName => "ObjectId";

		/// <summary>
		/// If the class has an Object ID field.
		/// </summary>
		public override bool HasObjectIdField => true;

		/// <summary>
		/// If the class has an Object ID.
		/// </summary>
		/// <param name="Value">Object reference.</param>
		public override bool HasObjectId(object Value)
		{
			if (Value is GenericObject Obj)
				return !Obj.ObjectId.Equals(Guid.Empty);
			else
				return false;
		}

		/// <summary>
		/// Gets the Object ID for a given object.
		/// </summary>
		/// <param name="Value">Object reference.</param>
		/// <param name="InsertIfNotFound">Insert object into database with new Object ID, if no Object ID is set.</param>
		/// <returns>Object ID for <paramref name="Value"/>.</returns>
		/// <exception cref="NotSupportedException">Thrown, if the corresponding class does not have an Object ID property, 
		/// or if the corresponding property type is not supported.</exception>
		public override Task<ObjectId> GetObjectId(object Value, bool InsertIfNotFound)
		{
			if (Value is GenericObject Obj)
			{
				if (!Obj.ObjectId.Equals(Guid.Empty))
					return Task.FromResult<ObjectId>(GeneratedObjectSerializerBase.GuidToObjectId(Obj.ObjectId));

				if (!InsertIfNotFound)
					throw new Exception("Object has no Object ID defined.");

				return this.GetObjectId(Value, true);
			}
			else
				throw new NotSupportedException("Objects of type " + Value.GetType().FullName + " not supported.");
		}

		/// <summary>
		/// Checks if a given field value corresponds to the default value for the corresponding field.
		/// </summary>
		/// <param name="FieldName">Name of field.</param>
		/// <param name="Value">Field value.</param>
		/// <returns>If the field value corresponds to the default value of the corresponding field.</returns>
		public override bool IsDefaultValue(string FieldName, object Value)
		{
			return false;
		}

		/// <summary>
		/// Name of collection objects of this type is to be stored in, if available. If not available, this property returns null.
		/// </summary>
		/// <param name="Object">Object in the current context. If null, the default collection name is requested.</param>
		public override string CollectionName(object Object)
		{
			if (!(Object is null) && Object is GenericObject Obj)
				return Obj.CollectionName;
			else
				return base.CollectionName(Object);
		}

	}
}
