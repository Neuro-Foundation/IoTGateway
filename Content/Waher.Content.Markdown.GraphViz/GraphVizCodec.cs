using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace Waher.Content.Markdown.GraphViz
{
    /// <summary>
    /// GraphViz encoder/decoder.
    /// </summary>
    public class GraphVizCodec : IContentDecoder, IContentEncoder
    {
        /// <summary>
        /// GraphViz encoder/decoder.
        /// </summary>
        public GraphVizCodec()
        {
        }

        /// <summary>
        /// text/vnd.graphviz
        /// </summary>
        public const string DefaultContentType = "text/vnd.graphviz";

        /// <summary>
        /// GraphViz content types.
        /// </summary>
        public static readonly string[] GraphVizContentTypes = new string[]
        {
            DefaultContentType
        };

        /// <summary>
        /// Plain text file extensions.
        /// </summary>
        public static readonly string[] GraphVizFileExtensions = new string[]
        {
            "gv", "dot", "neato", "fdp", "sfdp", "twopi", "circo"
        };

        /// <summary>
        /// Supported content types.
        /// </summary>
        public string[] ContentTypes => GraphVizContentTypes;

        /// <summary>
        /// Supported file extensions.
        /// </summary>
        public string[] FileExtensions => GraphVizFileExtensions;

        /// <summary>
        /// If the decoder decodes an object with a given content type.
        /// </summary>
        /// <param name="ContentType">Content type to decode.</param>
        /// <param name="Grade">How well the decoder decodes the object.</param>
        /// <returns>If the decoder can decode an object with the given type.</returns>
        public bool Decodes(string ContentType, out Grade Grade)
        {
            if (Array.IndexOf(GraphVizContentTypes, ContentType) >= 0)
            {
                Grade = Grade.Barely;
                return true;
            }
            else
            {
                Grade = Grade.NotAtAll;
                return false;
            }
        }

        /// <summary>
        /// Decodes an object.
        /// </summary>
        /// <param name="ContentType">Internet Content Type.</param>
        /// <param name="Data">Encoded object.</param>
        /// <param name="Encoding">Any encoding specified. Can be null if no encoding specified.</param>
        /// <param name="Fields">Any content-type related fields and their corresponding values.</param>
        ///	<param name="BaseUri">Base URI, if any. If not available, value is null.</param>
        /// <returns>Decoded object.</returns>
        /// <exception cref="ArgumentException">If the object cannot be decoded.</exception>
        public Task<object> DecodeAsync(string ContentType, byte[] Data, Encoding Encoding, KeyValuePair<string, string>[] Fields, Uri BaseUri)
        {
            string GraphViz = CommonTypes.GetString(Data, Encoding);
            return Task.FromResult<object>(new GraphVizDocument(GraphViz));
        }

        /// <summary>
        /// Tries to get the content type of an item, given its file extension.
        /// </summary>
        /// <param name="FileExtension">File extension.</param>
        /// <param name="ContentType">Content type.</param>
        /// <returns>If the extension was recognized.</returns>
        public bool TryGetContentType(string FileExtension, out string ContentType)
        {
            switch (FileExtension.ToLower())
            {
                case "gv":
                case "dot":
                case "neato":
                case "fdp":
                case "sfdp":
                case "twopi":
                case "circo":
                    ContentType = GraphVizContentTypes[0];
                    return true;

                default:
                    ContentType = string.Empty;
                    return false;
            }
        }

        /// <summary>
        /// Tries to get the file extension of an item, given its Content-Type.
        /// </summary>
        /// <param name="ContentType">Content type.</param>
        /// <param name="FileExtension">File extension.</param>
        /// <returns>If the Content-Type was recognized.</returns>
        public bool TryGetFileExtension(string ContentType, out string FileExtension)
        {
            switch (ContentType.ToLower())
            {
                case DefaultContentType:
                    FileExtension = GraphVizFileExtensions[0];
                    return true;

                default:
                    FileExtension = string.Empty;
                    return false;
            }
        }

        /// <summary>
        /// If the encoder encodes a given object.
        /// </summary>
        /// <param name="Object">Object to encode.</param>
        /// <param name="Grade">How well the encoder encodes the object.</param>
        /// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
        /// <returns>If the encoder can encode the given object.</returns>
        public bool Encodes(object Object, out Grade Grade, params string[] AcceptedContentTypes)
        {
            if (Object is GraphVizDocument)
            {
                if (InternetContent.IsAccepted(GraphVizContentTypes, AcceptedContentTypes))
                {
                    Grade = Grade.Barely;
                    return true;
                }
            }

            Grade = Grade.NotAtAll;
            return false;
        }

        /// <summary>
        /// Encodes an object.
        /// </summary>
        /// <param name="Object">Object to encode.</param>
        /// <param name="Encoding">Desired encoding of text. Can be null if no desired encoding is speified.</param>
        /// <param name="AcceptedContentTypes">Optional array of accepted content types. If array is empty, all content types are accepted.</param>
        /// <returns>Encoded object, as well as Content Type of encoding. Includes information about any text encodings used.</returns>
        /// <exception cref="ArgumentException">If the object cannot be encoded.</exception>
        public Task<KeyValuePair<byte[], string>> EncodeAsync(object Object, Encoding Encoding, params string[] AcceptedContentTypes)
        {
            if (!InternetContent.IsAccepted(GraphVizContentTypes, out string ContentType, AcceptedContentTypes))
                throw new ArgumentException("Unable to encode object, or content type not accepted.", nameof(Object));

            string s;

            if (Object is GraphVizDocument GraphVizDoc)
                s = GraphVizDoc.GraphDescription;
            else
                s = Object.ToString();

            if (Encoding is null)
            {
                ContentType += "; charset=utf-8";
                Encoding = Encoding.UTF8;
            }
            else
                ContentType += "; charset=" + Encoding.WebName;

            return Task.FromResult(new KeyValuePair<byte[], string>(Encoding.GetBytes(s), ContentType));
        }
    }
}
