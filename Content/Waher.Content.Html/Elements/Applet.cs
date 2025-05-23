﻿namespace Waher.Content.Html.Elements
{
	/// <summary>
	/// APPLET element
	/// </summary>
    public class Applet : HtmlElement
    {
		/// <summary>
		/// APPLET element
		/// </summary>
		/// <param name="Document">HTML Document.</param>
		/// <param name="Parent">Parent element. Can be null for root elements.</param>
		/// <param name="StartPosition">Start position.</param>
		public Applet(HtmlDocument Document, HtmlElement Parent, int StartPosition)
			: base(Document, Parent, StartPosition, "APPLET")
		{
		}
    }
}
