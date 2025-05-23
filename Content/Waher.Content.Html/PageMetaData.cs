﻿using Waher.Content.Html.Elements;
using Waher.Content.Html.OpenGraph;
using Waher.Runtime.Collections;

namespace Waher.Content.Html
{
	/// <summary>
	/// Contains meta-data about a page.
	/// 
	/// Protocols supported:
	/// The Open Graph protocol: http://ogp.me/
	/// Twitter Cards: https://developer.twitter.com/en/docs/tweets/optimize-with-cards/overview/markup
	/// </summary>
	public class PageMetaData
	{
		private ImageInformation[] images = null;
		private AudioInformation[] audio = null;
		private VideoInformation[] video = null;
		private string[] localeAlternate = null;
		private string title = null;
		private string type = null;
		private string url = null;
		private string description = null;
		private string determiner = null;
		private string locale = null;
		private string siteName = null;

		/// <summary>
		/// Contains meta-data about a page.
		/// 
		/// Protocols supported:
		/// The Open Graph protocol: http://ogp.me/
		/// Twitter Cards: https://developer.twitter.com/en/docs/tweets/optimize-with-cards/overview/markup
		/// </summary>
		public PageMetaData()
		{
		}

		/// <summary>
		/// Contains meta-data about a page.
		/// 
		/// Protocols supported:
		/// The Open Graph protocol: http://ogp.me/
		/// </summary>
		/// <param name="Doc">HTML Document</param>
		public PageMetaData(HtmlDocument Doc)
		{
			ChunkedList<ImageInformation> Images = null;
			ChunkedList<AudioInformation> Audio = null;
			ChunkedList<VideoInformation> Video = null;
			ChunkedList<string> LocaleAlternate = null;
			ImageInformation LastImage = null;
			AudioInformation LastAudio = null;
			VideoInformation LastVideo = null;
			string Name;
			string Value;

			if (!(Doc.Meta is null))
			{
				foreach (Meta Meta in Doc.Meta)
				{
					if (!Meta.HasAttributes)
						continue;

					Name = Value = null;

					if (Meta.HasAttributes)
					{
						foreach (HtmlAttribute Attr in Meta.Attributes)
						{
							switch (Attr.FullName)
							{
								case "property":
								case "name":
									Name = Attr.Value;
									break;

								case "content":
									Value = Attr.Value;
									break;
							}
						}
					}

					if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Value))
						continue;

					switch (Name)
					{
						case "og:title":
							this.title = Value;
							break;

						case "twitter:title":
							if (string.IsNullOrEmpty(this.title))
								this.title = Value;
							break;

						case "og:type":
							this.type = Value;
							break;

						case "twitter:card":
							if (string.IsNullOrEmpty(this.type))
								this.type = Value;
							break;

						case "og:url":
							this.url = Value;
							break;

						case "og:image":
						case "og:image:url":
						case "twitter:image":
							if (!(Images is null))
							{
								LastImage = null;

								foreach (ImageInformation Image in Images)
								{
									if (Image.Url == Value)
									{
										LastImage = Image;
										break;
									}
								}

								if (!(LastImage is null))
									break;
							}

							LastImage = new ImageInformation()
							{
								Url = Value
							};

							if (Images is null)
								Images = new ChunkedList<ImageInformation>();

							Images.Add(LastImage);
							break;

						case "og:image:secure_url":
							if (!(LastImage is null))
								LastImage.SecureUrl = Value;
							break;

						case "og:image:type":
							if (!(LastImage is null))
								LastImage.ContentType = Value;
							break;

						case "og:image:width":
							if (!(LastImage is null) && int.TryParse(Value, out int i))
								LastImage.Width = i;
							break;

						case "og:image:height":
							if (!(LastImage is null) && int.TryParse(Value, out i))
								LastImage.Height = i;
							break;

						case "og:image:alt":
						case "twitter:image:alt":
							if (!(LastImage is null))
								LastImage.Description = Value;
							break;

						case "og:audio":
							LastAudio = new AudioInformation()
							{
								Url = Value
							};

							if (Audio is null)
								Audio = new ChunkedList<AudioInformation>();

							Audio.Add(LastAudio);
							break;

						case "og:audio:secure_url":
							if (!(LastAudio is null))
								LastAudio.SecureUrl = Value;
							break;

						case "og:audio:type":
							if (!(LastAudio is null))
								LastAudio.ContentType = Value;
							break;

						case "og:description":
							this.description = Value;
							break;

						case "twitter:description":
						case "description":
							if (string.IsNullOrEmpty(this.description))
								this.description = Value;
							break;

						case "og:determiner":
							this.determiner = Value;
							break;

						case "og:locale":
							this.locale = Value;
							break;

						case "og:locale:alternate":
							if (LocaleAlternate is null)
								LocaleAlternate = new ChunkedList<string>();

							LocaleAlternate.Add(Value);
							break;

						case "og:site_name":
							this.siteName = Value;
							break;

						case "og:video":
							LastVideo = new VideoInformation()
							{
								Url = Value
							};

							if (Video is null)
								Video = new ChunkedList<VideoInformation>();

							Video.Add(LastVideo);
							break;

						case "og:video:secure_url":
							if (!(LastVideo is null))
								LastVideo.SecureUrl = Value;
							break;

						case "og:video:type":
							if (!(LastVideo is null))
								LastVideo.ContentType = Value;
							break;

						case "og:video:width":
							if (!(LastVideo is null) && int.TryParse(Value, out i))
								LastVideo.Width = i;
							break;

						case "og:video:height":
							if (!(LastVideo is null) && int.TryParse(Value, out i))
								LastVideo.Height = i;
							break;
					}
				}
			}

			if (string.IsNullOrEmpty(this.title) && !(Doc.Title is null))
				this.title = Doc.Title.InnerText.Trim();

			this.images = Images?.ToArray();
			this.audio = Audio?.ToArray();
			this.video = Video?.ToArray();
			this.localeAlternate = LocaleAlternate?.ToArray();
		}

		/// <summary>
		/// Image representing page.
		/// </summary>
		public ImageInformation[] Images
		{
			get => this.images;
			set => this.images = value;
		}

		/// <summary>
		/// Audio content
		/// </summary>
		public AudioInformation[] Audio
		{
			get => this.audio;
			set => this.audio = value;
		}

		/// <summary>
		/// Video content
		/// </summary>
		public VideoInformation[] Video
		{
			get => this.video;
			set => this.video = value;
		}

		/// <summary>
		/// Alternate locales
		/// </summary>
		public string[] LocaleAlternate
		{
			get => this.localeAlternate;
			set => this.localeAlternate = value;
		}

		/// <summary>
		/// Title of page.
		/// </summary>
		public string Title
		{
			get => this.title;
			set => this.title = value;
		}

		/// <summary>
		/// Type of page.
		/// </summary>
		public string Type
		{
			get => this.type;
			set => this.type = value;
		}

		/// <summary>
		/// Canonical URL of page.
		/// </summary>
		public string Url
		{
			get => this.url;
			set => this.url = value;
		}

		/// <summary>
		/// A description of the page.
		/// </summary>
		public string Description
		{
			get => this.description;
			set => this.description = value;
		}

		/// <summary>
		/// A word that appears before the title.
		/// </summary>
		public string Determiner
		{
			get => this.determiner;
			set => this.determiner = value;
		}

		/// <summary>
		/// Locale of information.
		/// </summary>
		public string Locale
		{
			get => this.locale;
			set => this.locale = value;
		}

		/// <summary>
		/// Name of web site publishing page
		/// </summary>
		public string SiteName
		{
			get => this.siteName;
			set => this.siteName = value;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			PageMetaData Meta = obj as PageMetaData;
			int i, c;

			if ((this is null) ^ (Meta is null))
				return false;

			if (this is null)
				return true;

			if (this.title != Meta.title ||
				this.type != Meta.type ||
				this.url != Meta.url ||
				this.description != Meta.description ||
				this.determiner != Meta.determiner ||
				this.locale != Meta.locale ||
				this.siteName != Meta.siteName)
			{
				return false;
			}

			if ((this.localeAlternate is null) ^ (Meta.localeAlternate is null))
				return false;

			if (!(this.localeAlternate is null))
			{
				if ((c = this.localeAlternate.Length) != Meta.localeAlternate.Length)
					return false;

				for (i = 0; i < c; i++)
				{
					if (this.localeAlternate[i] != Meta.localeAlternate[i])
						return false;
				}
			}

			if ((this.images is null) ^ (Meta.images is null))
				return false;

			if (!(this.images is null))
			{
				if ((c = this.images.Length) != Meta.images.Length)
					return false;

				for (i = 0; i < c; i++)
				{
					if (!this.images[i].Equals(Meta.images[i]))
						return false;
				}
			}

			if ((this.audio is null) ^ (Meta.audio is null))
				return false;

			if (!(this.audio is null))
			{
				if ((c = this.audio.Length) != Meta.audio.Length)
					return false;

				for (i = 0; i < c; i++)
				{
					if (!this.audio[i].Equals(Meta.audio[i]))
						return false;
				}
			}

			if ((this.video is null) ^ (Meta.video is null))
				return false;

			if (!(this.video is null))
			{
				if ((c = this.video.Length) != Meta.video.Length)
					return false;

				for (i = 0; i < c; i++)
				{
					if (!this.video[i].Equals(Meta.video[i]))
						return false;
				}
			}

			return true;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int Result = 0;

			if (!(this.title is null))
				Result = this.title.GetHashCode();

			if (!(this.type is null))
				Result ^= Result << 5 ^ this.type.GetHashCode();

			if (!(this.url is null))
				Result ^= Result << 5 ^ this.url.GetHashCode();

			if (!(this.description is null))
				Result ^= Result << 5 ^ this.description.GetHashCode();

			if (!(this.determiner is null))
				Result ^= Result << 5 ^ this.determiner.GetHashCode();

			if (!(this.locale is null))
				Result ^= Result << 5 ^ this.locale.GetHashCode();

			if (!(this.siteName is null))
				Result ^= Result << 5 ^ this.siteName.GetHashCode();

			if (!(this.localeAlternate is null))
			{
				foreach (string s in this.localeAlternate)
					Result ^= Result << 5 ^ s.GetHashCode();
			}

			if (!(this.images is null))
			{
				foreach (ImageInformation Obj in this.images)
					Result ^= Result << 5 ^ Obj.GetHashCode();
			}

			if (!(this.audio is null))
			{
				foreach (AudioInformation Obj in this.audio)
					Result ^= Result << 5 ^ Obj.GetHashCode();
			}

			if (!(this.video is null))
			{
				foreach (VideoInformation Obj in this.video)
					Result ^= Result << 5 ^ Obj.GetHashCode();
			}

			return Result;
		}

	}
}
