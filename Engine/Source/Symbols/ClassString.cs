/*
 * Struct: CodeClear.NaturalDocs.Engine.Symbols.ClassString
 * ____________________________________________________________________________
 *
 * A struct encapsulating a class string, which is a normalized way of representing what class a given
 * topic is in.  This also covers databases and any other hierarchy that uses a class ID.
 *
 * The encoding uses <SeparatorChars.Level2> since it encapsulates a <SymbolString> which uses
 * <SeparatorChars.Level1>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Globalization;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Symbols
	{
	public struct ClassString : IComparable, Collections.ILookupKey
		{

		// Group: Constants
		// __________________________________________________________________________

		/* Constant: SeparatorChar
		 * The character used to separate string components.
		 */
		public const char SeparatorChar = SeparatorChars.Level2;



		// Group: Functions
		// __________________________________________________________________________


		/* Function: ClassString
		 */
		private ClassString (string classString, string lookupKey)
			{
			this.classString = classString;
			this.lookupKey = lookupKey;
			}


		/* Function: FromParameters
		 * Creates a ClassString from the passed parameters.
		 */
		static public ClassString FromParameters (int hierarchyID, int languageID, bool caseSensitive, SymbolString symbol)
			{
			if (symbol == null)
				{  throw new NullReferenceException();  }

			// SymbolString plus case-sensitivity char, hierarchy ID, language ID, and separators.  It's almost definitely only
			// going to use one char each of the IDs, but getting room for a second one just to be certain isn't a big deal when
			// we're already paying for the allocation.
			StringBuilder stringBuilder = new System.Text.StringBuilder(symbol.ToString().Length + 7);

			if (caseSensitive)
				{  stringBuilder.Append('C');  }
			else
				{  stringBuilder.Append('i');  }

			AppendBase64Int(hierarchyID, stringBuilder);
			stringBuilder.Append(SeparatorChar);

			AppendBase64Int(languageID, stringBuilder);
			stringBuilder.Append(SeparatorChar);

			string symbolString = symbol.ToString();
			stringBuilder.Append(symbolString);

			string classString = stringBuilder.ToString();
			string lookupKey;

			if (caseSensitive)
				{  lookupKey = classString;  }
			else
				{
				stringBuilder.Remove(stringBuilder.Length - symbolString.Length, symbolString.Length);
				stringBuilder.Append(symbolString.ToLower(CultureInfo.InvariantCulture));
				lookupKey = stringBuilder.ToString();
				}

			return new ClassString(classString, lookupKey);
			}


		/* Function: FromExportedString
		 * Creates a ClassString from the passed string which originally came from another ClassString object.  This assumes the
		 * string is already be in the proper format.  Only use this when retrieving ClassStrings that were stored as plain text
		 * in a database or other data file.
		 */
		static public ClassString FromExportedString (string exportedClassString)
			{
			if (exportedClassString == null || exportedClassString.Length == 0)
				{  return new ClassString(null, null);  }

			string classString = exportedClassString;
			string lookupKey;

			if (classString[0] == 'C')
				{  lookupKey = classString;  }
			else
				{
				StringBuilder stringBuilder = new StringBuilder(classString.Length);

				// We can start at 2 because there's the case-sensitivity char plus the hierarchy ID will always take at least one char.
				int separator1Index = classString.IndexOf(SeparatorChar, 2);

				// We can use +2 because there's the separator char plus the language ID will always take at least one char.
				int separator2Index = classString.IndexOf(SeparatorChar, separator1Index + 2);

				stringBuilder.Append(classString, 0, separator2Index + 1);

				// Turning the entire thing to lowercase requires one allocation and only adds a few extra characters of work.
				// If we extracted a substring then turned that into lowercase it would require two allocations.
				string lowercaseClassString = classString.ToLower(CultureInfo.InvariantCulture);
				stringBuilder.Append(lowercaseClassString, separator2Index + 1, lowercaseClassString.Length - (separator2Index + 1));

				lookupKey = stringBuilder.ToString();
				}

			return new ClassString(classString, lookupKey);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Symbol
		 * The class as a <SymbolString>.
		 */
		public SymbolString Symbol
			{
			get
				{
				if (classString == null)
					{  return new SymbolString();  }

				// We can start at 2 because there's the case-sensitivity char plus the hierarchy ID will always take at least one char.
				int separator1Index = classString.IndexOf(SeparatorChar, 2);

				// We can use +2 because there's the separator char plus the language ID will always take at least one char.
				int separator2Index = classString.IndexOf(SeparatorChar, separator1Index + 2);

				return SymbolString.FromExportedString( classString.Substring(separator2Index + 1) );
				}
			}


		/* Property: HierarchyID
		 * Which hierarchy the class is a part of.
		 */
		public int HierarchyID
			{
			get
				{
				if (classString == null)
					{  return 0;  }

				// We start at 1 because of the case-sensitivity char.
				int value = DecodeBase64Int(classString, 1);

				return value;
				}
			}


		/* Property: LanguageID
		 * The ID of the language the class is associated with, or zero if it's irrelevant.
		 */
		public int LanguageID
			{
			get
				{
				if (classString == null)
					{  return 0;  }

				// We can start at 2 because there's the case-sensitivity char plus the hierarchy ID will always take at least one
				// char.
				int separator1Index = classString.IndexOf(SeparatorChar, 2);

				int value = DecodeBase64Int(classString, separator1Index + 1);

				return value;
				}
			}


		/* Property: CaseSensitive
		 *
		 * Fooled you.  There actually is no CaseSensitive property.  You don't have to worry about handling case sensitivity
		 * yourself, just use <LookupKey> for comparisons instead.
		 *
		 * Why is there no CaseSensitive property?  Because if there was somebody might mistakenly take it to mean they
		 * should lowercase the string for comparisons instead of using <LookupKey>.  That's bad, as the part that encodes
		 * <LanguageID> is always case sensitive and would be corrupted.  So this note serves as a warning for the people
		 * who might be looking for it.
		 */


		/* Property: LookupKey
		 * The key to use with <CodeDB.IDLookupCache>.
		 */
		public string LookupKey
			{
			get
				{  return lookupKey;  }
			}



		// Group: Private Functions
		// __________________________________________________________________________


		/* Function: AppendBase64Int
		 *
		 * Appends a base64-encoded integer to the StringBuilder.  Remember to append a <SeparatorChar>
		 * afterwards since it's required by <DecodeBase64Int()>.
		 *
		 * Format:
		 *
		 *   - The integer is encoded in base64 using the following charset: 0-9, A-Z, a-z, !, @.
		 *   - The encoding is little endian, so the first characters are the lowest order bits.  This is just for ease
		 *      of encoding since it's unlikely we'll need more than one character in practice.
		 *   - This charset is used instead of standard base64 to make it easier to read, since it mimics decimal
		 *      and hex at low values.
		 *   - Base64 is used instead of hex because it's more compact and is unlikely to require more than one
		 *      character in practice.
		 */
		static private void AppendBase64Int (int value, StringBuilder stringBuilder)
			{
			do
				{
				int codon = value & 0x0000003F;

				if (codon < 10)
					{  stringBuilder.Append((char)('0' + codon));  }
				else if (codon < 36)
					{  stringBuilder.Append((char)('A' + (codon - 10)));  }
				else if (codon < 62)
					{  stringBuilder.Append((char)('a' + (codon - 36)));  }
				else if (codon == 62)
					{  stringBuilder.Append('!');  }
				else // (value == 63)
					{  stringBuilder.Append('@');  }

				value >>= 6;
				}
			while (value > 0);
			}


		/* Function: DecodeBase64Int
		 * Decodes a base64-encoded integer from the string.  It starts at the passed index and ends at the next <SeparatorChar>.
		 */
		static private int DecodeBase64Int (string text, int index = 0)
			{
			if (text == null)
				{  return 0;  }

			int result = 0;

			while (text[index] != SeparatorChar)
				{
				char codon = text[index];
				int codonValue;

				if (codon >= '0' && codon <= '9')
					{  codonValue = codon - '0';  }
				else if (codon >= 'A' && codon <= 'Z')
					{  codonValue = 10 + (codon - 'A');  }
				else if (codon >= 'a' && codon <= 'z')
					{  codonValue = 36 + (codon - 'a');  }
				else if (codon == '!')
					{  codonValue = 62;  }
				else // (codon == '@')
					{  codonValue = 63;  }

				result <<= 6;
				result |= codonValue;

				index++;
				}

			return result;
			}



		// Group: Operators
		// __________________________________________________________________________


		/* operator: operator string
		 * A cast operator to covert the ClassString to a string.
		 */
		public static implicit operator string (ClassString classString)
			{
			return classString.classString;
			}

		/* Operator: operator ==
		 * This compares using <LookupKey> instead of <ToString()>.
		 */
		public static bool operator== (ClassString a, object b)
			{
			// We need to make the operator compare against object intead of another ClassString in order to support
			// directly comparing against null.
			return a.Equals(b);
			}

		/* Operator: operator !=
		 * This compares using <LookupKey> instead of <ToString()>.
		 */
		public static bool operator!= (ClassString a, object b)
			{
			return !(a.Equals(b));
			}

		/* Function: ToString
		 * Returns the ContextString as a string.  This is always case sensitive, unlike <LookupKey>.
		 */
		public override string ToString ()
			{
			return classString;
			}

		/* Function: GetHashCode
		 * This is generated from <LookupKey> instead of <ToString()>.
		 */
		public override int GetHashCode ()
			{
			if (lookupKey == null)
				{  return 0;  }
			else
				{  return lookupKey.GetHashCode();  }
			}

		/* Function: Equals
		 * This compares using <LookupKey> instead of <ToString()>.
		 */
		public override bool Equals (object other)
			{
			if (other == null)
				{  return (lookupKey == null);  }
			else if (other is ClassString)
				{  return (lookupKey == ((ClassString)other).lookupKey);  }
			else if (other is string)
				{  return (lookupKey == (string)other);  }
			else
				{  return false;  }
			}

		/* Function: CompareTo
		 * This compares using <LookupKey> instead of <ToString()>.
		 */
		public int CompareTo (object other)
			{
			return lookupKey.CompareTo(other);
			}



		// Group: Variables
		// __________________________________________________________________________


		/* string: classString
		 *
		 * The combined class string.
		 *
		 * - First will be the character 'C' or 'i' depending on whether it's case-sensitive or not.
		 * - Next will be the hierarchy ID encoded in base64, followed by a <SeparatorChar>.
		 * - Next will be the language ID encoded in base64, followed by a <SeparatorChar>.
		 * - Next will be an embedded <SymbolString> representing the class.
		 */
		private string classString;

		/* string: lookupKey
		 * If the language is case sensitive, this will be the same as <classString>.  If it's not, this will be <classString>
		 * with the <SymbolString> part in lowercase.
		 */
		private string lookupKey;

		}
	}
