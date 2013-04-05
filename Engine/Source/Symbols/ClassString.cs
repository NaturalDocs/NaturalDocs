/* 
 * Struct: GregValure.NaturalDocs.Engine.Symbols.ClassString
 * ____________________________________________________________________________
 * 
 * A struct encapsulating a class string, which is a normalized way of representing what class a given
 * topic is in.  This also covers databases and any other hierarchy that uses a class ID.
 * 
 * The encoding uses <SeparatorChars.Level2> since it encapsulates a <SymbolString> which uses 
 * <SeparatorChars.Level1>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Symbols
	{
	public struct ClassString : IComparable, Collections.ILookupKey
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: HierarchyType
		 * 
		 * Which hierarchy the ClassString is for.  The numeric values match the values in <CodeDB.Classes.Hierarchy>.
		 * 
		 * Class - The class hierarchy.
		 * Database - The database hierarchy.
		 */
		public enum HierarchyType : byte
			{  
			Class = 1, 
			Database = 2  
			}


		
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
		private ClassString (string newClassString, string newLookupKey)
			{
			classString = newClassString;
			lookupKey = newLookupKey;
			}

			
		/* Function: FromParameters
		 * Creates a ClassString from the passed parameters.
		 */
		static public ClassString FromParameters (HierarchyType hierarchy, int languageID, bool caseSensitive, SymbolString symbol)
			{
			if (symbol == null)
				{  throw new NullReferenceException();  }

			// SymbolString plus hierarchy, language ID, and separator.  It's almost definitely only going to use one char for the
			// language ID, but getting room for a second one just to be certain isn't a big deal when we're already paying for the
			// allocation.
			StringBuilder stringBuilder = new System.Text.StringBuilder(symbol.ToString().Length + 4);

			if (hierarchy == HierarchyType.Class)
				{  
				if (caseSensitive)
					{  stringBuilder.Append('C');  }
				else
					{  stringBuilder.Append('c');  }
				}
			else // (hierarchy == HierarchyType.Database)
				{  
				if (caseSensitive)
					{  stringBuilder.Append('D');  }
				else
					{  stringBuilder.Append('d');  }
				}

			do
				{
				int value = languageID & 0x0000003F;

				if (value < 10)
					{  stringBuilder.Append((char)('0' + value));  }
				else if (value < 36)
					{  stringBuilder.Append((char)('A' + (value - 10)));  }
				else if (value < 62)
					{  stringBuilder.Append((char)('a' + (value - 36)));  }
				else if (value == 62)
					{  stringBuilder.Append('!');  }
				else // (value == 63)
					{  stringBuilder.Append('@');  }

				languageID >>= 6;
				}
			while (languageID > 0);

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
				stringBuilder.Append(symbolString.ToLower());
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

			if (classString[0] >= 'A' && classString[0] <= 'Z')
				{  lookupKey = classString;  }
			else
				{
				StringBuilder stringBuilder = new StringBuilder(classString.Length);

				int separatorIndex = classString.IndexOf(SeparatorChar);
				stringBuilder.Append(classString, 0, separatorIndex + 1);

				// Turning the entire thing to lowercase requires one allocation and only adds a few extra characters of work.
				// If we extracted a substring then turned that into lowercase it would require two allocations.
				string lowercaseClassString = classString.ToLower();
				stringBuilder.Append(lowercaseClassString, separatorIndex + 1, lowercaseClassString.Length - (separatorIndex + 1));

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

				int separatorIndex = classString.IndexOf(SeparatorChar);

				return SymbolString.FromExportedString( classString.Substring(separatorIndex + 1) );
				}
			}


		/* Property: Hierarchy
		 * Which hierarchy the class is a part of.
		 */
		public HierarchyType Hierarchy
			{
			get
				{
				if (classString == null || classString[0] == 'C' || classString[0] == 'c')
					{  return HierarchyType.Class;  }
				else if (classString[0] == 'D' || classString[0] == 'd')
					{  return HierarchyType.Database;  }
				else
					{  throw new FormatException();  }
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

				int languageID = 0;
				int index = 1;

				do
					{
					char currentChar = classString[index];
					int value;

					if (currentChar >= '0' && currentChar <= '9')
						{  value = currentChar - '0';  }
					else if (currentChar >= 'A' && currentChar <= 'Z')
						{  value = 10 + (currentChar - 'A');  }
					else if (currentChar >= 'a' && currentChar <= 'z')
						{  value = 36 + (currentChar - 'a');  }
					else if (currentChar == '!')
						{  value = 62;  }
					else // (currentChar == '@')
						{  value = 63;  }

					value <<= (index - 1) * 6;
					languageID |= value;

					index++;
					}
				while (classString[index] != SeparatorChar);

				return languageID;
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
		 * - The first character will be 'C' or 'c' for class or 'D' or 'd' for database.
		 *   - Uppercase means the language is case sensitive, lowercase means it's not.
		 * - Next will be the language ID encoded in base64 using the following charset: 0-9, A-Z, a-z, !, @.
		 *   - The encoding is little endian, so the first characters are the lowest order bits.  This is just for ease
		 *      of encoding since it's unlikely we'll need more than one char in practice.
		 *   - This charset is used instead of standard base64 to make it easier to read, since it mimics decimal
		 *      and hex at low values.
		 *   - Base64 is used instead of hex because it's more compact and is unlikely to require more than one
		 *      character in practice.
		 * - Next will be <SeparatorChar>, which determines when the base64 ends.
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