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

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Symbols
	{
	public struct ClassString : IComparable
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
		private ClassString (string newClassString)
			{
			classString = newClassString;
			}

			
		/* Function: FromParameters
		 * Creates a ClassString from the passed parameters.
		 */
		static public ClassString FromParameters (HierarchyType hierarchy, int languageID, SymbolString symbol)
			{
			if (symbol == null)
				{  throw new NullReferenceException();  }

			// SymbolString plus hierarchy, language ID, and separator.  It's almost definitely only going to use one char for the
			// language ID, but allocating a second one just to be certain isn't a big deal.
			System.Text.StringBuilder classString = new System.Text.StringBuilder(symbol.ToString().Length + 4);

			if (hierarchy == HierarchyType.Class)
				{  classString.Append('C');  }
			else // (hierarchy == HierarchyType.Database)
				{  classString.Append('D');  }

			do
				{
				int value = languageID & 0x0000003F;

				if (value < 10)
					{  classString.Append((char)('0' + value));  }
				else if (value < 36)
					{  classString.Append((char)('A' + (value - 10)));  }
				else if (value < 62)
					{  classString.Append((char)('a' + (value - 36)));  }
				else if (value == 62)
					{  classString.Append('!');  }
				else // (value == 63)
					{  classString.Append('@');  }

				languageID >>= 6;
				}
			while (languageID > 0);

			classString.Append(SeparatorChar);
			classString.Append(symbol.ToString());

			return new ClassString(classString.ToString());
			}


		/* Function: FromExportedString
		 * 
		 * Creates a ClassString from the passed string which originally came from another ClassString object.  This assumes the
		 * string is already be in the proper format.  Only use this when retrieving ClassStrings that were stored as plain text 
		 * in a database or other data file.
		 * 
		 * This throws an exception if <SeparatorChars.Escape> is the first character, as that signifies a special string that should
		 * not be interpreted as a ClassString.  Null is acceptable however.
		 */
		static public ClassString FromExportedString (string exportedClassString)
			{
			if (exportedClassString != null)
				{
				if (exportedClassString.Length == 0)
					{  exportedClassString = null;  }
				else if (exportedClassString[0] == SeparatorChars.Escape)
					{  throw new FormatException("You cannot convert an escaped string to a ClassString.");  }
				}

			return new ClassString(exportedClassString);
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
				if (classString == null || classString[0] == 'C')
					{  return HierarchyType.Class;  }
				else if (classString[0] == 'D')
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
		 */
		public static bool operator== (ClassString a, object b)
			{
			// We need to make the operator compare against object intead of another ClassString in order to support
			// directly comparing against null.
			return a.Equals(b);
			}

		/* Operator: operator !=
		 */
		public static bool operator!= (ClassString a, object b)
			{
			return !(a.Equals(b));
			}

		/* Function: ToString
		 * Returns the ContextString as a string.
		 */
		public override string ToString ()
			{
			return classString;
			}
			
		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			if (classString == null)
				{  return 0;  }
			else
				{  return classString.GetHashCode();  }
			}

		/* Function: Equals
		 */
		public override bool Equals (object other)
			{
			if (other == null)
				{  return (classString == null);  }
			else if (other is ClassString)
				{  return (classString == ((ClassString)other).classString);  }
			else if (other is string)
				{  return (classString == (string)other);  }
			else
				{  return false;  }
			}
			
		/* Function: CompareTo
		 */
		public int CompareTo (object other)
			{
			return classString.CompareTo(other);
			}
		
			
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* string: classString
		 * 
		 * The combined class string.
		 * 
		 * - The first character will be 'C' for class or 'D' for database.
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
	
		}
	}