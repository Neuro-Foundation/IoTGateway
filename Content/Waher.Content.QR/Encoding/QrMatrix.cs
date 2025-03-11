﻿using System;
using System.Text;

namespace Waher.Content.QR.Encoding
{
	/// <summary>
	/// Delegate for mask functions
	/// </summary>
	/// <param name="x">Zero-based X-coordinte.</param>
	/// <param name="y">Zero-based Y-coordinte.</param>
	/// <returns>If the bit should be swapped.</returns>
	public delegate bool MaskFunction(int x, int y);

	/// <summary>
	/// Delegate for QR-code color functions
	/// </summary>
	/// <param name="CodeX">Zero-based normalized X-coordinte into code.</param>
	/// <param name="CodeY">Zero-based normalized Y-coordinte into code.</param>
	/// <param name="DotX">Zero-based normalized X-coordinte into dot.</param>
	/// <param name="DotY">Zero-based normalized Y-coordinte into dot.</param>
	/// <param name="Type">Type of dot to paint.</param>
	/// <returns>Color of pixel, in RGBA (LSB first, i.e. 0xAABBGGRR format).</returns>
	public delegate uint ColorFunction(float CodeX, float CodeY, float DotX, float DotY, DotType Type);

	/// <summary>
	/// Class used to compute a QR code matrix.
	/// </summary>
	public class QrMatrix
	{
		private static class FinderMarker
		{
			private const DotType X = DotType.FinderMarkerForegroundOuter;
			private const DotType x = DotType.FinderMarkerForegroundInner;
			private const DotType _ = DotType.FinderMarkerBackground;

			public static readonly DotType[,] Dots = new DotType[,]
			{
				{  X, X, X, X, X, X, X },
				{  X, _, _, _, _, _, X },
				{  X, _, x, x, x, _, X },
				{  X, _, x, x, x, _, X },
				{  X, _, x, x, x, _, X },
				{  X, _, _, _, _, _, X },
				{  X, X, X, X, X, X, X }
			};
		}

		private static class AlignmentMarker
		{
			private const DotType X = DotType.AlignmentMarkerForegroundOuter;
			private const DotType x = DotType.AlignmentMarkerForegroundInner;
			private const DotType _ = DotType.AlignmentMarkerBackground;

			public static readonly DotType[,] Dots = new DotType[,]
			{
				{  X, X, X, X, X },
				{  X, _, _, _, X },
				{  X, _, x, _, X },
				{  X, _, _, _, X },
				{  X, X, X, X, X }
			};
		}

		private static readonly char[] halfBlocks = new char[] { ' ', '▀', '▄', '█' };
		private static readonly char[] quarterBlocks = new char[] { ' ', '▘', '▝', '▀', '▖', '▌', '▞', '▛', '▗', '▚', '▐', '▜', '▄', '▙', '▟', '█' };

		private readonly int size;
		private readonly bool[,] defined;
		private bool[,] mask;
		private DotType[,] dots;

		/// <summary>
		/// Class used to compute a QR code matrix.
		/// </summary>
		/// <param name="Size">Size of matrix.</param>
		public QrMatrix(int Size)
		{
			if (Size < 21)
				throw new ArgumentException("Invalid size.", nameof(Size));

			this.size = Size;
			this.defined = this.mask = new bool[Size, Size];
			this.dots = new DotType[Size, Size];
		}

		/// <summary>
		/// Class used to compute a QR code matrix.
		/// </summary>
		public QrMatrix(DotType[,] Matrix, bool[,] Mask)
		{
			int c = Matrix.GetLength(0);
			if (Matrix.GetLength(1) != c)
				throw new ArgumentException("Matrix not square.", nameof(Matrix));

			if (c < 21 || c > 177 || ((c - 21) & 3) != 0)
				throw new ArgumentException("Invalid matrix dimensions.", nameof(Matrix));

			if (Mask.GetLength(0) != c || Mask.GetLength(1) != c)
				throw new ArgumentException("Mask must be same dimension as matrix.", nameof(Mask));

			this.size = c;
			this.dots = Matrix;
			this.mask = Mask;
			this.defined = (bool[,])Mask.Clone();
		}

		/// <summary>
		/// Size of the matrix (along each side).
		/// </summary>
		public int Size => this.size;

		/// <summary>
		/// Encoded dots.
		/// </summary>
		public DotType[,] Dots => this.dots;

		/// <summary>
		/// What parts of the mask has been defined.
		/// </summary>
		public bool[,] Defined => this.defined;

		/// <summary>
		/// Draws a Finder marker pattern in the matrix.
		/// </summary>
		/// <param name="X">Left coordinate of the marker.</param>
		/// <param name="Y">Right coordinate of the marker.</param>
		public void DrawFinderMarker(int X, int Y)
		{
			int x, y;

			for (y = 0; y < 7; y++)
			{
				for (x = 0; x < 7; x++)
				{
					this.defined[y + Y, x + X] = true;
					this.dots[y + Y, x + X] = FinderMarker.Dots[y, x];
				}
			}
		}

		/// <summary>
		/// Draws a Alignment marker pattern in the matrix.
		/// </summary>
		/// <param name="X">Left coordinate of the marker.</param>
		/// <param name="Y">Right coordinate of the marker.</param>
		public void DrawAlignmentMarker(int X, int Y)
		{
			int x, y;

			for (y = 0; y < 5; y++)
			{
				for (x = 0; x < 5; x++)
				{
					this.defined[y + Y, x + X] = true;
					this.dots[y + Y, x + X] = AlignmentMarker.Dots[y, x];
				}
			}
		}

		/// <summary>
		/// Draws a horizontal line in the matrix.
		/// </summary>
		/// <param name="X1">Left coordinate.</param>
		/// <param name="X2">Right coordinate.</param>
		/// <param name="Y">Y coordinate.</param>
		/// <param name="Dot">If pixels should be lit (true) or cleared (false).</param>
		/// <param name="Dotted">If pixels should be set and cleared consecutively.</param>
		public void HLine(int X1, int X2, int Y, DotType Dot, bool Dotted)
		{
			while (X1 <= X2)
			{
				if (!this.defined[Y, X1])
				{
					this.defined[Y, X1] = true;
					this.dots[Y, X1] = Dot;
				}

				X1++;

				if (Dotted)
					Dot = (DotType)((int)Dot ^ 1);
			}
		}

		/// <summary>
		/// Draws a horizontal line in the matrix.
		/// </summary>
		/// <param name="X">X coordinate.</param>
		/// <param name="Y1">Top coordinate.</param>
		/// <param name="Y2">Bottom coordinate.</param>
		/// <param name="Dot">If pixels should be lit (true) or cleared (false).</param>
		/// <param name="Dotted">If pixels should be set and cleared consecutively.</param>
		public void VLine(int X, int Y1, int Y2, DotType Dot, bool Dotted)
		{
			while (Y1 <= Y2)
			{
				if (!this.defined[Y1, X])
				{
					this.defined[Y1, X] = true;
					this.dots[Y1, X] = Dot;
				}

				Y1++;

				if (Dotted)
					Dot = (DotType)((int)Dot ^ 1);
			}
		}

		/// <summary>
		/// Number of defined dots in the matrix.
		/// </summary>
		public int NrDefinedDots
		{
			get
			{
				int Result = 0;
				int x, y;

				for (y = 0; y < this.size; y++)
				{
					for (x = 0; x < this.size; x++)
					{
						if (this.defined[y, x])
							Result++;
					}
				}

				return Result;
			}
		}

		/// <summary>
		/// Number of undefined dots in the matrix.
		/// </summary>
		public int NrUndefined => this.size * this.size - this.NrDefinedDots;

		/// <summary>
		/// Encodes data bits in free positions in the matrix.
		/// </summary>
		/// <param name="Data"></param>
		/// <returns>If all bytes fit into the matrix.</returns>
		public bool EncodeDataBits(byte[] Data)
		{
			int i = 0;
			int c = Data.Length;
			int State = 0;
			int x = this.size - 1;
			int y = this.size - 1;
			int j;
			byte b;

			while (i < c)
			{
				b = Data[i++];

				for (j = 0; j < 8; j++)
				{
					if (x < 0)
						return false;

					if (this.defined[y, x])
						j--;
					else
					{
						this.defined[y, x] = true;
						this.dots[y, x] = (b & 0x80) != 0 ? DotType.CodeForeground : DotType.CodeBackground;
						b <<= 1;
					}

					switch (State)
					{
						case 0:
							x--;
							State++;
							break;

						case 1:
							if (y == 0)
							{
								x--;
								if (x == 6)
									x--;
								State++;
							}
							else
							{
								y--;
								x++;
								State--;
							}
							break;

						case 2:
							x--;
							State++;
							break;

						case 3:
							if (y == this.size - 1)
							{
								x--;
								State = 0;
							}
							else
							{
								y++;
								x++;
								State--;
							}
							break;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Generates a text string representing the QR code using
		/// full block characters and spaces.
		/// Must be displayed in a font with fixed character sizes,
		/// preferrably where each character is twice as high as wide.
		/// </summary>
		/// <returns>QR Code.</returns>
		public string ToFullBlockText()
		{
			StringBuilder sb = new StringBuilder();

			string s = new string(' ', 16 + (this.size << 1));
			int x, y;

			for (y = 0; y < 4; y++)
				sb.AppendLine(s);

			for (y = 0; y < this.size; y++)
			{
				sb.Append("        ");

				for (x = 0; x < this.size; x++)
				{
					if (((int)this.dots[y, x] & 1) != 0)
						sb.Append("██");
					else
						sb.Append("  ");
				}

				sb.AppendLine("        ");
			}

			for (y = 0; y < 4; y++)
				sb.AppendLine(s);

			return sb.ToString();
		}

		/// <summary>
		/// Generates a text string representing the QR code using
		/// half block characters and spaces.
		/// Must be displayed in a font with fixed character sizes,
		/// preferrably where each character is twice as high as wide.
		/// </summary>
		/// <returns>QR Code.</returns>
		public string ToHalfBlockText()
		{
			StringBuilder sb = new StringBuilder();

			string s = new string(' ', 8 + this.size);
			int x, y, y2;
			int i;

			for (y = 0; y < 4; y++)
				sb.AppendLine(s);

			for (y = 0; y < this.size; y += 2)
			{
				y2 = y + 1;

				sb.Append("    ");

				for (x = 0; x < this.size; x++)
				{
					i = (int)this.dots[y, x] & 1;

					if (y2 < this.size && ((int)this.dots[y2, x] & 1) != 0)
						i |= 2;

					sb.Append(halfBlocks[i]);
				}

				sb.AppendLine("    ");
			}

			for (y = 0; y < 4; y++)
				sb.AppendLine(s);

			return sb.ToString();
		}

		/// <summary>
		/// Generates a text string representing the QR code using
		/// quarter block characters and spaces.
		/// Must be displayed in a font with fixed character sizes,
		/// preferrably where each character are square.
		/// </summary>
		/// <returns>QR Code.</returns>
		public string ToQuarterBlockText()
		{
			StringBuilder sb = new StringBuilder();

			string s = new string(' ', 4 + (this.size + 1) >> 1);
			int x, y, x2, y2;
			int i;

			sb.AppendLine(s);
			sb.AppendLine(s);

			for (y = 0; y < this.size; y += 2)
			{
				y2 = y + 1;

				sb.Append("  ");

				for (x = 0; x < this.size; x++)
				{
					x2 = x + 1;

					i = (int)this.dots[y, x] & 1;

					if (x2 < this.size && ((int)this.dots[y, x2] & 1) != 0)
						i |= 2;

					if (y2 < this.size && ((int)this.dots[y2, x] & 1) != 0)
						i |= 4;

					if (x2 < this.size && y2 < this.size && ((int)this.dots[y2, x2] & 1) != 0)
						i |= 8;

					sb.Append(quarterBlocks[i]);
				}

				sb.AppendLine("  ");
			}

			sb.AppendLine(s);
			sb.AppendLine(s);

			return sb.ToString();
		}

		/// <summary>
		/// Calculates a penalty score based on horizontal bands of dots of the same color.
		/// </summary>
		/// <returns>Penalty score</returns>
		public int PenaltyHorizontalBands()
		{
			int x, y, c;
			int Result = 0;
			bool? Prev;
			bool b;

			for (y = 0; y < this.size; y++)
			{
				Prev = null;
				c = 0;

				for (x = 0; x < this.size; x++)
				{
					if (Prev.HasValue)
					{
						if (Prev == (b = ((int)this.dots[y, x] & 1) != 0))
						{
							c++;
							if (c == 5)
								Result += 3;
							else if (c > 5)
								Result++;
						}
						else
						{
							c = 1;
							Prev = b;
						}
					}
					else
					{
						Prev = ((int)this.dots[y, x] & 1) != 0;
						c = 1;
					}
				}
			}

			return Result;
		}

		/// <summary>
		/// Calculates a penalty score based on horizontal bands of dots of the same color.
		/// </summary>
		/// <returns>Penalty score</returns>
		public int PenaltyVerticalBands()
		{
			int x, y, c;
			int Result = 0;
			bool? Prev;
			bool b;

			for (x = 0; x < this.size; x++)
			{
				Prev = null;
				c = 0;

				for (y = 0; y < this.size; y++)
				{
					if (Prev.HasValue)
					{
						if (Prev == (b = ((int)this.dots[y, x] & 1) != 0))
						{
							c++;
							if (c == 5)
								Result += 3;
							else if (c > 5)
								Result++;
						}
						else
							c = 1;

						Prev = b;
					}
					else
					{
						Prev = ((int)this.dots[y, x] & 1) != 0;
						c = 1;
					}
				}
			}

			return Result;
		}

		/// <summary>
		/// Calculates a penalty score based on same colored blocks.
		/// </summary>
		/// <returns>Penalty score</returns>
		public int PenaltyBlocks()
		{
			int x, y, c = this.size - 1;
			int Result = 0;
			int b;

			for (y = 0; y < c; y++)
			{
				for (x = 0; x < c; x++)
				{
					b = (int)this.dots[y, x] & 1;

					if (((int)this.dots[y + 1, x] & 1) == b &&
						((int)this.dots[y, x + 1] & 1) == b &&
						((int)this.dots[y + 1, x + 1] & 1) == b)
					{
						Result += 3;
					}
				}
			}

			return Result;
		}

		/// <summary>
		/// Calculates a penalty score based on horizontal finder patterns found.
		/// </summary>
		/// <returns>Penalty score</returns>
		public int PenaltyHorizontalFinderPattern()
		{
			int x, y, i;
			int Result = 0;
			bool b;

			for (y = 0; y < this.size; y++)
			{
				i = 0;
				for (x = 0; x < this.size; x++)
				{
					b = ((int)this.dots[y, x] & 1) != 0;
					switch (i)
					{
						case 0:
						case 1:
						case 2:
						case 3:
						case 5:
						case 9:
							if (!b)
								i++;
							else
								i = 0;
							break;

						case 4:
							if (b)
								i++;
							break;

						case 6:
						case 7:
						case 8:
							if (b)
								i++;
							else
								i = 0;
							break;

						case 10:
							if (b)
								Result += 40;

							i = 0;
							break;
					}
				}
			}

			return Result;
		}

		/// <summary>
		/// Calculates a penalty score based on vertical finder patterns found.
		/// </summary>
		/// <returns>Penalty score</returns>
		public int PenaltyVerticalFinderPattern()
		{
			int x, y, i;
			int Result = 0;
			bool b;

			for (x = 0; x < this.size; x++)
			{
				i = 0;
				for (y = 0; y < this.size; y++)
				{
					b = ((int)this.dots[y, x] & 1) != 0;
					switch (i)
					{
						case 0:
						case 1:
						case 2:
						case 3:
						case 5:
						case 9:
							if (!b)
								i++;
							else
								i = 0;
							break;

						case 4:
							if (b)
								i++;
							break;

						case 6:
						case 7:
						case 8:
							if (b)
								i++;
							else
								i = 0;
							break;

						case 10:
							if (b)
								Result += 40;

							i = 0;
							break;
					}
				}
			}

			return Result;
		}

		/// <summary>
		/// Calculates a penalty score based on the balance between dark and 
		/// light dots.
		/// </summary>
		/// <returns>Penalty score</returns>
		public int PenaltyBalance()
		{
			int x, y;
			int NrDark = 0;
			int NrLight = 0;

			for (y = 0; y < this.size; y++)
			{
				for (x = 0; x < this.size; x++)
				{
					if (((int)this.dots[y, x] & 1) != 0)
						NrDark++;
					else
						NrLight++;
				}
			}

			int PercentDark = (100 * NrDark) / (NrDark + NrLight);
			int Prev5 = (PercentDark / 5) * 5;
			int Next5 = Prev5 += 5;

			Prev5 = Math.Abs(Prev5 - 50) / 5;
			Next5 = Math.Abs(Next5 - 50) / 5;

			return Math.Min(Prev5, Next5) * 10;
		}

		/// <summary>
		/// Calculates the total penalty score of the matrix.
		/// </summary>
		/// <returns>Penalty score.</returns>
		public int Penalty()
		{
			return this.Penalty(null);
		}

		/// <summary>
		/// Calculates the total penalty score of the matrix.
		/// </summary>
		/// <param name="Mask">Optional mask function. May be null.</param>
		/// <returns>Penalty score.</returns>
		public int Penalty(MaskFunction Mask)
		{
			DotType[,] Bak = this.dots;

			if (!(Mask is null))
				this.ApplyMask(Mask);

			int Result =
				this.PenaltyHorizontalBands() +
				this.PenaltyVerticalBands() +
				this.PenaltyBlocks() +
				this.PenaltyHorizontalFinderPattern() +
				this.PenaltyVerticalFinderPattern() +
				this.PenaltyBalance();

			this.dots = Bak;

			return Result;
		}

		/// <summary>
		/// Mask function 0
		/// </summary>
		/// <param name="x">Zero-based X-coordinte.</param>
		/// <param name="y">Zero-based Y-coordinte.</param>
		/// <returns>If the bit should be swapped.</returns>
		public static bool Mask0(int x, int y) => ((x + y) & 1) == 0;

		/// <summary>
		/// Mask function 1
		/// </summary>
		/// <param name="_">Zero-based X-coordinte.</param>
		/// <param name="y">Zero-based Y-coordinte.</param>
		/// <returns>If the bit should be swapped.</returns>
		public static bool Mask1(int _, int y) => (y & 1) == 0;

		/// <summary>
		/// Mask function 2
		/// </summary>
		/// <param name="x">Zero-based X-coordinte.</param>
		/// <param name="_">Zero-based Y-coordinte.</param>
		/// <returns>If the bit should be swapped.</returns>
		public static bool Mask2(int x, int _) => (x % 3) == 0;

		/// <summary>
		/// Mask function 3
		/// </summary>
		/// <param name="x">Zero-based X-coordinte.</param>
		/// <param name="y">Zero-based Y-coordinte.</param>
		/// <returns>If the bit should be swapped.</returns>
		public static bool Mask3(int x, int y) => ((x + y) % 3) == 0;

		/// <summary>
		/// Mask function 4
		/// </summary>
		/// <param name="x">Zero-based X-coordinte.</param>
		/// <param name="y">Zero-based Y-coordinte.</param>
		/// <returns>If the bit should be swapped.</returns>
		public static bool Mask4(int x, int y) => (((x / 3) + (y / 2)) & 1) == 0;

		/// <summary>
		/// Mask function 5
		/// </summary>
		/// <param name="x">Zero-based X-coordinte.</param>
		/// <param name="y">Zero-based Y-coordinte.</param>
		/// <returns>If the bit should be swapped.</returns>
		public static bool Mask5(int x, int y) => ((x * y) & 1) + ((x * y) % 3) == 0;

		/// <summary>
		/// Mask function 6
		/// </summary>
		/// <param name="x">Zero-based X-coordinte.</param>
		/// <param name="y">Zero-based Y-coordinte.</param>
		/// <returns>If the bit should be swapped.</returns>
		public static bool Mask6(int x, int y) => ((((x * y) & 1) + ((x * y) % 3)) & 1) == 0;

		/// <summary>
		/// Mask function 7
		/// </summary>
		/// <param name="x">Zero-based X-coordinte.</param>
		/// <param name="y">Zero-based Y-coordinte.</param>
		/// <returns>If the bit should be swapped.</returns>
		public static bool Mask7(int x, int y) => ((((x + y) & 1) + ((x * y) % 3)) & 1) == 0;

		/// <summary>
		/// Saves the currently defined dots as a mask.
		/// </summary>
		public void SaveMask()
		{
			this.mask = (bool[,])this.defined.Clone();
		}

		/// <summary>
		/// Applies a mask on the matrix.
		/// </summary>
		/// <param name="Mask">Mask function</param>
		public void ApplyMask(MaskFunction Mask)
		{
			int x, y;

			this.dots = (DotType[,])this.dots.Clone();

			for (y = 0; y < this.size; y++)
			{
				for (x = 0; x < this.size; x++)
				{
					if (!this.mask[y, x] && Mask(x, y))
						this.dots[y, x] = (DotType)((int)this.dots[y, x] ^ 1);
				}
			}
		}

		/// <summary>
		/// Writes bits to the matrix.
		/// </summary>
		/// <param name="Bits">Bits to write to the matrix, from MSB to LSB.</param>
		/// <param name="X">Start X-coordinate.</param>
		/// <param name="Y">Start Y-coordinate.</param>
		/// <param name="Dx">Movement along X-axis after each bit.</param>
		/// <param name="Dy">Movement along Y-axis after each bit.</param>
		/// <param name="NrBits">Number of bits to write.</param>
		public void WriteBits(uint Bits, int X, int Y, int Dx, int Dy, int NrBits)
		{
			while (NrBits > 0)
			{
				this.defined[Y, X] = true;
				this.dots[Y, X] = (Bits & 0x80000000) != 0 ? DotType.CodeForeground : DotType.CodeBackground;
				Bits <<= 1;
				NrBits--;
				X += Dx;
				Y += Dy;
			}
		}

		/// <summary>
		/// Converts the matrix to pixels, each pixel represented by 4 bytes
		/// in the order Red, Green, Blue, Alpha (RGBA).
		/// </summary>
		/// <returns>Pixels</returns>
		public byte[] ToRGBA()
		{
			return this.ToRGBA(this.size + 8, this.size + 8);
		}

		/// <summary>
		/// Converts the matrix to pixels, each pixel represented by 4 bytes
		/// in the order Red, Green, Blue, Alpha (RGBA).
		/// </summary>
		/// <param name="Width">Width of resulting bitmap image.</param>
		/// <param name="Height">Height of resulting bitmap image.</param>
		/// <returns>Pixels</returns>
		public byte[] ToRGBA(int Width, int Height)
		{
			byte[] Result = new byte[Width * Height * 4];
			int SourceSize = this.size + 8;
			int HalfSourceSize = SourceSize >> 1;
			int HalfWidth = Width >> 1;
			int HalfHeight = Height >> 1;
			int Left = (4 * Width + HalfSourceSize) / SourceSize;
			int Top = (4 * Height + HalfSourceSize) / SourceSize;
			int dx = ((SourceSize << 16) + HalfWidth) / Width;
			int dy = ((SourceSize << 16) + HalfHeight) / Height;
			int i = 0;
			int imgX, imgY;
			int srcX, srcY;
			int x, y;

			for (imgY = srcY = 0; imgY < Height; imgY++)
			{
				y = srcY >> 16;

				if (imgY < Top || y >= this.size)
				{
					for (imgX = 0; imgX < Width; imgX++)
					{
						Result[i++] = 0xff;
						Result[i++] = 0xff;
						Result[i++] = 0xff;
						Result[i++] = 0xff;
					}

					continue;
				}

				for (imgX = 0; imgX < Left; imgX++)
				{
					Result[i++] = 0xff;
					Result[i++] = 0xff;
					Result[i++] = 0xff;
					Result[i++] = 0xff;
				}

				for (srcX = 0; imgX < Width; imgX++)
				{
					x = srcX >> 16;
					if (x >= this.size)
						break;

					if (((int)this.dots[y, x] & 1) != 0)
					{
						Result[i++] = 0x00;
						Result[i++] = 0x00;
						Result[i++] = 0x00;
					}
					else
					{
						Result[i++] = 0xff;
						Result[i++] = 0xff;
						Result[i++] = 0xff;
					}

					Result[i++] = 0xff;
					srcX += dx;
				}

				for (; imgX < Width; imgX++)
				{
					Result[i++] = 0xff;
					Result[i++] = 0xff;
					Result[i++] = 0xff;
					Result[i++] = 0xff;
				}

				srcY += dy;
			}

			return Result;
		}

		/// <summary>
		/// Converts the matrix to pixels, each pixel represented by 4 bytes
		/// in the order Red, Green, Blue, Alpha (RGBA).
		/// </summary>
		/// <param name="Color">Color function used to color dots representing ones.</param>
		/// <param name="AntiAlias">If anti-aliasing is to be used.</param>
		/// <returns>Pixels</returns>
		public byte[] ToRGBA(ColorFunction Color, bool AntiAlias)
		{
			return this.ToRGBA(this.size + 8, this.size + 8, Color, AntiAlias);
		}

		/// <summary>
		/// Converts the matrix to pixels, each pixel represented by 4 bytes
		/// in the order Red, Green, Blue, Alpha (RGBA).
		/// </summary>
		/// <param name="Color">Color function used to color dots representing ones.</param>
		/// <param name="Quality">Number of dots to calculate per each dimension in each pixel.</param>
		/// <returns>Pixels</returns>
		public byte[] ToRGBA(ColorFunction Color, int Quality)
		{
			return this.ToRGBA(this.size + 8, this.size + 8, Color, Quality);
		}

		/// <summary>
		/// Converts the matrix to pixels, each pixel represented by 4 bytes
		/// in the order Red, Green, Blue, Alpha (RGBA).
		/// </summary>
		/// <param name="Width">Width of resulting bitmap image.</param>
		/// <param name="Height">Height of resulting bitmap image.</param>
		/// <param name="Color">Color function used to color dots representing ones.</param>
		/// <param name="AntiAlias">If anti-aliasing is to be used.</param>
		/// <returns>Pixels</returns>
		public byte[] ToRGBA(int Width, int Height, ColorFunction Color, bool AntiAlias)
		{
			return this.ToRGBA(Width, Height, Color, AntiAlias ? 2 : 1);
		}

		/// <summary>
		/// Converts the matrix to pixels, each pixel represented by 4 bytes
		/// in the order Red, Green, Blue, Alpha (RGBA).
		/// </summary>
		/// <param name="Width">Width of resulting bitmap image.</param>
		/// <param name="Height">Height of resulting bitmap image.</param>
		/// <param name="Color">Color function used to color dots representing ones.</param>
		/// <param name="Quality">Number of dots to calculate per each dimension in each pixel.</param>
		/// <returns>Pixels</returns>
		public byte[] ToRGBA(int Width, int Height, ColorFunction Color, int Quality)
		{
			if (Width > 2048)
				throw new ArgumentException("Too large.", nameof(Width));

			if (Height > 2048)
				throw new ArgumentException("Too large.", nameof(Height));

			if (Quality <= 0)
				throw new ArgumentException("Invalid quality.", nameof(Quality));

			int QWidth = Width * Quality;
			int QHeight = Height * Quality;

			if (QWidth > 8192 || QHeight > 8192)
				throw new ArgumentException("Invalid quality.", nameof(Quality));

			byte[] Result = new byte[Width * Height * 4];
			int CalcSize = QWidth * 4;
			int[] Calc = new int[CalcSize];
			int SourceMargin = 4;
			float SourceSize = this.size + (2 * SourceMargin);
			float dx = SourceSize / QWidth;
			float dy = SourceSize / QHeight;
			float x0 = -SourceMargin - 0.5f;
			float y0 = -SourceMargin - 0.5f;
			int i, j;
			int q2 = Quality * Quality;
			int q2r = q2 >> 1;
			int imgX, imgY;
			int srcX, srcY;
			int qY, qX;
			float x, y, scale;
			uint cl;
			DotType Dot;
			bool yOutside;

			scale = 1.0f / this.size;

			for (imgY = i = 0, y = y0; imgY < Height; imgY++)
			{
				Array.Clear(Calc, 0, CalcSize);

				for (qY = 0; qY < Quality; qY++, y += dy)
				{
					srcY = (int)Math.Floor(y);
					yOutside = srcY < 0 || srcY >= this.size;

					for (imgX = 0, x = x0, j = 0; imgX < Width; imgX++, j += 4)
					{
						for (qX = 0; qX < Quality; qX++, x += dx)
						{
							srcX = (int)Math.Floor(x);

							if (yOutside || srcX < 0 || srcX >= this.size)
								Dot = DotType.CodeBackground;
							else
								Dot = this.dots[srcY, srcX];

							cl = Color(x * scale, y * scale, x - srcX, y - srcY, Dot);

							Calc[j] += (byte)cl;
							cl >>= 8;
							Calc[j + 1] += (byte)cl;
							cl >>= 8;
							Calc[j + 2] += (byte)cl;
							cl >>= 8;
							Calc[j + 3] += (byte)cl;
						}
					}
				}

				for (imgX = 0, j = 0; imgX < Width; imgX++)
				{
					Result[i++] = (byte)((Calc[j++] + q2r) / q2);
					Result[i++] = (byte)((Calc[j++] + q2r) / q2);
					Result[i++] = (byte)((Calc[j++] + q2r) / q2);
					Result[i++] = (byte)((Calc[j++] + q2r) / q2);
				}
			}

			return Result;
		}
	}
}
