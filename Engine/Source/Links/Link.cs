/* 
 * Class: GregValure.NaturalDocs.Engine.Links.Link
 * ____________________________________________________________________________
 * 
 * A class encapsulating all the information available about a link.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine.Links
	{
	public class Link
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public Link ()
			{
			linkID = 0;
			type = LinkType.NaturalDocs;
			textOrSymbol = null;
			context = new ContextString();
			contextID = 0;
			fileID = 0;
			languageID = 0;
			endingSymbol = new EndingSymbol();
			targetTopicID = 0;
			targetScore = 0;
			}
			
			
		/* Function: SameIDPropertiesAs
		 * Returns whether the identifying properties of the link (<Type>, <TextOrSymbol>, <Context>, <FileID>, <LanguageID>)
		 * are the same as the passed one.
		 */
		public bool SameIDPropertiesAs (Link other)
			{
			return (CompareIDPropertiesTo(other) == 0);
			}


		/* Function: CompareIDPropertiesTo
		 * 
		 * Compares the identifying properties of the link (<Type>, <TextOrSymbol>, <Context>, <FileID>, <LanguageID>)
		 * and returns a value similar to a string comparison result which is suitable for sorting a list of Links.  It will return zero
		 * if all the properties are equal.
		 */
		public int CompareIDPropertiesTo (Link other)
			{
			// DEPENDENCY: What CopyNonIDPropertiesFrom() does depends on what this compares.

			int result = textOrSymbol.CompareTo(other.textOrSymbol);

			if (result != 0)
				{  return result;  }

			if (context == null)
				{
				if (other.context == null)
					{  result = 0;  }
				else
					{  result = -1;  }
				}
			else 
				{
				if (other.context == null)
					{  result = 1;  }
				else
					{  result = context.ToString().CompareTo(other.context.ToString());  }
				}

			if (result != 0)
				{  return result;  }

			if (type != other.type)
				{  return (type - other.type);  }

			if (fileID != other.fileID)
				{  return (fileID - other.fileID);  }

			return (languageID - other.languageID);
			}


		/* Function: CopyNonIDPropertiesFrom
		 * Makes this link copy all the properties not tested by <CompareIDPropertiesTo()> and <SameIDPropertiesAs()> from 
		 * the passed link.
		 */
		public void CopyNonIDPropertiesFrom (Link other)
			{
			// DEPENDENCY: What this copies depends on what CompareIDPropertiesTo() does not.

			linkID = other.LinkID;
			contextID = other.ContextID;
			endingSymbol = other.EndingSymbol;
			targetTopicID = other.TargetTopicID;
			targetScore = other.TargetScore;
			}



		// Group: Properties
		// __________________________________________________________________________
		
			
		/* Property: LinkID
		 * The link's ID number, or zero if it hasn't been set.
		 */
		public int LinkID
			{
			get
				{  return linkID;  }
			set
				{  linkID = value;  }
			}
			
			
		/* Property: Type
		 * The links' type.
		 */
		public LinkType Type
			{
			get
				{  return type;  }
			set
				{  type = value;  }
			}
			
			
		/* Property: TextOrSymbol
		 * If <Type> is <LinkType.NaturalDocs>, this will be the plain text of the link.  If it's any other <LinkType>, this will
		 * be the exported <Symbols.SymbolString> of the link text.  If the calling code knows exactly which type of link it
		 * is, it can use the <Text> or <Symbol> properties instead.
		 */
		public string TextOrSymbol
			{
			get
				{  return textOrSymbol;  }
			set
				{  textOrSymbol = value;  }
			}


		/* Property: Text
		 * If <Type> is <LinkType.NaturalDocs>, this will be the plain text of the link.  Throws an exception if you try to use
		 * this property for any other <LinkType>.
		 */
		public string Text
			{
			get
				{
				if (type != LinkType.NaturalDocs)
					{  throw new InvalidOperationException();  }

				return textOrSymbol;
				}
			set
				{
				if (type != LinkType.NaturalDocs)
					{  throw new InvalidOperationException();  }

				textOrSymbol = value;
				}
			}


		/* Property: Symbol
		 * If <Type> is anything other than <LinkType.NaturalDocs>, this will be the <Symbols.SymbolString> of the link text.
		 * Throws an exception if you try to use this property for <LinkType.NaturalDocs>.
		 */
		public Symbols.SymbolString Symbol
			{
			get
				{
				if (type == LinkType.NaturalDocs)
					{  throw new InvalidOperationException();  }

				return SymbolString.FromExportedString(textOrSymbol);
				}
			set
				{
				if (type == LinkType.NaturalDocs)
					{  throw new InvalidOperationException();  }

				textOrSymbol = value.ToString();
				}
			}
			
			
		/* Property: Context
		 * The context the link appears in.
		 */
		public Symbols.ContextString Context
			{
			get
				{  return context;  }
			set
				{  context = value;  }
			}


		/* Property: ContextID
		 * The ID of <Context> if known, or zero if not.
		 */
		public int ContextID
			{
			get
				{  return contextID;  }
			set
				{  contextID = value;  }
			}
			
			
		/* Property: FileID
		 * The ID number of the file that defines this link
		 */
		public int FileID
			{
			get
				{  return fileID;  }
			set
				{  fileID = value;  }
			}
			
			
		/* Property: LanguageID
		 * The ID number of the language of the topic that defines this link.
		 */
		public int LanguageID
			{
			get
				{  return languageID;  }
			set
				{  languageID = value;  }
			}


		/* Property: EndingSymbol
		 * The ending symbol of the link.  If this is a Type or ClassParent link this is the only one needed, but if it's
		 * a Natural Docs link there may be more in <CodeDB.AlternateLinkEndingSymbols>.
		 */
		public Symbols.EndingSymbol EndingSymbol
			{
			get
				{  return endingSymbol;  }
			set
				{  endingSymbol = value;  }
			}


		/* Property: TargetTopicID
		 * The ID number of the <Topic> the link resolves to, or zero if none or it hasn't been determined yet.
		 */
		public int TargetTopicID
			{
			get
				{  return targetTopicID;  }
			set
				{  targetTopicID = value;  }
			}


		/* Property: TargetScore
		 * If <TargetTopicID> is set, the numeric score of the match.
		 */
		public long TargetScore
			{
			get
				{  return targetScore;  }
			set
				{  targetScore = value;  }
			}			
			

		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: linkID
		 * The links's ID number, or zero if not specified.
		 */
		protected int linkID;
		
		/* var: type
		 * The <LinkType>.
		 */
		protected LinkType type;

		/* var: textOrSymbol
		 * If <type> is <LinkType.NaturalDocs>, this will be the plain text of the link.  If it's any other <LinkType>
		 * it will be the exported <Symbols.SymbolString> of the link text.
		 */
		protected string textOrSymbol;
		
		/* var: context
		 * The context the link appears in.
		 */
		protected Symbols.ContextString context;

		/* var: contextID
		 * The ID of <context> if known, or zero if not.
		 */
		protected int contextID;
		
		/* var: fileID
		 * The ID number of the file that defines this link.
		 */
		protected int fileID;
		
		/* var: languageID
		 * The ID number of the language of the topic that defines this link.
		 */
		protected int languageID;

		/* var: endingSymbol
		 * The <EndingSymbol> of the link.  If this is a Type or ClassParent link this is the only one needed, but if it's
		 * a Natural Docs link there may be more in <CodeDB.AlternateLinkEndingSymbols>.
		 */
		protected Symbols.EndingSymbol endingSymbol;

		/* var: targetTopicID
		 * The ID number of the <Topic> the link resolves to, or zero if none or it hasn't been determined yet.
		 */
		protected int targetTopicID;

		/* var: targetScore
		 * If <targetTopicID> is set, the numeric score of the match.
		 */
		protected long targetScore;
				
		}
	}
