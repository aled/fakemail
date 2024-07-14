using System;

namespace Fakemail.Core
{
    /// <summary>
    /// Copied from https://raw.githubusercontent.com/icsharpcode/SharpZipLib/master/src/ICSharpCode.SharpZipLib/Checksum/Adler32.cs
    /// (MIT license)
    /// </summary>
   
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0054:Use compound assignment")]
    public sealed class Adler32
	{
		#region Instance Fields

		/// <summary>
		/// largest prime smaller than 65536
		/// </summary>
		private static readonly uint BASE = 65521;

		/// <summary>
		/// The CRC data checksum so far.
		/// </summary>
		private uint checkValue;

		#endregion Instance Fields

		/// <summary>
		/// Initialise a default instance of <see cref="Adler32"></see>
		/// </summary>
		public Adler32()
		{
			Reset();
		}

		/// <summary>
		/// Resets the Adler32 data checksum as if no update was ever called.
		/// </summary>
		public void Reset()
		{
			checkValue = 1;
		}

		/// <summary>
		/// Returns the Adler32 data checksum computed so far.
		/// </summary>
		public long Value
		{
			get
			{
				return checkValue;
			}
		}

		/// <summary>
		/// Updates the checksum with the byte b.
		/// </summary>
		/// <param name="bval">
		/// The data value to add. The high byte of the int is ignored.
		/// </param>
		public void Update(int bval)
		{
			// We could make a length 1 byte array and call update again, but I
			// would rather not have that overhead
			uint s1 = checkValue & 0xFFFF;
			uint s2 = checkValue >> 16;

			s1 = (s1 + ((uint)bval & 0xFF)) % BASE;
			s2 = (s1 + s2) % BASE;

			checkValue = (s2 << 16) + s1;
		}

		/// <summary>
		/// Updates the Adler32 data checksum with the bytes taken from
		/// a block of data.
		/// </summary>
		/// <param name="buffer">Contains the data to update the checksum with.</param>
		public void Update(byte[] buffer)
		{
            ArgumentNullException.ThrowIfNull(buffer);

            Update(new ArraySegment<byte>(buffer, 0, buffer.Length));
		}

        /// <summary>
        /// Update Adler32 data checksum based on a portion of a block of data
        /// </summary>
        /// <param name = "segment">
        /// The chunk of data to add
        /// </param>
        public void Update(ArraySegment<byte> segment)
		{
			//(By Per Bothner)
			uint s1 = checkValue & 0xFFFF;
			uint s2 = checkValue >> 16;
			var count = segment.Count;
			var offset = segment.Offset;
			while (count > 0)
			{
				// We can defer the modulo operation:
				// s1 maximally grows from 65521 to 65521 + 255 * 3800
				// s2 maximally grows by 3800 * median(s1) = 2090079800 < 2^31
				int n = 3800;
				if (n > count)
				{
					n = count;
				}
				count -= n;
				while (--n >= 0)
				{
					s1 = s1 + (uint)(segment.Array[offset++] & 0xff);
					s2 = s2 + s1;
				}
				s1 %= BASE;
				s2 %= BASE;
			}
			checkValue = (s2 << 16) | s1;
		}
	}
}
