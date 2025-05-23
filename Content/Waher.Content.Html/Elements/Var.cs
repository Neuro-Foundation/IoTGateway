﻿namespace Waher.Content.Html.Elements
{
	/// <summary>
	/// VAR element
	/// </summary>
    public class Var : HtmlElement
    {
		/// <summary>
		/// VAR element
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent element. Can be null for root elements.</param>
		/// <param name="StartPosition">Start position.</param>
		public Var(HtmlDocument Document, HtmlElement Parent, int StartPosition)
			: base(Document, Parent, StartPosition, "VAR")
		{
		}
    }
}
