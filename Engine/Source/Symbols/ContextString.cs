/*
 * Struct: CodeClear.NaturalDocs.Engine.Symbols.ContextString
 * ____________________________________________________________________________
 *
 * A struct encapsulating a context string, which is a normalized way of representing what scope and "using"
 * statements are active at a given point.
 *
 * The encoding uses <SeparatorChars.Level3> since it encapsulates <UsingStrings> which use
 * <SeparatorChars.Level2>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Symbols
	{
	public struct ContextString : IComparable, Collections.ILookupKey
		{
		// Group: Constants
		// __________________________________________________________________________

		/* Constant: SeparatorChar
		 * The character used to separate string components.
		 */
		public const char SeparatorChar = SeparatorChars.Level3;



		// Group: Functions
		// __________________________________________________________________________


		/* Function: ContextString
		 */
		private ContextString (string newContextString)
			{
			contextString = newContextString;
			}


		/* Function: FromExportedString
		 * Creates a ContextString from the passed string which originally came from another ContextString object.  This assumes
		 * the string is already be in the proper format.  Only use this when retrieving ContextStrings that were stored as plain text
		 * in a database or other data file.
		 */
		static public ContextString FromExportedString (string exportedContextString)
			{
			if (exportedContextString != null && exportedContextString.Length == 0)
				{  exportedContextString = null;  }

			return new ContextString(exportedContextString);
			}


		/* Function: GetUsingStatements
		 * Returns the "using" statements as a list, or null if there are none.
		 */
		public IList<UsingString> GetUsingStatements()
			{
			if (contextString == null)
				{  return null;  }

			int separatorIndex = contextString.IndexOf(SeparatorChar);

			if (separatorIndex == -1)
				{  return null;  }

			List<UsingString> usingStatements = new List<UsingString>();

			for (;;)
				{
				int nextSeparatorIndex = contextString.IndexOf(SeparatorChar, separatorIndex + 1);

				if (nextSeparatorIndex == -1)
					{
					usingStatements.Add( UsingString.FromExportedString( contextString.Substring(separatorIndex + 1) ) );
					break;
					}
				else
					{
					usingStatements.Add( UsingString.FromExportedString(
						contextString.Substring(separatorIndex + 1, nextSeparatorIndex - (separatorIndex + 1))
						) );
					separatorIndex = nextSeparatorIndex;
					}
				}

			return usingStatements;
			}


 		/* Function: AddUsingStatement
		 * Adds a "using" statement to the context.  It does not remove or replace any of the existing ones.
		 */
		public void AddUsingStatement (UsingString usingStatement)
			{
			if (contextString == null)
				{  contextString = SeparatorChar + usingStatement;  }
			else
				{  contextString += SeparatorChar + usingStatement;  }
			}


		/* Function: InheritUsingStatementsFrom
		 * Adds all the "using" statements from the passed context to this one _before_ the existing ones.  It will not check for
		 * or remove duplicates.
		 */
		public void InheritUsingStatementsFrom (ContextString other)
			{
			if (other.HasUsingStatements == false)
				{  return;  }

			string otherUsingStatements;

			if (other.contextString[0] == SeparatorChar)
				{  otherUsingStatements = other.contextString;  }
			else
				{  otherUsingStatements = other.contextString.Substring( other.contextString.IndexOf(SeparatorChar) );  }

			if (contextString == null)
				{  contextString = otherUsingStatements;  }
			else if (contextString[0] == SeparatorChar)
				{  contextString = otherUsingStatements + contextString;  }
			else
				{
				int firstSeparator = contextString.IndexOf(SeparatorChar);

				if (firstSeparator == -1)
					{  contextString += otherUsingStatements;  }
				else
					{  contextString = contextString.Substring(0, firstSeparator) + otherUsingStatements + contextString.Substring(firstSeparator);  }
				}
			}


		/* Function: ClearUsingStatements
		 * Removes all "using" statements from the context.
		 */
		public void ClearUsingStatements()
			{
			if (contextString == null)
				{  return;  }

			int separatorIndex = contextString.IndexOf(SeparatorChar);

			if (separatorIndex == -1)
				{  return;  }
			else if (separatorIndex == 0)
				{  contextString = null;  }
			else
				{  contextString = contextString.Substring(0, separatorIndex);  }
			}


		/* Function: GetRawTextScope
		 * Retrieves the bounds of the scope part of <RawText>.  Returns true if there is a scope, false if it's global.
		 * This function is useful if you need to compare something to many ContextStrings and don't want to constantly
		 * create intermediate strings.
		 */
		public bool GetRawTextScope (out int index, out int length)
			{
			if (contextString == null || contextString[0] == SeparatorChar)
				{
				index = 0;
				length = 0;
				return false;
				}
			else
				{
				index = 0;
				length = contextString.IndexOf(SeparatorChar);

				if (length == -1)
					{  length = contextString.Length;  }

				return true;
				}
			}


		/* Function: GetRawTextUsingStatement
		 * Retrieves the <RawText> bounds of the selected "using" statement.  If there isn't a "using" statement for the passed
		 * index, it will return false.  This function is useful if you need to compare something against many ContextStrings and
		 * don't want to constantly create intermediate strings and lists.
		 */
		 public bool GetRawTextUsingStatement (int statementIndex, out int rawTextIndex, out int rawTextLength)
			{
			rawTextIndex = 0;
			rawTextLength = 0;

			if (contextString == null)
				{  return false;  }

			int count = statementIndex;
			int index = contextString.IndexOf(SeparatorChar);  // To get past the scope

			while (index != -1 && count > 0)
				{
				index = contextString.IndexOf(SeparatorChar, index + 1);
				count--;
				}

			if (index == -1)
				{  return false;  }

			rawTextIndex = index + 1;

			index = contextString.IndexOf(SeparatorChar, rawTextIndex);

			if (index == -1)
				{  rawTextLength = contextString.Length - rawTextIndex;  }
			else
				{  rawTextLength = index - rawTextIndex;  }

			return true;
			}


		// Group: Properties
		// __________________________________________________________________________


		/* Property: Scope
		 * The scope as a <SymbolString>, or null if global.
		 */
		public SymbolString Scope
			{
			get
				{
				if (contextString == null)
					{  return new SymbolString();  }

				int separatorIndex = contextString.IndexOf(SeparatorChar);

				if (separatorIndex == -1)
					{  return SymbolString.FromExportedString(contextString);  }
				else if (separatorIndex == 0)
					{  return new SymbolString();  }
				else
					{  return SymbolString.FromExportedString( contextString.Substring(0, separatorIndex) );  }
				}

			set
				{
				if (contextString == null)
					{
					contextString = value;
					return;
					}

				int separatorIndex = contextString.IndexOf(SeparatorChar);

				if (value == null)
					{
					if (separatorIndex == -1)
						{  contextString = null;  }
					else if (separatorIndex != 0)
						{  contextString = contextString.Substring(separatorIndex);  }
					}

				else // value != null
					{
					if (separatorIndex == -1)
						{  contextString = value;  }
					else if (separatorIndex == 0)
						{  contextString = value + contextString;  }
					else
						{  contextString = value + contextString.Substring(separatorIndex);  }
					}
				}
			}


		/* Property: ScopeIsGlobal
		 * Whether the scope is global.  This is more efficient than checking <Scope> against null as it doesn't have
		 * to create a <SymbolString> to return.
		 */
		public bool ScopeIsGlobal
			{
			get
				{
				if (contextString == null)
					{  return true;  }
				else if (contextString[0] == SeparatorChar)
					{  return true;  }
				else
					{  return false;  }
				}
			}


		/* Property: HasUsingStatements
		 * Whether the context contains using statements.
		 */
		public bool HasUsingStatements
			{
			get
				{
				return (contextString != null && contextString.IndexOf(SeparatorChar) != -1);
				}
			}


		/* Property: RawText
		 * Returns the raw string for use with functions like <GetRawTextScope()> and <GetRawTextUsingStatement()>.
		 */
		public string RawText
			{
			get
				{  return contextString;  }
			}


		/* Property: LookupKey
		 * The key to use with <CodeDB.IDLookupCache>.
		 */
		public string LookupKey
			{
			get
				{  return contextString;  }
			}



		// Group: Operators
		// __________________________________________________________________________


		/* operator: operator string
		 * A cast operator to covert the context to a string.
		 */
		public static implicit operator string (ContextString context)
			{
			return context.contextString;
			}

		/* Operator: operator ==
		 */
		public static bool operator== (ContextString a, object b)
			{
			// We need to make the operator compare against object intead of another ContextString in order to support
			// directly comparing against null.
			return a.Equals(b);
			}

		/* Operator: operator !=
		 */
		public static bool operator!= (ContextString a, object b)
			{
			return !(a.Equals(b));
			}

		/* Function: ToString
		 * Returns the ContextString as a string.
		 */
		public override string ToString ()
			{
			return contextString;
			}

		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			if (contextString == null)
				{  return 0;  }
			else
				{  return contextString.GetHashCode();  }
			}

		/* Function: Equals
		 */
		public override bool Equals (object other)
			{
			if (other == null)
				{  return (contextString == null);  }
			else if (other is ContextString)
				{  return (contextString == ((ContextString)other).contextString);  }
			else if (other is string)
				{  return (contextString == (string)other);  }
			else
				{  return false;  }
			}

		/* Function: CompareTo
		 */
		public int CompareTo (object other)
			{
			return contextString.CompareTo(other);
			}



		// Group: Variables
		// __________________________________________________________________________


		/* string: contextString
		 * The context string in normalized form.  The first segment separated by <SeparatorChar> is the scope, and
		 * each following segment is a "using" statement.
		 *
		 * - If the scope is global and there are no "using" statements, the string will be null.
		 * - If there is a scope but no "using" statements, the string will be the the scope.  It will not be followed by a
		 *   <SeparatorChar>.
		 * - If the scope is global and there are "using" statements, the string will be <SeparatorChar> followed by the
		 *   "using" statements.
		 * - If there is a scope and "using" statements", the string will be the scope, a <SeparatorChar>, and then the
		 *   "using" statements.
		 */
		private string contextString;

		}
	}
