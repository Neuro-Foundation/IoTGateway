using System;
using System.IO;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Images;
using Waher.Runtime.Inventory;
using Waher.Script;

namespace Waher.Content.Markdown.GraphViz
{
    /// <summary>
    /// Converts GraphViz documents to images.
    /// </summary>
    public class GraphVizDocumentToImageConverter : IContentConverter
    {
        /// <summary>
        /// Converts GraphViz documents to images.
        /// </summary>
        public GraphVizDocumentToImageConverter()
        {
        }

        /// <summary>
        /// Converts content from these content types.
        /// </summary>
        public string[] FromContentTypes => new string[] { GraphVizCodec.DefaultContentType };

        /// <summary>
        /// Converts content to these content types. 
        /// </summary>
        public virtual string[] ToContentTypes
        {
            get
            {
                return new string[]
                {
                    ImageCodec.ContentTypePng,
                    ImageCodec.ContentTypeSvg
                };
            }
        }

        /// <summary>
        /// How well the content is converted.
        /// </summary>
        public virtual Grade ConversionGrade => Grade.Excellent;

        /// <summary>
        /// Performs the actual conversion.
        /// </summary>
        /// <param name="State">State of the current conversion.</param>
        /// <returns>If the result is dynamic (true), or only depends on the source (false).</returns>
        public async Task<bool> ConvertAsync(ConversionState State)
        {
            string GraphDescription;

            using (StreamReader rd = new StreamReader(State.From))
            {
                GraphDescription = rd.ReadToEnd();
            }

            string s = State.ToContentType;
            int i;

            i = s.IndexOf(';');
            if (i > 0)
                s = s.Substring(0, i);

            bool Png = string.Compare(s, ImageCodec.ContentTypePng, true) == 0;
            bool Svg = string.Compare(s, ImageCodec.ContentTypeSvg, true) == 0;

            if (!(State.PossibleContentTypes is null))
            {
                foreach (string ContentType in State.PossibleContentTypes)
                {
                    s = ContentType;
                    i = s.IndexOf(';');
                    if (i > 0)
                        s = s.Substring(0, i);

                    Png |= string.Compare(s, ImageCodec.ContentTypePng, true) == 0;
                    Svg |= string.Compare(s, ImageCodec.ContentTypeSvg, true) == 0;
                }
            }

            Variables Variables = new Variables();
            GraphInfo Graph;

            if (Svg)
            {
                Graph = await GraphViz.GetFileName("dot", GraphDescription, ResultType.Svg, true, Variables);
                State.ToContentType = ImageCodec.ContentTypeSvg;
            }
            else if (Png)
            {
                Graph = await GraphViz.GetFileName("dot", GraphDescription, ResultType.Png, true, Variables);
                State.ToContentType = ImageCodec.ContentTypePng;
            }
            else
                throw new Exception("Unable to convert document from " + State.FromContentType + " to " + State.ToContentType);

            byte[] Data = await Resources.ReadAllBytesAsync(Graph.FileName);

            await State.To.WriteAsync(Data, 0, Data.Length);

            return false;
        }

    }
}