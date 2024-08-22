﻿using System.Text;

namespace Waher.Networking.XMPP.DataForms.Layout
{
	/// <summary>
	/// Base class for all layout elements in a data form layout.
	/// </summary>
	public abstract class LayoutElement
	{
		private readonly DataForm form;

		internal LayoutElement(DataForm Form)
		{
			this.form = Form;
		}

		/// <summary>
		/// Data Form.
		/// </summary>
		public DataForm Form => this.form;

		internal abstract bool RemoveExcluded();

		internal abstract void Serialize(StringBuilder Output);

		/// <summary>
		/// Sorts the contents of the layout element.
		/// </summary>
		public virtual void Sort()
		{
			// Do nothing by default.
		}
	}
}
