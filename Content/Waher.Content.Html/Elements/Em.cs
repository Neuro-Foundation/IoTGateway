﻿namespace Waher.Content.Html.Elements
{
	/// <summary>
	/// EM element
	/// </summary>
    public class Em : HtmlElement
    {
		/// <summary>
		/// EM element
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent element. Can be null for root elements.</param>
		/// <param name="StartPosition">Start position.</param>
		public Em(HtmlDocument Document, HtmlElement Parent, int StartPosition)
			: base(Document, Parent, StartPosition, "EM")
		{
		}
    }
}
