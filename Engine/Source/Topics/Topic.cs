﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Topics.Topic
 * ____________________________________________________________________________
 *
 * A class encapsulating all the information available about a topic.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Topics
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
		public enum ChangeFlags : uint
			{
			Title = 0x00000001,
			Body = 0x00000002,
			Summary = 0x00000004,
			Prototype = 0x00000008,
			Symbol = 0x00000010,
			SymbolDefinitonNumber = 0x00000020,
			Class = 0x00000040,
			IsList = 0x00000080,
			IsEmbedded = 0x00000100,

			CommentTypeID = 0x00000200,
			DeclaredAccessLevel = 0x00000400,
			EffectiveAccessLevel = 0x00000800,
			Tags = 0x00001000,

			LanguageID = 0x00002000,
			CommentLineNumber = 0x00004000,
			CodeLineNumber = 0x00008000,

			FileID = 0x00010000,
			FilePosition = 0x00020000,
			PrototypeContext = 0x00040000,
			BodyContext = 0x00080000,

			All = Title | Body | Summary | Prototype | Symbol | SymbolDefinitonNumber | Class |
					 IsList | IsEmbedded | CommentTypeID | DeclaredAccessLevel | EffectiveAccessLevel |
					 Tags | LanguageID | CommentLineNumber | CodeLineNumber |
					 FileID | FilePosition | PrototypeContext | BodyContext
			}


		/* Enum: IgnoreFields
		 *
		 * When querying topics from the database, not all fields may be needed in all circumstances.  This is a
		 * bitfield that allows you to specify which fields can be ignored.  This is also stored in the object so that,
		 * in debug builds, if you try to access any of these fields an exception will be thrown.
		 */
		[Flags]
		public enum IgnoreFields : uint
			{
			None = 0x00000000,

			TopicID = 0x00000001,
			Title = 0x00000002,
			Body = 0x00000004,
			BodyLength = 0x00000008,
			Summary = 0x00000010,
			Prototype = 0x00000020,
			Symbol = 0x00000040,
			SymbolDefinitionNumber = 0x00000080,
			ClassString = 0x00000100,
			ClassID = 0x00000200,
			IsList = 0x00000400,
			IsEmbedded = 0x00000800,
			CommentTypeID = 0x00001000,
			DeclaredAccessLevel = 0x00002000,
			EffectiveAccessLevel = 0x00004000,
			Tags = 0x00008000,
			FileID = 0x00010000,
			FilePosition = 0x00020000,
			CommentLineNumber = 0x00040000,
			CodeLineNumber = 0x00080000,
			LanguageID = 0x00100000,
			PrototypeContext = 0x00200000,
			PrototypeContextID = 0x00400000,
			BodyContext = 0x00800000,
			BodyContextID = 0x01000000
			}


		/* Enum: BuildFlags
		 * Flags that store which build-on-demand fields have aready been generated.
		 */
		[Flags]
		public enum BuildFlags : byte
			{
			None = 0x00,

			ParsedPrototype = 0x01,
			ParsedClassPrototype = 0x02,
			TitleParameters = 0x04,
			PrototypeParameters = 0x08
			}



		// Group: Functions
		// __________________________________________________________________________


		public Topic (CommentTypes.Manager manager)
			{
			this.commentTypes = manager;
			topicID = 0;

			title = null;
			body = null;
			bodyLength = 0;
			summary = null;
			prototype = null;
			parsedPrototype = null;
			parsedClassPrototype = null;
			symbol = new SymbolString();
			symbolDefinitionNumber = 0;
			classString = new ClassString();
			classID = 0;
			isList = false;
			isEmbedded = false;
			titleParameters = new ParameterString();
			prototypeParameters = new ParameterString();

			commentTypeID = 0;
			isList = false;
			declaredAccessLevel = Languages.AccessLevel.Unknown;
			effectiveAccessLevel = Languages.AccessLevel.Unknown;
			tagIDs = null;

			languageID = 0;
			commentLineNumber = 0;
			codeLineNumber = 0;

			fileID = 0;
			filePosition = 0;
			prototypeContext = new ContextString();
			prototypeContextID = 0;
			bodyContext = new ContextString();
			bodyContextID = 0;

			ignoredFields = IgnoreFields.None;
			buildFlags = BuildFlags.None;
			}


		/* Function: Duplicate
		 */
		public Topic Duplicate ()
			{
			Topic duplicate = new Topic(commentTypes);

			//duplicate.manager = manager;
			duplicate.topicID = topicID;

			duplicate.title = title;
			duplicate.body = body;
			duplicate.bodyLength = bodyLength;
			duplicate.summary = summary;
			duplicate.prototype = prototype;

			// It might be okay to copy these references, but it introduces a hidden dependency so let's not to play it safe.
			// Let the duplicate generate its own copy.
			duplicate.parsedPrototype = null;
			duplicate.parsedClassPrototype = null;

			duplicate.symbol = symbol;
			duplicate.symbolDefinitionNumber = symbolDefinitionNumber;
			duplicate.classString = classString;
			duplicate.classID = classID;
			duplicate.isList = isList;
			duplicate.isEmbedded = isEmbedded;
			duplicate.titleParameters = titleParameters;
			duplicate.prototypeParameters = prototypeParameters;

			duplicate.commentTypeID = commentTypeID;
			duplicate.isList = isList;
			duplicate.declaredAccessLevel = declaredAccessLevel;
			duplicate.effectiveAccessLevel = effectiveAccessLevel;

			if (tagIDs == null)
				{  duplicate.tagIDs = null;  }
			else
				{  duplicate.tagIDs = new IDObjects.NumberSet(tagIDs);  }

			duplicate.languageID = languageID;
			duplicate.commentLineNumber = commentLineNumber;
			duplicate.codeLineNumber = codeLineNumber;

			duplicate.fileID = fileID;
			duplicate.filePosition = filePosition;
			duplicate.prototypeContext = prototypeContext;
			duplicate.prototypeContextID = prototypeContextID;
			duplicate.bodyContext = bodyContext;
			duplicate.bodyContextID = bodyContextID;

			duplicate.ignoredFields = ignoredFields;
			duplicate.buildFlags = buildFlags;

			// Have to reset the flags for the parsed prototypes we didn't copy.
			duplicate.buildFlags &= ~(BuildFlags.ParsedPrototype | BuildFlags.ParsedClassPrototype);

			return duplicate;
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
		 * <TopicID>, <ClassID>, <PrototypeContextID>, and <BodyContextID> are not included in the comparison because it's
		 * assumed that you would be comparing Topics from a parse, where they would not be set, to Topics from the database,
		 * where they would.  <Temporary Properties> are also not compared because they do not correspond to database fields.
		 */
		public DatabaseCompareResult DatabaseCompare (Topic other, out ChangeFlags changeFlags)
			{
			// topicID - Wouldn't be known coming from a parse.
			// title - Important in linking.
			// body - Only the body's existence and length are important in linking, not the content beyond that, so this is
			//			  handled by bodyLength.
			// bodyLength - Important in linking because links may favor topics that have a body, or that have a longer
			//						body length.
			// summary - Not important in linking.
			// prototype - Important in linking because links may favor topics that have a prototype, or that have a longer
			//					 prototype length, or that have a prototype where the parameters match the ones in the link.
			// parsedPrototype - Not a database field.
			// parsedClassPrototype - Not a database field.
			// symbol - Important in linking.
			// symbolDefinitonNumber - Important in linking.
			// classString - Not important in linking.  Changes in classString would be reflected in symbol.
			// classID - Wouldn't be known coming from a parse.
			// isList - Not important in linking.
			// isEmbedded - Not important in linking.
			// titleParameters - Not a database field.
			// prototypeParameters - Not a database field.
			// commentTypeID - Important in linking.
			// declaredAccessLevel - Not important in linking.
			// effectiveAccessLevel - Not important in linking, but return Different anyway because it could affect visibility
			//								   when building a filtered set of documentation.
			// tagIDs - Not important in linking, but return Different anyway because it could affect visibility when building a
			//				filtered set of documentation.
			// fileID - Not important in linking, but return Different anyway because the same topic in two different files
			//			  are considered two separate topics.
			// filePosition - Not important in linking.  SymbolDefinitionNumber will take care of the first being favored.
			// commentLineNumber - Not imporant in linking.  SymbolDefinitionNumber will take care of the first being favored.
			// codeLineNumber - Not important in linking.  SymbolDefinitionNumber will take care of the first being favored.
			// languageID - Important in linking.
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
				commentTypeID != other.commentTypeID ||
				bodyLength != other.bodyLength ||
				effectiveAccessLevel != other.effectiveAccessLevel ||

				// String comparisons, most likely to be different
				title != other.title ||
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

			if (tagIDs == null || tagIDs.IsEmpty)
				{
				if (other.tagIDs != null && other.tagIDs.IsEmpty == false)
					{
					changeFlags = ChangeFlags.All;
					return DatabaseCompareResult.Different;
					}
				}
			else if (other.tagIDs == null || other.tagIDs.IsEmpty || tagIDs != other.tagIDs)
				{
				changeFlags = ChangeFlags.All;
				return DatabaseCompareResult.Different;
				}


			// Now we're either Same or Similar.  We want to collect exactly what's different for Similar.
			changeFlags = 0;

			// DEPENDENCY: CodeDB.Accessor.UpdateTopic() must update all fields that are relevant here.  If this function changes
			// that one must change as well.

			if (body != other.body)
				{  changeFlags |= ChangeFlags.Body;  }
			if (summary != other.summary)
				{  changeFlags |= ChangeFlags.Summary;  }
			if (classString != other.classString)
				{  changeFlags |= ChangeFlags.Class;  }
			if (declaredAccessLevel != other.declaredAccessLevel)
				{  changeFlags |= ChangeFlags.DeclaredAccessLevel;  }
			if (isList != other.isList)
				{  changeFlags |= ChangeFlags.IsList;  }
			if (isEmbedded != other.isEmbedded)
				{  changeFlags |= ChangeFlags.IsEmbedded;  }
			if (commentLineNumber != other.commentLineNumber)
				{  changeFlags |= ChangeFlags.CommentLineNumber;  }
			if (codeLineNumber != other.codeLineNumber)
				{  changeFlags |= ChangeFlags.CodeLineNumber;  }
			if (filePosition != other.filePosition)
				{  changeFlags |= ChangeFlags.FilePosition;  }

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
		 * Adds an individual tag ID to <TagIDs>.
		 */
		public void AddTagID (int tagID)
			{
			#if DEBUG
			if ((ignoredFields & IgnoreFields.Tags) != 0)
				{  throw new InvalidOperationException("Tried to access AddTagID when tags were ignored.");  }
			#endif

			if (tagIDs == null)
				{  tagIDs = new IDObjects.NumberSet();  }

			tagIDs.Add(tagID);
			}


		/* Function: AddTagsFrom
		 * Adds all the tags in the other <Topic> to this one.
		 */
		public void AddTagsFrom (Topic other)
			{
			if (other.tagIDs == null || other.tagIDs.IsEmpty)
				{  return;  }

			else if (tagIDs == null)
				{  tagIDs = new IDObjects.NumberSet(other.tagIDs);  }

			else
				{  tagIDs.Add(other.tagIDs);  }
			}


		/* Function: HasTagID
		 * Whether <TagIDs> contains an individual tag ID.
		 */
		public bool HasTagID (int tagID)
			{
			#if DEBUG
			if ((ignoredFields & IgnoreFields.Tags) != 0)
				{  throw new InvalidOperationException("Tried to access HasTagID when tags were ignored.");  }
			#endif

			if (tagIDs == null)
				{  return false;  }

			return tagIDs.Contains(tagID);
			}


		/* Function: ToString
		 * This is only available to aid debugging in Visual Studio.  When you have a list of objects it puts the result of
		 * ToString() next to each one.  You should not rely on it for anything else.
		 */
		override public string ToString ()
			{
			if (isEmbedded)
				{  return "> " + title;  }
			else
				{  return title;  }
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
				buildFlags &= ~BuildFlags.TitleParameters;
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

				if (body != null)
					{  bodyLength = body.Length;  }
				else
					{  bodyLength = 0;  }
				}
			}


		/* Property: BodyLength
		 * The length of the topic's body.  If <Body> is set this is the equivalent of checking its Length property.
		 * However, if <IgnoreFields.Body> is on you can use this to still have its length retrieved from the database.
		 */
		public int BodyLength
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.BodyLength) != 0)
					{  throw new InvalidOperationException("Tried to access BodyLength when that field was ignored.");  }
				#endif

				return bodyLength;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.BodyLength) != 0)
					{  throw new InvalidOperationException("Tried to access BodyLength when that field was ignored.");  }
				if (body != null && body.Length != value)
					{  throw new InvalidOperationException("Tried to set BodyLength when Body was defined and has a different length.");  }
				#endif

				bodyLength = value;
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
				parsedClassPrototype = null;
				buildFlags &= ~(BuildFlags.PrototypeParameters | BuildFlags.ParsedPrototype | BuildFlags.ParsedClassPrototype);
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


		/* Property: ClassString
		 * The <ClassString> that this topic defines or is a part of.
		 */
		public ClassString ClassString
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
		 * The ID of <ClassString>, or zero if <ClassString> is null or its ID is unknown.
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


		/* Property: IsList
		 *
		 * Whether this is a list topic used to document multiple elements.  This will not be set for enums, though it would be set
		 * for a list of enums.
		 */
		public bool IsList
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.IsList) != 0)
					{  throw new InvalidOperationException("Tried to access IsList when that field was ignored.");  }
				#endif

				return isList;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.IsList) != 0)
					{  throw new InvalidOperationException("Tried to access IsList when that field was ignored.");  }
				#endif

				isList = value;
				}
			}


		/* Property: IsEmbedded
		 * Whether this topic is embedded in a prior topic.  This is used for members of list and enum topics.  Entries that appear in
		 * definition lists within these topics will get their own <Topic> objects to allow for linking, but they will not appear in the
		 * output because they are already covered by the parent.  Embedded topics will always come after their parent
		 * in a list of topics, so the last non-embedded topic is the parent.
		 */
		public bool IsEmbedded
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.IsEmbedded) != 0)
					{  throw new InvalidOperationException("Tried to access IsEmbedded when that field was ignored.");  }
				#endif

				return isEmbedded;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.IsEmbedded) != 0)
					{  throw new InvalidOperationException("Tried to access IsEmbedded when that field was ignored.");  }
				#endif

				isEmbedded = value;
				}
			}


		/* Property: CommentTypeID
		 * The ID of the topic's comment type, or zero if it hasn't been set.
		 */
		public int CommentTypeID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CommentTypeID) != 0)
					{  throw new InvalidOperationException("Tried to access CommentTypeID when that field was ignored.");  }
				#endif

				return commentTypeID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CommentTypeID) != 0)
					{  throw new InvalidOperationException("Tried to access CommentTypeID when that field was ignored.");  }
				#endif

				commentTypeID = value;
				}
			}


		/* Property: DeclaredAccessLevel
		 * The declared access level of the topic, or <Languages.AccessLevel.Unknown> if it isn't known or hasn't been set.
		 * For a public member of an internal class, this would be public.
		 */
		public Languages.AccessLevel DeclaredAccessLevel
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.DeclaredAccessLevel) != 0)
					{  throw new InvalidOperationException("Tried to access DeclaredAccessLevel when that field was ignored.");  }
				#endif

				return declaredAccessLevel;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.DeclaredAccessLevel) != 0)
					{  throw new InvalidOperationException("Tried to access DeclaredAccessLevel when that field was ignored.");  }
				#endif

				declaredAccessLevel = value;
				}
			}


		/* Property: EffectiveAccessLevel
		 * The effective access level of the topic, or <Languages.AccessLevel.Unknown> if it isn't known or hasn't been set.  For a
		 * public member of an internal class, this would be internal.
		 */
		public Languages.AccessLevel EffectiveAccessLevel
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.EffectiveAccessLevel) != 0)
					{  throw new InvalidOperationException("Tried to access EffectiveAccessLevel when that field was ignored.");  }
				#endif

				return effectiveAccessLevel;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.EffectiveAccessLevel) != 0)
					{  throw new InvalidOperationException("Tried to access EffectiveAccessLevel when that field was ignored.");  }
				#endif

				effectiveAccessLevel = value;
				}
			}


		/* Property: TagIDs
		 * An <IDObjects.NumberSet> containing all the tag IDs applied to this topic, or null if none.
		 */
		public IDObjects.NumberSet TagIDs
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Tags) != 0)
					{  throw new InvalidOperationException("Tried to access TagString when tags were ignored.");  }
				#endif

				return tagIDs;
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

				if (tagIDs != null && !tagIDs.IsEmpty)
					{  return tagIDs.ToString();  }
				else
					{  return null;  }
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Tags) != 0)
					{  throw new InvalidOperationException("Tried to access TagString when tags were ignored.");  }
				#endif

				if (tagIDs == null)
					{  tagIDs = new IDObjects.NumberSet(value);  }
				else
					{  tagIDs.SetTo(value);  }
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


		/* Property: FilePosition
		 * The relative position of the topic in the file, or zero if it hasn't been set yet.  The first topic will be one, and every
		 * subsequent one will have a higher number.  This is done because not every topic will have a <CommentLineNumber>
		 * or a <CodeLineNumber> so the file order may not be determinable from them.
		 */
		public int FilePosition
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.FilePosition) != 0)
					{  throw new InvalidOperationException("Tried to access FilePosition when that field was ignored.");  }
				#endif

				return filePosition;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.FilePosition) != 0)
					{  throw new InvalidOperationException("Tried to access FilePosition when that field was ignored.");  }
				#endif

				filePosition = value;
				}
			}


		/* Property: CommentLineNumber
		 * The line number the topic's comment begins on, or zero if it hasn't been set yet or the topic doesn't have a comment.
		 */
		public int CommentLineNumber
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CommentLineNumber) != 0)
					{  throw new InvalidOperationException("Tried to access CommentLineNumber when that field was ignored.");  }
				#endif

				return commentLineNumber;
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
		 * The line number the topic's code element begins on, or zero if it hasn't been set yet or the topic doesn't have a code
		 * element.
		 */
		public int CodeLineNumber
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CodeLineNumber) != 0)
					{  throw new InvalidOperationException("Tried to access CodeLineNumber when that field was ignored.");  }
				#endif

				return codeLineNumber;
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
		 * The ID of <PrototypeContext>, or zero if <PrototypeContext> is null or its ID is unknown.
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


		/* Property: PrototypeContextIDKnown
		 * Whether <PrototypeContextID> is known, which basically tests whether <PrototypeContextID> is zero when
		 * <PrototypeContext> is not null.
		 */
		public bool PrototypeContextIDKnown
			{
			get
				{  return (prototypeContextID != 0 || prototypeContext == null);  }
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
		 * The ID of <BodyContext>, or zero if <BodyContext> is null or its ID is unknown.
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


		/* Property: BodyContextIDKnown
		 * Whether <BodyContextID> is known, which basically tests whether <BodyContextID> is zero when <BodyContext>
		 * is not null.
		 */
		public bool BodyContextIDKnown
			{
			get
				{  return (bodyContextID != 0 || bodyContext == null);  }
			}



		// Group: Temporary Properties
		// These properties aid in processing but are not stored in the database.
		// __________________________________________________________________________


		/* Property: CommentTypes
		 * The <CommentTypes.Manager> associated with this topic.
		 */
		public CommentTypes.Manager CommentTypes
			{
			get
				{  return commentTypes;  }
			}


		/* Property: EngineInstance
		 * The <Engine.Instance> associated with this topic.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{  return CommentTypes.EngineInstance;  }
			}


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

				if ((buildFlags & BuildFlags.ParsedPrototype) == 0)
					{
					if (prototype == null)
						{  parsedPrototype = null;  }
					else
						{  parsedPrototype = EngineInstance.Languages.FromID(languageID).Parser.ParsePrototype(prototype, commentTypeID);  }

					buildFlags |= BuildFlags.ParsedPrototype;
					}

				return parsedPrototype;
				}
			}


		/* Property: ParsedClassPrototype
		 * If <Prototype> is not null and the comment type is part of the class hierarchy, this will be the prototype in <ParsedClassPrototype>
		 * form.
		 */
		public ParsedClassPrototype ParsedClassPrototype
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Prototype) != 0)
					{  throw new InvalidOperationException("Tried to access ParsedClassPrototype when the prototype was ignored.");  }
				#endif

				if ((buildFlags & BuildFlags.ParsedClassPrototype) == 0)
					{
					if (prototype == null)
						{  parsedClassPrototype = null;  }
					else
						{  parsedClassPrototype = EngineInstance.Languages.FromID(languageID).Parser.ParseClassPrototype(prototype, commentTypeID);  }

					buildFlags |= BuildFlags.ParsedClassPrototype;
					}

				return parsedClassPrototype;
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

				return (ParameterString.GetParametersIndex(title) != -1);
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
				if ((ignoredFields & IgnoreFields.Title) != 0)
					{  throw new InvalidOperationException("Tried to access TitleParameters when the title was ignored.");  }
				#endif

				if ((buildFlags & BuildFlags.TitleParameters) == 0)
					{
					int parametersIndex = ParameterString.GetParametersIndex(title);

					if (parametersIndex == -1)
						{  titleParameters = new ParameterString();  }
					else
						{  titleParameters = ParameterString.FromPlainText(title.Substring(parametersIndex));  }

					buildFlags |= BuildFlags.TitleParameters;
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

				if ((buildFlags & BuildFlags.PrototypeParameters) == 0)
					{
					ParsedPrototype parsedPrototype = this.ParsedPrototype;

					if (parsedPrototype == null || parsedPrototype.NumberOfParameters == 0)
						{  prototypeParameters = new ParameterString();  }
					else
						{
						string[] parameterTypes = new string[parsedPrototype.NumberOfParameters];
						Tokenization.TokenIterator start, end;

						for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
							{
							if (parsedPrototype.GetBaseParameterType(i, out start, out end))
								{  parameterTypes[i] = start.TextBetween(end);  }
							}

						prototypeParameters = ParameterString.FromParameterTypes(parameterTypes);
						}

					buildFlags |= BuildFlags.PrototypeParameters;
					}

				return prototypeParameters;
				}
			}


		/* Property: IsEnum
		 * Whether this topic uses a comment type that has the Enum flag set.  If <CommentTypeID> isn't set this will be false.
		 */
		public bool IsEnum
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CommentTypeID) != 0)
					{  throw new InvalidOperationException("Tried to access IsEnum when the comment type ID was ignored.");  }
				#endif

				return (commentTypeID != 0 && CommentTypes.FromID(commentTypeID).IsEnum == true);
				}
			}


		/* Property: IsGroup
		 * Whether this topic is a group topic.  If <CommentTypeID> isn't set this will be false.
		 */
		public bool IsGroup
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CommentTypeID) != 0)
					{  throw new InvalidOperationException("Tried to access IsGroup when the comment type ID was ignored.");  }
				#endif

				return (commentTypeID != 0 && CommentTypes.GroupCommentTypeID == commentTypeID);
				}
			}


		/* Property: DefinesClass
		 * Whether this topic defines its <ClassString> as opposed to just being a member.
		 */
		public bool DefinesClass
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.CommentTypeID) != 0)
					{  throw new InvalidOperationException("Tried to access DefinesClass when the comment type ID was ignored.");  }
				if ((ignoredFields & IgnoreFields.IsList) != 0)
					{  throw new InvalidOperationException("Tried to access DefinesClass when IsList was ignored.");  }
				#endif

				var commentType = CommentTypes.FromID(commentTypeID);

				return (commentType.InHierarchy && isList == false);
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: manager
		 * The <CommentTypes.Manager> associated with this topic.
		 */
		protected CommentTypes.Manager commentTypes;

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

		/* var: bodyLength
		 * The length of <body>.  This is a separate field because it may be desirable to just have the length of the
		 * body without actually retrieving the body from the database.
		 */
		protected int bodyLength;

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

		/* var: parsedClassPrototype
		 * The <prototype> in <ParsedClassPrototype> form, or null if <prototype> is null, it hasn't been generated yet,
		 * or this isn't appropriate for the comment type.
		 */
		protected ParsedClassPrototype parsedClassPrototype;

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

		/* var: classString
		 * The <ClassString> this topic defines or is a part of.
		 */
		protected ClassString classString;

		/* var: classID
		 * The ID of <classString>, or zero if it's null or its ID is unknown.
		 */
		protected int classID;

		/* var: isList
		 * Whether this is a list topic used to document multiple items.  This will not be set for enums, though it would be
		 * set for a list of enums.
		 */
		protected bool isList;

		/* var: isEmbedded
		 * Whether this topic is embedded in a prior topic.  This is used for entries in lists and enums.  Entries that appear in
		 * definition lists within these topics will get their own <Topic> objects to allow for linking, but they will not appear in
		 * the output because they are already covered by the parent.  Embedded topics will always come after their parent
		 * in a list of topics, so the last non-embedded topic is the parent.
		 */
		protected bool isEmbedded;

		/* var: titleParameters
		 * Any parameters found in the title, as opposed to the prototype.
		 */
		protected ParameterString titleParameters;

		/* var: prototypeParameters
		 * Any parameters found in the prototype.
		 */
		protected ParameterString prototypeParameters;

		/* var: commentTypeID
		 * The ID number of the comment's type, or zero if not specified.
		 */
		protected int commentTypeID;

		/* var: declaredAccessLevel
		 * The declared access level of the topic.  For a public member of an internal class, this would be public.
		 */
		protected Languages.AccessLevel declaredAccessLevel;

		/* var: effectiveAccessLevel
		 * The effective access level of the topic.  For a public member of an internal class, this would be internal.
		 */
		protected Languages.AccessLevel effectiveAccessLevel;

		/* var: tagIDs
		 * A set of the tags applied to this topic.  May or may not be null if there are none.
		 */
		protected IDObjects.NumberSet tagIDs;

		/* var: fileID
		 * The ID of the source file this topic appears in, or zero if not specified.
		 */
		protected int fileID;

		/* var: filePosition
		 * The relative position of the topic in the file, or zero if not specified.  One will be the first topic and subsequent
		 * topics will have higher numbers.  This is done because the final position may not be determinable from
		 * <commentLineNumber> and <codeLineNumber>.
		 */
		protected int filePosition;

		/* var: commentLineNumber
		 * The line number the comment appears on, or zero if not specified or the topic doesn't have one.
		 */
		protected int commentLineNumber;

		/* var: codeLineNumber
		 * The line number the actual code element appears on, or zero if not specified or the topic doesn't have one.
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
		 * The ID of <prototypeContext>, or zero if it's null or its ID is unknown.
		 */
		protected int prototypeContextID;

		/* var: bodyContext
		 * The <ContextString> that all body links should use.
		 */
		protected ContextString bodyContext;

		/* var: bodyContextID
		 * The ID of <bodyContext>, or zero if it's null or its ID is unknown.
		 */
		protected int bodyContextID;

		/* var: ignoredFields
		 * The <IgnoreFields> applied to this object.
		 */
		protected IgnoreFields ignoredFields;

		/* var: buildFlags
		 * Which build-on-demand fields have been generated.
		 */
		protected BuildFlags buildFlags;


		}
	}
