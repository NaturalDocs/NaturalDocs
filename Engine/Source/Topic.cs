/* 
 * Class: GregValure.NaturalDocs.Engine.Topic
 * ____________________________________________________________________________
 * 
 * A class encapsulating all the information available about a topic.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine
	{
	public class Topic
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: DatabaseCompareResult
		 * 
		 * The result of two <Topics> being compared with <DatabaseCompare()>.
		 * 
		 *		Same - The topics are exactly the same.
		 *		Different - The topics are different in substantial ways.
		 *		Similar_WontAffectLinking - The topics differ in some fields, but in such a way as to not affect 
		 *														  linking if one were substituted for another.
		 */
		public enum DatabaseCompareResult : byte
			{  Same, Different, Similar_WontAffectLinking  }


		/* Enum: ChangeFlags
		 * A bitfield that represents all the properties that were changed.  Note that this only applies when
		 * <DatabaseCompareResult.Similar_WontAffectLinking> is returned.  However, to prevent coding mistakes,
		 * all fields will be set to true when <DatabaseCompareResult.Different> is returned.
		 */
		[Flags]
		public enum ChangeFlags : ushort
			{
			Title = 0x0001,
			Body = 0x0002,
			Summary = 0x0004,
			Prototype = 0x0008,
			Symbol = 0x0010,
			SymbolDefinitonNumber = 0x0020,

			TopicTypeID = 0x0040,
			AccessLevel = 0x0080,
			Tags = 0x0100,

			LanguageID = 0x0200,
			CommentLineNumber = 0x0400,
			CodeLineNumber = 0x0800,
			
			FileID = 0x1000,
			PrototypeContext = 0x2000,
			BodyContext = 0x4000,

			All = Title | Body | Summary | Prototype | Symbol | SymbolDefinitonNumber |
					 TopicTypeID | AccessLevel | Tags |
					 LanguageID | CommentLineNumber | CodeLineNumber |
					 FileID | PrototypeContext | BodyContext
			}


		/* Enum: IgnoreFields
		 * 
		 * When querying topics from the database, not all fields may be needed in all circumstances.  This is a
		 * bitfield that allows you to specify which fields can be ignored.  This is also stored in the object so that,
		 * in debug builds, if you try to access any of these fields an exception will be thrown.
		 * 
		 * Fields can be specified individually, but these precombined values are also available:
		 * 
		 *		OnlyContextIDs - Combines PrototypeContext and BodyContext but leaves their IDs in place.  If you're
		 *										comparing two topics from a database where the IDs are guaranteed to be set, this
		 *										prevents an extra lookup.
		 */
		[Flags]
		public enum IgnoreFields : uint
			{
			None = 0x00000000,

			TopicID = 0x00000001,
			Title = 0x00000002,
			Body = 0x00000004,

			Summary = 0x00000010,
			Prototype = 0x00000020,
			Symbol = 0x00000040,
			SymbolDefinitionNumber = 0x00000080,
			TopicTypeID = 0x00000100,
			AccessLevel = 0x00000200,
			Tags = 0x00000400,
			FileID = 0x00000800,
			CommentLineNumber = 0x00001000,
			CodeLineNumber = 0x00002000,
			LanguageID = 0x00004000,
			PrototypeContext = 0x00008000,
			PrototypeContextID = 0x00010000,
			BodyContext = 0x00020000,
			BodyContextID = 0x00040000,

			OnlyContextIDs = PrototypeContext | BodyContext,
			OnlyBodyLength = Body
			}


			
		// Group: Functions
		// __________________________________________________________________________
		
		
		public Topic ()
			{
			topicID = 0;

			title = null;
			body = null;
			summary = null;
			prototype = null;
			parsedPrototype = null;
			symbol = new SymbolString();
			symbolDefinitionNumber = 0;
			titleParameters = new ParameterString();
			titleParametersGenerated = false;
			prototypeParameters = new ParameterString();
			prototypeParametersGenerated = false;

			topicTypeID = 0;
			usesPluralKeyword = false;
			accessLevel = Languages.AccessLevel.Unknown;
			tags = null;

			languageID = 0;
			commentLineNumber = 0;
			codeLineNumber = 0;
			
			fileID = 0;
			prototypeContext = new ContextString();
			prototypeContextID = 0;
			bodyContext = new ContextString();
			bodyContextID = 0;

			ignoredFields = IgnoreFields.None;
			}
			
			
		/* Function: DatabaseCompare
		 * 
		 * Compares two topics, returning whether they are the same, different, or similar enough that one can be substituted
		 * for the other without affecting linking.  Similar allows use of <CodeDB.Accessor.UpdateTopic()>.
		 * 
		 * If it returns <DatabaseCompareResult.Similar_WontAffectLinking>, changeFlags will also be set noting which specific
		 * fields have changed.  This isn't done with <DatabaseCompareResult.Different>, but all the flags will be set to true
		 * anyway to prevent errors.
		 * 
		 * <TopicID>, <PrototypeContextID>, and <BodyContextID> are not included in the comparison because it's assumed that 
		 * you would be comparing Topics from a parse, where they would not be set, to Topics from the database, where they 
		 * would.  <Temporary Properties> are also not compared because they do not correspond to database fields.
		 */
		public DatabaseCompareResult DatabaseCompare (Topic other, out ChangeFlags changeFlags)
			{
			// topicID - Wouldn't be known coming from a parse.

			// title - Important in linking.
			// body - Important in linking because links may favor topics with a longer body length.
			// summary - Not important in linking.
			// prototype - Important in linking because links may favor topics that have a prototype.
			// parsedPrototype - Not a database field.
			// symbol - Important in linking.
			// symbolDefinitonNumber - Important in linking.
			// titleParameters - Not a database field.
			// prototypeParameters - Not a database field.

			// topicTypeID - Important in linking.
			// usesPluralKeyword - Not a database field.
			// accessLevel - Important in linking.
			// tags - Important in linking.

			// languageID - Important in linking.
			// commentLineNumber - Not imporant in linking.
			// codeLineNumber - Not important in linking.
			
			// fileID - Not important in linking, but return Different anyway because the same topic in two different files 
			//				  are considered two separate topics.
			// prototypeContext - Not important in linking.
			// prototypeContextID - Wouldn't be known coming from a parse.
			// bodyContext - Not important in linking.
			// bodyContextID - Wouldn't be known coming from a parse.

			#if DEBUG
			if (ignoredFields != IgnoreFields.None || other.ignoredFields != IgnoreFields.None)
				{  throw new InvalidOperationException("Cannot compare topics that have ignored fields.");  }
			#endif

			if (	
				// Quick integer comparisons, only somewhat likely to be different but faster than a string comparison
				topicTypeID != other.topicTypeID ||
				accessLevel != other.accessLevel ||

				// String comparisons, most likely to be different			
				title != other.title ||
				body != other.body ||
				prototype != other.prototype ||
				symbol != other.symbol ||

				// Rest of the integer comparisons, not likely to be different
				symbolDefinitionNumber != other.symbolDefinitionNumber ||
				fileID != other.fileID ||
				languageID != other.languageID)
				{  
				changeFlags = ChangeFlags.All;
				return DatabaseCompareResult.Different;  
				}
				
			if (tags == null || tags.IsEmpty)
				{
				if (other.tags != null && other.tags.IsEmpty == false)
					{  
					changeFlags = ChangeFlags.All;
					return DatabaseCompareResult.Different;  
					}
				}
			else if (other.tags == null || other.tags.IsEmpty || tags != other.tags)
				{
				changeFlags = ChangeFlags.All;  
				return DatabaseCompareResult.Different;  
				}


			// Now we're either Same or Similar.  We want to collect exactly what's different for Similar.
			changeFlags = 0;

			// DEPENDENCY: CodeDB.Accessor.UpdateTopic() must update all fields that are relevant here.  If this function changes 
			// that one must change as well.

			if (summary != other.summary)
				{  changeFlags |= ChangeFlags.Summary;  }

			// It's important to compare the properties and not the variables here.  Both variables may not have been set by the 
			// parser, in which case the properties can return substitute values.  Both variables will always be set when topics are 
			// retrieved from the database, so unless you compare them to the properties with the substitutions applied the topics 
			// may be seen as as unequal when they're not.
			if (CommentLineNumber != other.CommentLineNumber)
				{  changeFlags |= ChangeFlags.CommentLineNumber;  }
			if (CodeLineNumber != other.CodeLineNumber)
				{  changeFlags |= ChangeFlags.CodeLineNumber;  }

			if (prototypeContext != other.prototypeContext)
				{  changeFlags |= ChangeFlags.PrototypeContext;  }
			if (bodyContext != other.bodyContext)
				{  changeFlags |= ChangeFlags.BodyContext;  }

			if (changeFlags == 0)
				{  return DatabaseCompareResult.Same;  }
			else
				{  return DatabaseCompareResult.Similar_WontAffectLinking;  }
			}


		/* Function: AddTagID
		 * Adds an individual tag ID to <TagString>.
		 */
		public void AddTagID (int tagID)
			{
			#if DEBUG
			if ((ignoredFields & IgnoreFields.Tags) != 0)
				{  throw new InvalidOperationException("Tried to access AddTagID when tags were ignored.");  }
			#endif

			if (tags == null)
				{  tags = new IDObjects.NumberSet();  }
				
			tags.Add(tagID);
			}


		/* Function: HasTagID
		 * Whether <TagString> contains an individual tag ID.
		 */
		public bool HasTagID (int tagID)
			{
			#if DEBUG
			if ((ignoredFields & IgnoreFields.Tags) != 0)
				{  throw new InvalidOperationException("Tried to access HasTagID when tags were ignored.");  }
			#endif

			if (tags == null)
				{  return false;  }
				
			return tags.Contains(tagID);
			}




		// Group: Database Properties
		// These properties map directly to database fields with minimal processing.
		// __________________________________________________________________________
		
			
		/* Property: TopicID
		 * The topic's ID number, or zero if it hasn't been set.
		 */
		public int TopicID
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TopicID) != 0)
					{  throw new InvalidOperationException("Tried to access TopicID when that field was ignored.");  }
				#endif

				return topicID;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TopicID) != 0)
					{  throw new InvalidOperationException("Tried to access TopicID when that field was ignored.");  }
				#endif

				topicID = value;  
				}
			}
			
			
		/* Property: Title
		 * The title of the topic, or null if it hasn't been set.
		 */
		public string Title
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Title) != 0)
					{  throw new InvalidOperationException("Tried to access Title when that field was ignored.");  }
				#endif

				return title;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Title) != 0)
					{  throw new InvalidOperationException("Tried to access Title when that field was ignored.");  }
				#endif

				title = value;
				titleParametersGenerated = false;
				}
			}
			
			
		/* Property: Body
		 * The body of the topic's comment in <NDMarkup>, or null if it hasn't been set.
		 */
		public string Body
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Body) != 0)
					{  throw new InvalidOperationException("Tried to access Body when that field was ignored.");  }
				#endif

				return body;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Body) != 0)
					{  throw new InvalidOperationException("Tried to access Body when that field was ignored.");  }
				#endif

				body = value;  
				}
			}


		/* Property: Summary
		 * The summary of the topic's comment in <NDMarkup>, or null if it hasn't been set.
		 */
		public string Summary
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Summary) != 0)
					{  throw new InvalidOperationException("Tried to access Summary when that field was ignored.");  }
				#endif

				return summary;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Summary) != 0)
					{  throw new InvalidOperationException("Tried to access Summary when that field was ignored.");  }
				#endif

				summary = value;  
				}
			}


		/* Property: Prototype
		 * The plain text prototype of the topic, or null if it doesn't exist.
		 */
		public string Prototype
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Prototype) != 0)
					{  throw new InvalidOperationException("Tried to access Prototype when that field was ignored.");  }
				#endif

				return prototype;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Prototype) != 0)
					{  throw new InvalidOperationException("Tried to access Prototype when that field was ignored.");  }
				#endif

				prototype = value;  
				parsedPrototype = null;
				prototypeParametersGenerated = false;
				}
			}
			
			
		/* Property: Symbol
		 * The fully resolved symbol of the topic, or null if it hasn't been set.
		 */
		public SymbolString Symbol
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Symbol) != 0)
					{  throw new InvalidOperationException("Tried to access Symbol when that field was ignored.");  }
				#endif

				return symbol;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Symbol) != 0)
					{  throw new InvalidOperationException("Tried to access Symbol when that field was ignored.");  }
				#endif

				symbol = value;  
				}
			}


		/* Property: SymbolDefinitionNumber
		 * Every unique <Symbol> defined in a file is given a number, the first one being one.  Every duplicate definition
		 * of the same symbol will receive an incremented number based on the source file order.  This will be zero if it
		 * hasn't been determined yet.
		 */
		public int SymbolDefinitionNumber
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.SymbolDefinitionNumber) != 0)
					{  throw new InvalidOperationException("Tried to access SymbolDefinitionNumber when that field was ignored.");  }
				#endif

				return symbolDefinitionNumber;
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.SymbolDefinitionNumber) != 0)
					{  throw new InvalidOperationException("Tried to access SymbolDefinitionNumber when that field was ignored.");  }
				#endif

				symbolDefinitionNumber = value;  
				}
			}


		/* Property: TopicTypeID
		 * The ID of the topic's type, or zero if it hasn't been set.
		 */
		public int TopicTypeID
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TopicTypeID) != 0)
					{  throw new InvalidOperationException("Tried to access TopicTypeID when that field was ignored.");  }
				#endif

				return topicTypeID;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TopicTypeID) != 0)
					{  throw new InvalidOperationException("Tried to access TopicTypeID when that field was ignored.");  }
				#endif

				topicTypeID = value;  
				}
			}
			
			
		/* Property: AccessLevel
		 * The access level of the topic, or <Languages.AccessLevel.Unknown> if it isn't known or hasn't been set.
		 */
		public Languages.AccessLevel AccessLevel
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.AccessLevel) != 0)
					{  throw new InvalidOperationException("Tried to access AccessLevel when that field was ignored.");  }
				#endif

				return accessLevel;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.AccessLevel) != 0)
					{  throw new InvalidOperationException("Tried to access AccessLevel when that field was ignored.");  }
				#endif

				accessLevel = value;  
				}
			}
			
			
		/* Property: TagString
		 * A string representation of an <IDObjects.NumberSet> containing all the tag IDs applied to this topic, or
		 * null if there are no tags applied or it hasn't been set.
		 */
		public string TagString
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Tags) != 0)
					{  throw new InvalidOperationException("Tried to access TagString when tags were ignored.");  }
				#endif

				if (tags != null && !tags.IsEmpty)
					{  return tags.ToString();  }
				else
					{  return null;  }
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Tags) != 0)
					{  throw new InvalidOperationException("Tried to access TagString when tags were ignored.");  }
				#endif

				if (tags != null)
					{  tags.FromString(value);  }
				else if (!String.IsNullOrEmpty(value) && value != IDObjects.NumberSet.EmptySetString)
					{  tags = new IDObjects.NumberSet(value);  }
				}
			}
			

		/* Property: FileID
		 * The ID number of the source file this topic appears in, or zero if it hasn't been set.
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
			
			
		/* Property: CommentLineNumber
		 * The line number the topic's comment begins on, if any.  If it has not been set, this will return <CodeLineNumber>.
		 * If neither of them have been set, this will return zero.
		 */
		public int CommentLineNumber
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CommentLineNumber) != 0)
					{  throw new InvalidOperationException("Tried to access CommentLineNumber when that field was ignored.");  }
				#endif

				if (commentLineNumber == 0)
					{  return codeLineNumber;  }
				else
					{  return commentLineNumber;  }
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CommentLineNumber) != 0)
					{  throw new InvalidOperationException("Tried to access CommentLineNumber when that field was ignored.");  }
				#endif

				commentLineNumber = value;  
				}
			}
			
			
		/* Property: CodeLineNumber
		 * The line number the topic's code element begins on, if any.  If it has not been set, this will return <CommentLineNumber>.
		 * If neither of them have been set, this will return zero.
		 */
		public int CodeLineNumber
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CodeLineNumber) != 0)
					{  throw new InvalidOperationException("Tried to access CodeLineNumber when that field was ignored.");  }
				#endif

				if (codeLineNumber == 0)
					{  return commentLineNumber;  }
				else
					{  return codeLineNumber;  }
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CodeLineNumber) != 0)
					{  throw new InvalidOperationException("Tried to access CodeLineNumber when that field was ignored.");  }
				#endif

				codeLineNumber = value;  
				}
			}
			
			
		/* Property: LanguageID
		 * The ID number of the language of this topic, or zero if it hasn't been set.
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


		/* Property: PrototypeContext
		 * The <ContextString> that all prototype links should use.
		 */
		public ContextString PrototypeContext
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.PrototypeContext) != 0)
					{  throw new InvalidOperationException("Tried to access PrototypeContext when that field was ignored.");  }
				#endif

				return prototypeContext;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.PrototypeContext) != 0)
					{  throw new InvalidOperationException("Tried to access PrototypeContext when that field was ignored.");  }
				#endif

				prototypeContext = value;  
				}
			}


		/* Property: PrototypeContextID
		 * The ID of <PrototypeContext> if known, or zero if not.
		 */
		public int PrototypeContextID
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.PrototypeContextID) != 0)
					{  throw new InvalidOperationException("Tried to access PrototypeContextID when that field was ignored.");  }
				#endif

				return prototypeContextID;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.PrototypeContextID) != 0)
					{  throw new InvalidOperationException("Tried to access PrototypeContextID when that field was ignored.");  }
				#endif

				prototypeContextID = value;  
				}
			}


		/* Property: BodyContext
		 * The <ContextString> that all body links should use.
		 */
		public ContextString BodyContext
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.BodyContext) != 0)
					{  throw new InvalidOperationException("Tried to access BodyContext when that field was ignored.");  }
				#endif

				return bodyContext;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.BodyContext) != 0)
					{  throw new InvalidOperationException("Tried to access BodyContext when that field was ignored.");  }
				#endif

				bodyContext = value;  
				}
			}
			
			
		/* Property: BodyContextID
		 * The ID of <BodyContext> if known, or zero if not.
		 */
		public int BodyContextID
			{
			get
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.BodyContextID) != 0)
					{  throw new InvalidOperationException("Tried to access BodyContextID when that field was ignored.");  }
				#endif

				return bodyContextID;  
				}
			set
				{  
				#if DEBUG
				if ((ignoredFields & IgnoreFields.BodyContextID) != 0)
					{  throw new InvalidOperationException("Tried to access BodyContextID when that field was ignored.");  }
				#endif

				bodyContextID = value;  
				}
			}


			
		// Group: Temporary Properties
		// These properties aid in processing but are not stored in the database.
		// __________________________________________________________________________
		

		/* Property: IgnoredFields
		 * 
		 * When querying topics from the database, not all fields may be needed in all situations.  The database
		 * may accept <IgnoreFields> flags to skip retrieving parts of them.  If that's done, the flags should also
		 * be set here so that in debug builds an exception will be thrown if you try to access those properties.
		 * 
		 * IgnoredFields defaults to <IgnoreFields.None> so that topics created by parsing don't have to worry
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

					
		/* Property: UsesPluralKeyword
		 * 
		 * Whether the topic is a Natural Docs topic which uses the plural keyword form.
		 * 
		 * This isn't included in the database because any effects of this should already be reflected in <Body's> <NDMarkup>
		 * by the time it gets there.
		 */
		public bool UsesPluralKeyword
			{
			get
				{  return usesPluralKeyword;  }
			set
				{  usesPluralKeyword = value;  }
			}


		/* Property: ParsedPrototype
		 * If <Prototype> is not null, this will be it in <ParsedPrototype> form.
		 */
		public ParsedPrototype ParsedPrototype
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Prototype) != 0)
					{  throw new InvalidOperationException("Tried to access ParsedPrototype when the prototype was ignored.");  }
				#endif

				if (parsedPrototype != null)
					{  return parsedPrototype;  }
				if (prototype == null)
					{  return null;  }

				parsedPrototype = Engine.Instance.Languages.FromID(languageID).ParsePrototype(prototype, topicTypeID);

				return parsedPrototype;
				}
			}


		/* Property: HasTitleParameters
		 * Whether there are any parameters in the title.  Unlike testing <TitleParameters> against null, this also distinguishes
		 * between titles with empty parentheses and titles with none.
		 */
		public bool HasTitleParameters
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Title) != 0)
					{  throw new InvalidOperationException("Tried to access HasTitleParameters when the title was ignored.");  }
				#endif

				return (ParameterString.GetEndingParenthesesIndex(title) != -1);
				}
			}
			
			
		/* Property: TitleParameters
		 * The parameters found in the title, as opposed to the prototype, or null if none.
		 */
		public ParameterString TitleParameters
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Tags) != 0)
					{  throw new InvalidOperationException("Tried to access TitleParameters when the title was ignored.");  }
				#endif

				if (!titleParametersGenerated)
					{
					int parenthesesIndex = ParameterString.GetEndingParenthesesIndex(title);

					if (parenthesesIndex == -1)
						{  titleParameters = new ParameterString();  }
					else
						{  titleParameters = ParameterString.FromParenthesesString(title.Substring(parenthesesIndex));  }

					titleParametersGenerated = true;
					}

				return titleParameters;
				}
			}
			
			
		/* Property: PrototypeParameters
		 * The parameters found in the prototype, or null if none.
		 */
		public ParameterString PrototypeParameters
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Prototype) != 0)
					{  throw new InvalidOperationException("Tried to access PrototypeParameters when the prototype was ignored.");  }
				#endif

				if (!prototypeParametersGenerated)
					{
					ParsedPrototype parsedPrototype = ParsedPrototype;

					if (parsedPrototype == null || parsedPrototype.NumberOfParameters == 0)
						{  prototypeParameters = new ParameterString();  }
					else
						{
						string[] parameterTypes = new string[parsedPrototype.NumberOfParameters];
						Tokenization.TokenIterator start, end;

						for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
							{
							parsedPrototype.GetBaseParameterType(i, out start, out end);
							parameterTypes[i] = parsedPrototype.Tokenizer.TextBetween(start, end);
							}

						prototypeParameters = ParameterString.FromParameterTypes(parameterTypes);
						}

					prototypeParametersGenerated = true;
					}

				return prototypeParameters;
				}
			}
			
			

		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: topicID
		 * The topic's ID number, or zero if not specified.
		 */
		protected int topicID;
		
		/* var: title
		 * The title of the comment, or null if not specified.
		 */
		protected string title;
		
		/* var: body
		 * The body of the comment, or null if not specified.
		 */
		protected string body;

		/* var: summary
		 * The summary of the comment, or null if not specified.
		 */
		protected string summary;

		/* var: prototype
		 * The plain-text prototype of the topic, or null if not present.
		 */
		protected string prototype;

		/* var: parsedPrototype
		 * The <prototype> in <ParsedPrototype> form, or null if <prototype> is null or it hasn't been generated yet.
		 */
		protected ParsedPrototype parsedPrototype;
		
		/* var: symbol
		 * The topic's fully resolved symbol, or null if not specified.
		 */
		protected SymbolString symbol;

		/* var: symbolDefinitionNumber
		 * Every unique <symbol> defined in a file is given a number, the first one being one.  Every duplicate definition
		 * of the same symbol will receive an incremented number based on the source file order.  This will be zero if it
		 * hasn't been determined yet.
		 */
		protected int symbolDefinitionNumber;

		/* var: titleParameters
		 * Any parameters found in the title, as opposed to the prototype.
		 */
		protected ParameterString titleParameters;

		/* var: titleParametersGenerated
		 * Whether <titleParameters> was generated, as it's done on demand and stored.
		 */
		protected bool titleParametersGenerated;
		
		/* var: prototypeParameters
		 * Any parameters found in the prototype.
		 */
		protected ParameterString prototypeParameters;

		/* var: prototypeParametersGenerated
		 * Whether <prototypeParameters> was generated, as it's done on demand and stored.
		 */
		protected bool prototypeParametersGenerated;
		
		/* var: topicTypeID
		 * The ID number of the topic's type, or zero if not specified.
		 */
		protected int topicTypeID;
		
		/* var: usesPluralKeyword
		 * Whether the topic is a Natural Docs comment which uses the plural form of a keyword.
		 */
		protected bool usesPluralKeyword;

		/* var: accessLevel
		 * The access level of the topic.
		 */
		protected Languages.AccessLevel accessLevel;
		
		/* var: tags
		 * A set of the tags applied to this topic.  May or may not be null if there are none.
		 */
		protected IDObjects.NumberSet tags;
				
		/* var: fileID
		 * The ID of the source file this topic appears in, or zero if not specified.
		 */
		protected int fileID;

		/* var: commentLineNumber
		 * The line number the comment appears on, or zero if not specified.
		 */
		protected int commentLineNumber;

		/* var: codeLineNumber
		 * The line number the actual code element appears on, or zero if not specified.
		 */
		protected int codeLineNumber;
		
		/* var: languageID
		 * The ID of the topic's language, or zero if not specified.
		 */
		protected int languageID;

		/* var: prototypeContext
		 * The <ContextString> that all prototype links should use.
		 */
		protected ContextString prototypeContext;

		/* var: prototypeContextID
		 * The ID of <prototypeContext> if known, or zero if not.
		 */
		protected int prototypeContextID;

		/* var: bodyContext
		 * The <ContextString> that all body links should use.
		 */
		protected ContextString bodyContext;
		
		/* var: bodyContextID
		 * The ID of <bodyContext> if known, or zero if not.
		 */
		protected int bodyContextID;

		/* var: ignoredFields
		 * The <IgnoreFields> applied to this object.
		 */
		protected IgnoreFields ignoredFields;

		}
	}
