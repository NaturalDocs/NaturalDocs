/*
 * Class: CodeClear.NaturalDocs.Engine.Links.Link
 * ____________________________________________________________________________
 *
 * A class encapsulating all the information available about a link.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public class Link
		{

		// Group: Types
		// __________________________________________________________________________

		/* Enum: IgnoreFields
		 *
		 * When querying links from the database, not all fields may be needed in all circumstances.  This is a
		 * bitfield that allows you to specify which fields can be ignored.  This is also stored in the object so that,
		 * in debug builds, if you try to access any of these fields an exception will be thrown.
		 */
		[Flags]
		public enum IgnoreFields : ushort
			{
			None = 0x0000,

			LinkID = 0x0001,
			Type = 0x0002,
			TextOrSymbol = 0x0004,
			Context = 0x0008,
			ContextID = 0x0010,
			FileID = 0x0020,
			ClassString = 0x0040,
			ClassID = 0x0080,
			LanguageID = 0x0100,
			EndingSymbol = 0x0200,
			TargetTopicID = 0x0400,
			TargetClassID = 0x0800,
			TargetScore = 0x1000
			}



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
			classString = new ClassString();
			classID = 0;
			languageID = 0;
			endingSymbol = new EndingSymbol();
			targetTopicID = 0;
			targetClassID = 0;
			targetScore = 0;

			ignoredFields = IgnoreFields.None;
			}


		/* Function: SameIdentifyingPropertiesAs
		 * Returns whether the identifying properties of the link (<Type>, <TextOrSymbol>, <Context>, <ClassString>, <FileID>,
		 * <LanguageID>) are the same as the passed one.
		 */
		public bool SameIdentifyingPropertiesAs (Link other)
			{
			return (CompareIdentifyingPropertiesTo(other) == 0);
			}


		/* Function: CompareIdentifyingPropertiesTo
		 *
		 * Compares the identifying properties of the link (<Type>, <TextOrSymbol>, <Context>, <ClassString>, <FileID>,
		 * <LanguageID>) and returns a value similar to a string comparison result which is suitable for sorting a list of Links.  It
		 * will return zero if all the properties are equal.
		 */
		public int CompareIdentifyingPropertiesTo (Link other)
			{
			// DEPENDENCY: What CopyNonIdentifyingPropertiesFrom() does depends on what this compares.

			#if DEBUG
			if (ignoredFields != IgnoreFields.None || other.ignoredFields != IgnoreFields.None)
				{  throw new InvalidOperationException("Cannot compare links that have ignored fields.");  }
			#endif

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

			if (classString == null)
				{
				if (other.classString == null)
					{  result = 0;  }
				else
					{  result = -1;  }
				}
			else
				{
				if (other.classString == null)
					{  result = 1;  }
				else
					{  result = classString.ToString().CompareTo(other.classString.ToString());  }
				}

			if (result != 0)
				{  return result;  }

			if (type != other.type)
				{  return (type - other.type);  }

			if (fileID != other.fileID)
				{  return (fileID - other.fileID);  }

			return (languageID - other.languageID);
			}


		/* Function: CopyNonIdentifyingPropertiesFrom
		 * Makes this link copy all the properties not tested by <CompareIdentifyingPropertiesTo()> and
		 * <SameIdentifyingPropertiesAs()> from the passed link.
		 */
		public void CopyNonIdentifyingPropertiesFrom (Link other)
			{
			// DEPENDENCY: What this copies depends on what CompareIdentifyingPropertiesTo() does not.

			linkID = other.LinkID;
			contextID = other.ContextID;
			classID = other.ClassID;
			endingSymbol = other.EndingSymbol;
			targetTopicID = other.TargetTopicID;
			targetClassID = other.TargetClassID;
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
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.LinkID) != 0)
					{  throw new InvalidOperationException("Tried to access LinkID when that field was ignored.");  }
				#endif

				return linkID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.LinkID) != 0)
					{  throw new InvalidOperationException("Tried to access LinkID when that field was ignored.");  }
				#endif

				linkID = value;
				}
			}


		/* Property: Type
		 * The links' type.
		 */
		public LinkType Type
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Type) != 0)
					{  throw new InvalidOperationException("Tried to access Type when that field was ignored.");  }
				#endif

				return type;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Type) != 0)
					{  throw new InvalidOperationException("Tried to access Type when that field was ignored.");  }
				#endif

				type = value;
				}
			}


		/* Property: TextOrSymbol
		 * If <Type> is <LinkType.NaturalDocs>, this will be the plain text of the link.  If it's any other <LinkType>, this will
		 * be the exported <Symbols.SymbolString> of the link text.  If the calling code knows exactly which type of link it
		 * is, it can use the <Text> or <Symbol> properties instead.
		 */
		public string TextOrSymbol
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TextOrSymbol) != 0)
					{  throw new InvalidOperationException("Tried to access TextOrSymbol when that field was ignored.");  }
				#endif

				return textOrSymbol;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TextOrSymbol) != 0)
					{  throw new InvalidOperationException("Tried to access TextOrSymbol when that field was ignored.");  }
				#endif

				textOrSymbol = value;
				}
			}


		/* Property: Text
		 * If <Type> is <LinkType.NaturalDocs>, this will be the plain text of the link.  Throws an exception if you try to use
		 * this property for any other <LinkType>.
		 */
		public string Text
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TextOrSymbol) != 0)
					{  throw new InvalidOperationException("Tried to access Text when that field was ignored.");  }
				#endif

				if (type != LinkType.NaturalDocs)
					{  throw new InvalidOperationException();  }

				return textOrSymbol;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TextOrSymbol) != 0)
					{  throw new InvalidOperationException("Tried to access Text when that field was ignored.");  }
				#endif

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
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TextOrSymbol) != 0)
					{  throw new InvalidOperationException("Tried to access Symbol when that field was ignored.");  }
				#endif

				if (type == LinkType.NaturalDocs)
					{  throw new InvalidOperationException();  }

				return SymbolString.FromExportedString(textOrSymbol);
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TextOrSymbol) != 0)
					{  throw new InvalidOperationException("Tried to access Symbol when that field was ignored.");  }
				#endif

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
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Context) != 0)
					{  throw new InvalidOperationException("Tried to access Context when that field was ignored.");  }
				#endif

				return context;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Context) != 0)
					{  throw new InvalidOperationException("Tried to access Context when that field was ignored.");  }
				#endif

				context = value;
				}
			}


		/* Property: ContextID
		 * The ID of <Context> if known, or zero if not.  It will also be zero if <Context> is null.
		 */
		public int ContextID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ContextID) != 0)
					{  throw new InvalidOperationException("Tried to access ContextID when that field was ignored.");  }
				#endif

				return contextID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ContextID) != 0)
					{  throw new InvalidOperationException("Tried to access ContextID when that field was ignored.");  }
				#endif

				contextID = value;
				}
			}


		/* Property: ContextIDKnown
		 * Whether <ContextID> is known, which basically tests whether <ContextID> is zero when <Context> is not null.
		 */
		public bool ContextIDKnown
			{
			get
				{  return (contextID != 0 || context == null);  }
			}


		/* Property: FileID
		 * The ID number of the file that defines this link
		 */
		public int FileID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.FileID) != 0)
					{  throw new InvalidOperationException("Tried to access FileID when that field was ignored.");  }
				#endif

				return fileID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.FileID) != 0)
					{  throw new InvalidOperationException("Tried to access FileID when that field was ignored.");  }
				#endif

				fileID = value;
				}
			}


		/* Property: ClassString
		 * The class the link appears in.
		 */
		public Symbols.ClassString ClassString
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ClassString) != 0)
					{  throw new InvalidOperationException("Tried to access ClassString when that field was ignored.");  }
				#endif

				return classString;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ClassString) != 0)
					{  throw new InvalidOperationException("Tried to access ClassString when that field was ignored.");  }
				#endif

				classString = value;
				}
			}


		/* Property: ClassID
		 * The ID of <ClassString> if known, or zero if not.  It will also be zero if <ClassString> is null.
		 */
		public int ClassID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ClassID) != 0)
					{  throw new InvalidOperationException("Tried to access ClassID when that field was ignored.");  }
				#endif

				return classID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ClassID) != 0)
					{  throw new InvalidOperationException("Tried to access ClassID when that field was ignored.");  }
				#endif

				classID = value;
				}
			}


		/* Property: ClassIDKnown
		 * Whether <ClassID> is known, which basically tests whether <ClassID> is zero when <ClassString> is not null.
		 */
		public bool ClassIDKnown
			{
			get
				{  return (classID != 0 || classString == null);  }
			}


		/* Property: LanguageID
		 * The ID number of the language of the topic that defines this link.
		 */
		public int LanguageID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.LanguageID) != 0)
					{  throw new InvalidOperationException("Tried to access LanguageID when that field was ignored.");  }
				#endif

				return languageID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.LanguageID) != 0)
					{  throw new InvalidOperationException("Tried to access LanguageID when that field was ignored.");  }
				#endif

				languageID = value;
				}
			}


		/* Property: EndingSymbol
		 * The ending symbol of the link.  If this is a Type or ClassParent link this is the only ending symbol, but if it's
		 * a Natural Docs link there may be more in <CodeDB.AlternativeLinkEndingSymbols>.
		 */
		public Symbols.EndingSymbol EndingSymbol
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.EndingSymbol) != 0)
					{  throw new InvalidOperationException("Tried to access EndingSymbol when that field was ignored.");  }
				#endif

				return endingSymbol;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.EndingSymbol) != 0)
					{  throw new InvalidOperationException("Tried to access EndingSymbol when that field was ignored.");  }
				#endif

				endingSymbol = value;
				}
			}


		/* Property: IsResolved
		 * Whether the link has a target topic.  Is equivalent to testing whether <TargetTopicID> is zero.
		 */
		public bool IsResolved
			{
			get
				{  return (targetTopicID != 0);  }
			}


		/* Property: TargetTopicID
		 * The ID number of the <Topic> the link resolves to, or zero if none.
		 */
		public int TargetTopicID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetTopicID) != 0)
					{  throw new InvalidOperationException("Tried to access TargetTopicID when that field was ignored.");  }
				#endif

				return targetTopicID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetTopicID) != 0)
					{  throw new InvalidOperationException("Tried to access TargetTopicID when that field was ignored.");  }
				#endif

				targetTopicID = value;
				}
			}


		/* Property: TargetClassID
		 * The class ID number of the <Topic> the link resolves to, or zero if none.
		 */
		public int TargetClassID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetClassID) != 0)
					{  throw new InvalidOperationException("Tried to access TargetClassID when that field was ignored.");  }
				#endif

				return targetClassID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetClassID) != 0)
					{  throw new InvalidOperationException("Tried to access TargetClassID when that field was ignored.");  }
				#endif

				targetClassID = value;
				}
			}


		/* Property: TargetScore
		 * If this link is resolved, the numeric score of the match.
		 */
		public long TargetScore
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetScore) != 0)
					{  throw new InvalidOperationException("Tried to access TargetScore when that field was ignored.");  }
				#endif

				return targetScore;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetScore) != 0)
					{  throw new InvalidOperationException("Tried to access TargetScore when that field was ignored.");  }
				#endif

				targetScore = value;
				}
			}


		/* Property: TargetInterpretationIndex
		 * If this link is resolved and it's a Natural Docs link, the index into its <LinkInterpretations> that was used.
		 */
		public int TargetInterpretationIndex
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetScore) != 0)
					{  throw new InvalidOperationException("Tried to access TargetInterpretationIndex when TargetScore was ignored.");  }
				#endif

				// DEPENDENCY: This depends on the score format generated by Engine.Links.Manager.Score().

				// Relevant part of format:
				// -------- -------- -------- -------- ---IIIII I------- -------- --------
				// I - How high on the interpretation list (named/plural/possessive) the match is.

				return 63 - (int)( (targetScore & 0x000000001F800000) >> 23 );
				}
			}


		/* Property: IgnoredFields
		 *
		 * When querying links from the database, not all fields may be needed in all situations.  The database
		 * may accept <IgnoreFields> flags to skip retrieving parts of them.  If that's done, the flags should also
		 * be set here so that in debug builds an exception will be thrown if you try to access those properties.
		 *
		 * IgnoredFields defaults to <IgnoreFields.None> so that links created by parsing don't have to worry
		 * about them.
		 *
		 */
		public IgnoreFields IgnoredFields
			{
			get
				{  return ignoredFields;  }
			set
				{  ignoredFields = value;  }
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

		/* var: classString
		 * The class this link appears in.
		 */
		protected Symbols.ClassString classString;

		/* var: classID
		 * The ID of <classString> if known, or zero if not.
		 */
		protected int classID;

		/* var: languageID
		 * The ID number of the language of the topic that defines this link.
		 */
		protected int languageID;

		/* var: endingSymbol
		 * The <EndingSymbol> of the link.  If this is a Type or ClassParent link this is the only one needed, but if it's
		 * a Natural Docs link there may be more in <CodeDB.AlternativeLinkEndingSymbols>.
		 */
		protected Symbols.EndingSymbol endingSymbol;

		/* var: targetTopicID
		 * The ID number of the <Topic> the link resolves to, or zero if none.
		 */
		protected int targetTopicID;

		/* var: targetClassID
		 * The class ID of the <Topic> the link resolves to, or zero if none.
		 */
		protected int targetClassID;

		/* var: targetScore
		 * If <targetTopicID> is set, the numeric score of the match.
		 */
		protected long targetScore;

		/* var: ignoredFields
		 * The <IgnoreFields> applied to this object.
		 */
		protected IgnoreFields ignoredFields;
		}
	}
