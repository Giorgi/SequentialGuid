﻿using System;
using System.Numerics;
using Sander.SequentialGuid.App;

namespace Sander.SequentialGuid
{
	/// <summary>
	///     Extension methods for GUID
	/// </summary>
	public static class GuidExtensions
	{
		/// <summary>
		///     Convert GUID to BigInteger
		///     <para>
		///         Defaults to isCompliant = true, as this is the more common use outside Microsoft/.NET.
		///         E.g. compatible with Python or Java UUID, and http://guid-convert.appspot.com.
		///         See also https://stackoverflow.com/questions/9195551/why-does-guid-tobytearray-order-the-bytes-the-way-it-does
		///     </para>
		/// </summary>
		public static BigInteger ToBigInteger(this Guid guid, bool isCompliant = true)
		{
			var bytes = isCompliant ? ToCompliantByteArray(guid) : guid.ToByteArray();

			//[...] the most significant bit of the last element in the byte array.
			//This bit is set (the value of the byte is 0xFF) if the array is created from a negative BigInteger value. The bit is not set (the value of the byte is zero)
			// To prevent positive values from being misinterpreted as negative values, you can add a zero-byte value to the end of the array.
			//from https://docs.microsoft.com/en-us/dotnet/api/system.numerics.biginteger.-ctor?view=netframework-4.7.2#System_Numerics_BigInteger__ctor_System_Byte___
			if (bytes[15] == 0xFF)
			{
				Array.Resize(ref bytes, 17);
				bytes[16] = 0x00;
			}

			return new BigInteger(bytes);
		}


		/// <summary>
		///     Convert GUID to pair of Int64s, sometimes used in languages without native GUID/UUID implementation (Javascript)
		///     <para>
		///         Note that two longs cannot hold GUID larger than ffffffff-ffff-7fff-ffff-ffffffffff7f,
		///         but that shouldn't be an issue in most realistic use cases
		///     </para>
		/// </summary>
		public static (long, long) ToLongs(this Guid guid)
		{
			GuidConverter.GuidToInt64(guid, out var first, out var second);
			return (first, second);
		}

		/// <summary>
		///     Get the hex character at the specified position (0..31).
		///     Character is returned in lowercase
		///     <para>Far faster and uses less memory than using guid-to-string</para>
		/// </summary>
		public static char GetCharacterAt(this Guid guid, int position)
		{
			if (position < 0 || position > 31)
				throw new ArgumentOutOfRangeException(nameof(position), position,
					$"Position must be between 0 and 31, but received {position}");

			//remap position to internal byte order, as characters 0..13 do not match the byte order
			var remap = position >> 1;
			switch (remap)
			{
				case 0:
					remap = 3;
					break;
				case 1:
					remap = 2;
					break;
				case 2:
					remap = 1;
					break;
				case 3:
					remap = 0;
					break;
				case 4:
					remap = 5;
					break;
				case 5:
					remap = 4;
					break;
				case 6:
					remap = 7;
					break;
				case 7:
					remap = 6;
					break;
			}

			var guidByte = new GuidBytes { Guid = guid }.GetByteAt(remap);

			//logic similar to https://github.com/dotnet/corefx/blob/7622efd2dbd363a632e00b6b95be4d990ea125de/src/Common/src/CoreLib/System/Guid.cs#L989,
			//but we're using nibble and not full byte
			var nibbleByte = (position % 2 == 0 ? (guidByte & 0xF0) >> 4 : guidByte & 0x0F) & 0xf;
			return (char)(nibbleByte > 9 ? nibbleByte + 87 : nibbleByte + 48);
		}

		/// <summary>
		///     Get byte from GUID without converting GUID to byte array. Position is the native position of the byte in GUID
		///     structure on Windows/.NET (0..15)
		///     <para>This is very slightly faster than using Guid.ToByteArray()[position], but uses far less memory</para>
		/// </summary>
		public static byte GetByteAt(this Guid guid, int position) =>
			new GuidBytes { Guid = guid }.GetByteAt(position);

		/// <summary>
		///     Get GUID as byte array compatible with common use outside Microsoft/.NET.
		///     <para>
		///         E.g. compatible with Python or Java UUID, and http://guid-convert.appspot.com.
		///         See also https://stackoverflow.com/questions/9195551/why-does-guid-tobytearray-order-the-bytes-the-way-it-does
		///     </para>
		/// </summary>
		public static byte[] ToCompliantByteArray(this Guid guid)
		{
			var converter = new GuidBytes { Guid = guid };
			return new[]
			{
				converter.B15,
				converter.B14,
				converter.B13,
				converter.B12,
				converter.B11,
				converter.B10,
				converter.B9,
				converter.B8,
				converter.B6,
				converter.B7,
				converter.B4,
				converter.B5,
				converter.B0,
				converter.B1,
				converter.B2,
				converter.B3
			};
		}
	}
}
