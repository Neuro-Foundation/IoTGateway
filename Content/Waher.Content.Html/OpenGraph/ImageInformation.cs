﻿namespace Waher.Content.Html.OpenGraph
{
	/// <summary>
	/// Image information, as defined by the Open Graph protocol.
	/// </summary>
    public class ImageInformation : VideoInformation
    {
		private string description = null;

		/// <summary>
		/// Image information, as defined by the Open Graph protocol.
		/// </summary>
		public ImageInformation()
		{
		}

		/// <summary>
		/// A description of what is in the image (not a caption).
		/// If not defined, null is returned.
		/// </summary>
		public string Description
		{
			get => this.description;
			set => this.description = value;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is ImageInformation Image)
			{
				return base.Equals(Image) &&
					this.description == Image.description;
			}
			else
				return false;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int Result = base.GetHashCode();

			if (!(this.description is null))
				Result ^= Result << 5 ^ this.description.GetHashCode();

			return Result;
		}

	}
}
