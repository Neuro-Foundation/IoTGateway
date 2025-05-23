﻿namespace Waher.Content.Html.Elements
{
	/// <summary>
	/// ABBR element
	/// </summary>
    public class Abbr : HtmlElement
    {
		/// <summary>
		/// ABBR element
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent element. Can be null for root elements.</param>
		/// <param name="StartPosition">Start position.</param>
		public Abbr(HtmlDocument Document, HtmlElement Parent, int StartPosition)
			: base(Document, Parent, StartPosition, "ABBR")
		{
		}
    }
}
