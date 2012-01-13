/* 
 * Class: GregValure.NaturalDocs.Engine.Topic
 * ____________________________________________________________________________
 * 
 * A class encapsulating all the information available about a topic.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine
	{
	public class Topic
		{
		
		public enum DatabaseCompareResult
			{  Equal, NotEqual, EqualExceptLineNumbersAndBody  }
			
			
			
		// Group: Functions
		// __________________________________________________________________________
		
		
		public Topic ()
			{
			topicID = 0;
			fileID = 0;
			languageID = 0;
			commentLineNumber = 0;
			codeLineNumber = 0;
			title = null;
			body = null;
			summary = null;
			prototype = null;
			symbol = new SymbolString();
			parameters = new ParameterString();
			topicTypeID = 0;
			accessLevel = Languages.AccessLevel.Unknown;
			tags = null;
			
			usesPluralKeyword = false;
			parsedPrototype = null;
			}
			
			
		/* Function: DatabaseCompare
		 * 
		 * Compares two topics, returning whether they are equal, not equal, or equal except for line numbers and <Body>.
		 * The latter is an important distinction because it allows use of <CodeDB.Accessor.UpdateTopic()>.
		 * 
		 * <TopicID> is not included in the comparison because it's assumed that you would be comparing Topics from a parse, 
		 * where they would not be set, to Topics from the database, where they would.  <Temporary Properties> are also not 
		 * compared because they do not correspond to database fields.
		 */
		public DatabaseCompareResult DatabaseCompare (Topic other)
			{
			if (	// Quick integer comparisons, only somewhat likely to be different but faster than a string comparison
				topicTypeID != other.topicTypeID ||
				accessLevel != other.accessLevel ||

				// String comparisons, most likely to be different			
				title != other.title ||
				summary != other.summary ||
				symbol != other.symbol ||
				prototype != other.prototype ||
				parameters != other.parameters ||

				// Rest of the integer comparisons, not likely to be different
				fileID != other.fileID ||
				languageID != other.languageID)
				{  return DatabaseCompareResult.NotEqual;  }
				
			if (tags == null || tags.IsEmpty)
				{
				if (other.tags != null && other.tags.IsEmpty == false)
					{  return DatabaseCompareResult.NotEqual;  }
				}
			else if (other.tags == null || other.tags.IsEmpty)
				{  return DatabaseCompareResult.NotEqual;  }
			else if (tags != other.tags)
				{  return DatabaseCompareResult.NotEqual;  }
			
			// Compare line number properties instead of variables so we get the substitution
			if (CodeLineNumber != other.CodeLineNumber ||
				CommentLineNumber != other.CommentLineNumber ||
				body != other.Body)
				{  return DatabaseCompareResult.EqualExceptLineNumbersAndBody;  }
				
			return DatabaseCompareResult.Equal;
			}


		/* Function: AddTagID
		 * Adds an individual tag ID to <TagString>.
		 */
		public void AddTagID (int tagID)
			{
			if (tags == null)
				{  tags = new IDObjects.NumberSet();  }
				
			tags.Add(tagID);
			}


		/* Function: HasTagID
		 * Whether <TagString> contains an individual tag ID.
		 */
		public bool HasTagID (int tagID)
			{
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
				{  return topicID;  }
			set
				{  topicID = value;  }
			}
			
			
		/* Property: FileID
		 * The ID number of the source file this topic appears in, or zero if it hasn't been set.
		 */
		public int FileID
			{
			get
				{  return fileID;  }
			set
				{  fileID = value;  }
			}
			
			
		/* Property: LanguageID
		 * The ID number of the language of this topic, or zero if it hasn't been set.
		 */
		public int LanguageID
			{
			get
				{  return languageID;  }
			set
				{  languageID = value;  }
			}
			
			
		/* Property: CommentLineNumber
		 * The line number the topic's comment begins on, if any.  If it has not been set, this will return <CodeLineNumber>.
		 * If neither of them have been set, this will return zero.
		 */
		public int CommentLineNumber
			{
			get
				{
				if (commentLineNumber == 0)
					{  return codeLineNumber;  }
				else
					{  return commentLineNumber;  }
				}
			set
				{  commentLineNumber = value;  }
			}
			
			
		/* Property: CodeLineNumber
		 * The line number the topic's code element begins on, if any.  If it has not been set, this will return <CommentLineNumber>.
		 * If neither of them have been set, this will return zero.
		 */
		public int CodeLineNumber
			{
			get
				{
				if (codeLineNumber == 0)
					{  return commentLineNumber;  }
				else
					{  return codeLineNumber;  }
				}
			set
				{  codeLineNumber = value;  }
			}
			
			
		/* Property: Title
		 * The title of the topic, or null if it hasn't been set.
		 */
		public string Title
			{
			get
				{  return title;  }
			set
				{  title = value;  }
			}
			
			
		/* Property: Body
		 * The body of the topic's comment in <NDMarkup>, or null if it hasn't been set.
		 */
		public string Body
			{
			get
				{  return body;  }
			set
				{  body = value;  }
			}


		/* Property: Summary
		 * The summary of the topic's comment in <NDMarkup>, or null if it hasn't been set.
		 */
		public string Summary
			{
			get
				{  return summary;  }
			set
				{  summary = value;  }
			}


		/* Property: Prototype
		 * The plain text prototype of the topic, or null if it doesn't exist.
		 */
		public string Prototype
			{
			get
				{  return prototype;  }
			set
				{  
				prototype = value;  
				parsedPrototype = null;
				}
			}
			
			
		/* Property: Symbol
		 * The fully resolved symbol of the topic, or null if it hasn't been set.
		 */
		public SymbolString Symbol
			{
			get
				{  return symbol;  }
			set
				{  symbol = value;  }
			}


		/* Property: Parameters
		 * The parameter string associated with the topic, or null if none.
		 */
		public ParameterString Parameters
			{
			get
				{  return parameters;  }
			set
				{  parameters = value;  }
			}
			
			
		/* Property: TopicTypeID
		 * The ID of the topic's type, or zero if it hasn't been set.
		 */
		public int TopicTypeID
			{
			get
				{  return topicTypeID;  }
			set
				{  topicTypeID = value;  }
			}
			
			
		/* Property: AccessLevel
		 * The access level of the topic, or <Languages.AccessLevel.Unknown> if it isn't known or hasn't been set.
		 */
		public Languages.AccessLevel AccessLevel
			{
			get
				{  return accessLevel;  }
			set
				{  accessLevel = value;  }
			}
			
			
		/* Property: TagString
		 * A string representation of an <IDObjects.NumberSet> containing all the tag IDs applied to this topic, or
		 * null if there are no tags applied or it hasn't been set.
		 */
		public string TagString
			{
			get
				{
				if (tags != null && !tags.IsEmpty)
					{  return tags.ToString();  }
				else
					{  return null;  }
				}
			set
				{
				if (tags != null)
					{  tags.FromString(value);  }
				else if (!String.IsNullOrEmpty(value) && value != IDObjects.NumberSet.EmptySetString)
					{  tags = new IDObjects.NumberSet(value);  }
				}
			}
			

			
			
		// Group: Temporary Properties
		// These properties aid in processing but are not stored in the database.
		// __________________________________________________________________________
		
					
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
		 * 
		 * If <Prototype> is not null, this will be it in <ParsedPrototype> form.
		 * 
		 * This is generated automatically the first time it is accessed.  However, if desired you can also use the assignment to
		 * pre-generate them.
		 */
		public ParsedPrototype ParsedPrototype
			{
			get
				{
				if (parsedPrototype != null)
					{  return parsedPrototype;  }
				if (prototype == null)
					{  return null;  }

				parsedPrototype = Engine.Instance.Languages.FromID(languageID).GetParser().ParsePrototype(prototype, topicTypeID);

				return parsedPrototype;
				}

			set
				{
				#if DEBUG
					if (prototype == null)
						{  throw new InvalidOperationException("Can't assign a ParsedPrototype to a Topic when Prototype is null.");  }
					if (value == null)
						{  throw new InvalidOperationException("Can't assign a null ParsedPrototype to a Topic.");  }
					if (value.Tokenizer.RawText != prototype)
						{  throw new InvalidOperationException("Can't assign a ParsedPrototype to a Topic when it doesn't match Prototype.");  }
				#endif

				parsedPrototype = value;
				}
			}


		/* Property: IsParsedPrototypeGenerated
		 * Whether a <ParsedPrototype> has been pre-generated for this topic.  You can't simply check <ParsedPrototype> for null 
		 * since it's demand-generated, so if you're interested in pre-generating them you can check this instead.
		 */
		public bool IsParsedPrototypeGenerated
			{
			get
				{  return (parsedPrototype != null);  }
			}

			

		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: topicID
		 * The topic's ID number, or zero if not specified.
		 */
		protected int topicID;
		
		/* var: fileID
		 * The ID of the source file this topic appears in, or zero if not specified.
		 */
		protected int fileID;

		/* var: languageID
		 * The ID of the topic's language, or zero if not specified.
		 */
		protected int languageID;
		
		/* var: commentLineNumber
		 * The line number the comment appears on, or zero if not specified.
		 */
		protected int commentLineNumber;

		/* var: codeLineNumber
		 * The line number the actual code element appears on, or zero if not specified.
		 */
		protected int codeLineNumber;
		
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

		/* var: symbol
		 * The topic's fully resolved symbol, or null if not specified.
		 */
		protected SymbolString symbol;
		
		/* var: parameters
		 * The parameters of the symbol, or null if none.
		 */
		protected ParameterString parameters;

		/* var: topicTypeID
		 * The ID number of the topic's type, or zero if not specified.
		 */
		protected int topicTypeID;
		
		/* var: usesPluralKeyword
		 * Whether the topic is a Natural Docs comment which uses the plural form of a keyword.
		 */
		protected bool usesPluralKeyword;

		/* var: parsedPrototype
		 * The <prototype> in <ParsedPrototype> form, or null if <prototype> is null or it hasn't been generated yet.
		 */
		protected ParsedPrototype parsedPrototype;
		
		/* var: accessLevel
		 * The access level of the topic.
		 */
		protected Languages.AccessLevel accessLevel;
		
		/* var: tags
		 * A set of the tags applied to this topic.  May or may not be null if there are none.
		 */
		protected IDObjects.NumberSet tags;
				
		}
	}
