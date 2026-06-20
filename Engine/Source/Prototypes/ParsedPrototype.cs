/*
 * Class: CodeClear.NaturalDocs.Engine.Prototypes.ParsedPrototype
 * ____________________________________________________________________________
 *
 * A class that wraps a <Tokenizer> for a prototype that's been marked with <PrototypeParsingTypes>, providing easier
 * access to things like parameter lines.
 *
 * Usage:
 *
 *		The functions and properties obviously rely on the relevant tokens being set.  You cannot expect a proper result from things
 *		like <ParameterSection.NumberOfParameters> unless the tokens are marked with <PrototypeParsingType.StartOfParams>,
 *		<PrototypeParsingType.ParamSeparator>, etc.  Likewise, you can't get anything from <ParameterSection.GetParameterName()>
 *		unless you also have tokens marked with <PrototypeParsingType.Name>.  However, you can set the parameter divider tokens,
 *		call <ParameterSection.GetParameterBounds()>, and then use those bounds to further parse the parameter and set tokens like
 *		<PrototypeParsingType.Name>.
 *
 *		Section and parameter divisions are not calculated on the fly.  They are calculated once at object creation and then saved.
 *		If you make changes to section or parameter delimiting tokens call <RecalculateSections()> to make sure the changes are
 *		reflected in the other functions.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Diagnostics;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Prototypes
	{
	[DebuggerDisplay("{DebuggerDisplay}")]
	public class ParsedPrototype
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: ParsedPrototype
		 * Creates a new parsed prototype.
		 */
		public ParsedPrototype (Tokenizer prototype, int languageID, int commentTypeID, Engine.Instance engineInstance)
			{
			tokenizer = prototype;
			sections = null;

			parameterStyle = engineInstance.Languages.FromID(languageID).ParameterStyle;

			this.engineInstance = engineInstance;
			this.languageID = languageID;
			this.commentTypeID = commentTypeID;

			RecalculateSections();
			}


		/* Function: GetAccessLevel
		 * Returns the <Languages.AccessLevel> if it can be determined by the prototype.  This should only be used with basic
		 * language support as it's not as reliable as the results from the dedicated language parsers.
		 */
		public Languages.AccessLevel GetAccessLevel ()
			{
			// Just return the first section that's able to return a value
			foreach (var section in sections)
				{
				var accessLevel = section.GetAccessLevel();

				if (accessLevel != Languages.AccessLevel.Unknown)
					{  return accessLevel;  }
				}

			return Languages.AccessLevel.Unknown;
			}


		/* Function: RecalculateSections
		 *
		 * Recalculates the <Sections> list.  This is automatically called by the constructor so you only need to call this manually if you made
		 * changes to these token types after creating this object.
		 *
		 * Sections are delimited with <PrototypeParsingType.StartOfPrototypeSection> and <PrototypeParsingType.EndOfPrototypeSection>.
		 * Neither of these token types are required to appear, and if they do not the entire prototype will be in one section.  Also, they are
		 * not required to appear together.  Sections can be delimited by only start tokens or only end tokens, whichever is most convenient
		 * to the language parser and won't interfere with marking other types.
		 *
		 * Each section containing <PrototypeParsingType.StartOfParams> or similar will generate a <ParameterSection>.  All others will generate
		 * a regular <Section>.
		 */
		virtual public void RecalculateSections ()
			{
			if (sections == null)
				{  sections = new List<Section>(1);  }
			else
				{  sections.Clear();  }

			TokenIterator startOfSection = tokenizer.FirstToken;
			TokenIterator endOfSection = startOfSection;

			for (;;)
				{

				// Find the section bounds

				bool sectionIsEmpty = true;
				bool sectionHasParams = false;

				while (endOfSection.IsInBounds)
					{
					// End the section if we find the start of a new one, but not if it's just the first token of the current one
					if (endOfSection.PrototypeParsingType == PrototypeParsingType.StartOfPrototypeSection &&
						endOfSection > startOfSection)
						{  break;  }

					// End the section if we're starting parameters when the current section already has some
					if (StartOfParamsTypes.Contains(endOfSection.PrototypeParsingType) &&
						sectionHasParams)
						{  break;  }

					// At this point we know the current token will be part of the section
					if (endOfSection.FundamentalType != FundamentalType.Whitespace)
						{  sectionIsEmpty = false; }

					if (StartOfParamsTypes.Contains(endOfSection.PrototypeParsingType))
						{
						sectionHasParams = true;
						endOfSection.Next();
						}
					else if (endOfSection.PrototypeParsingType == PrototypeParsingType.EndOfPrototypeSection)
						{
						endOfSection.Next();
						break;
						}
					else
						{
						endOfSection.Next();
						}
					}


				// Process the section

				if (!sectionIsEmpty)
					{
					endOfSection.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfSection);
					startOfSection.NextPastWhitespace(endOfSection);

					if (sectionHasParams)
						{  sections.Add( new ParameterSection(startOfSection, endOfSection, this) );  }
					else
						{  sections.Add( new Section(startOfSection, endOfSection) );  }
					}


				// Continue?

				if (endOfSection.IsInBounds)
					{  startOfSection = endOfSection;  }
				else
					{  break;  }
				}


			// Sanity check.  This should only happen if all the sections were whitespace, which shouldn't normally happen but I
			// suppose could with a manual prototype.

			if (sections.Count < 1)
				{  sections.Add( new Section(tokenizer.FirstToken, tokenizer.EndOfTokens) );  }
			}



		// Group: Parsing Support Functions
		// __________________________________________________________________________


		/* Function: GetEndOfBlock
		 *
		 * If the iterator is on one of the following opening token types, returns a reference to the closing token and to the end of
		 * the block, which is after the last symbol of the closing token.  It will handle any nested blocks.  If the iterator isn't on an
		 * appropriate token or it couldn't find the end of the block it will return false.
		 *
		 * Supported Block Types:
		 *
		 *		- <PrototypeParsingType.OpeningTypeModifier>
		 *		- <PrototypeParsingType.OpeningParamModifier>
		 *		- <PrototypeParsingType.StartOfTuple>
		 */
		public static bool GetEndOfBlock (TokenIterator openingSymbol, out TokenIterator closingSymbol, out TokenIterator endOfBlock)
			{
			return GetEndOfBlock(openingSymbol, openingSymbol.Tokenizer.EndOfTokens, out closingSymbol, out endOfBlock);
			}


		/* Function: GetEndOfBlock
		 *
		 * If the iterator is on one of the following opening token types, returns a reference to the closing token and to the end of
		 * the block, which is after the last symbol of the closing token.  It will handle any nested blocks.  If the iterator isn't on an
		 * appropriate token or it couldn't find the end of the block it will return false.
		 *
		 * Supported Block Types:
		 *
		 *		- <PrototypeParsingType.OpeningTypeModifier>
		 *		- <PrototypeParsingType.OpeningParamModifier>
		 *		- <PrototypeParsingType.StartOfTuple>
		 */
		public static bool GetEndOfBlock (TokenIterator openingSymbol, TokenIterator limit, out TokenIterator closingSymbol,
														 out TokenIterator endOfBlock)
			{
			if (openingSymbol.PrototypeParsingType != PrototypeParsingType.OpeningTypeModifier &&
				openingSymbol.PrototypeParsingType != PrototypeParsingType.OpeningParamModifier &&
				openingSymbol.PrototypeParsingType != PrototypeParsingType.StartOfTuple)
				{
				closingSymbol = openingSymbol;
				endOfBlock = openingSymbol;
				return false;
				}

			TokenIterator iterator = openingSymbol;
			iterator.Next();
			int level = 1;

			// We're going to cheat and assume all blocks are balanced and nested in a valid way. This lets us handle them all
			// with a simple level count.
			for (;;)
				{
				if (iterator >= limit)
					{
					closingSymbol = openingSymbol;
					endOfBlock = openingSymbol;
					return false;
					}
				else if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						   iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier ||
						   iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
					{
					level++;
					}
				else if (iterator.PrototypeParsingType == PrototypeParsingType.ClosingTypeModifier ||
						   iterator.PrototypeParsingType == PrototypeParsingType.ClosingParamModifier ||
						   iterator.PrototypeParsingType == PrototypeParsingType.EndOfTuple)
					{
					level--;

					if (level == 0)
						{  break;  }
					}

				iterator.Next();
				}

			closingSymbol = iterator;
			iterator.Next();

			while (iterator.PrototypeParsingType == PrototypeParsingType.ClosingExtensionSymbol)
				{  iterator.Next();  }

			endOfBlock = iterator;

			return true;
			}


		/* Function: TryToSkipBlock
		 *
		 * If the iterator is on one of the following opening token types, moves the iterator past the entire block including
		 * any nested blocks and returns true.
		 *
		 * Supported Block Types:
		 *
		 *		- <PrototypeParsingType.OpeningTypeModifier>
		 *		- <PrototypeParsingType.OpeningParamModifier>
		 *		- <PrototypeParsingType.StartOfTuple>
		 */
		public static bool TryToSkipBlock (ref TokenIterator iterator)
			{
			return TryToSkipBlock(ref iterator, iterator.Tokenizer.EndOfTokens);
			}


		/* Function: TryToSkipBlock
		 *
		 * If the iterator is on one of the following opening token types, moves the iterator past the entire block including
		 * any nested blocks and returns true.
		 *
		 * Supported Block Types:
		 *
		 *		- <PrototypeParsingType.OpeningTypeModifier>
		 *		- <PrototypeParsingType.OpeningParamModifier>
		 *		- <PrototypeParsingType.StartOfTuple>
		 */
		public static bool TryToSkipBlock (ref TokenIterator iterator, TokenIterator limit)
			{
			TokenIterator closingSymbol, endOfBlock;

			if (GetEndOfBlock(iterator, limit, out closingSymbol, out endOfBlock))
				{
				iterator = endOfBlock;
				return true;
				}
			else
				{  return false;  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Tokenizer
		 * The tokenized prototype.
		 */
		public Tokenizer Tokenizer
			{
			get
				{  return tokenizer;  }
			}


		/* Property: Sections
		 * The list of <Sections> making up the prototype.
		 */
		public List<Section> Sections
			{
			get
				{  return sections;  }
			}


		/* Property: MainParameterSection
		 * Returns the <ParameterSection> that should be used for things like parentheses in Natural Docs links, or null if
		 * there isn't one.  There may be multiple parameter sections so calling code should decide whether this is good
		 * enough or if it should go through the individual <Sections> so that it can check all of them.
		 */
		public ParameterSection MainParameterSection
			{
			get
				{
				foreach (var section in Sections)
					{
					// Will be null if not a ParameterSection.  Will not throw an exception.
					var parameterSection = section as ParameterSection;

					// Return the first ParameterSection with StartOfParams.  We don't want to use other types like
					// StartOfMetadataParams.
					if (parameterSection != null &&
						parameterSection.StartingParameterType == PrototypeParsingType.StartOfParams)
						{
						return parameterSection;
						}
					}

				return null;
				}
			}


		/* Property: ParameterStyle
		 * The format of the prototype's parameters, such as C-style ("int x") or Pascal-style ("x: int").  This should only be set if the
		 * associated language's parameter style is <ParameterStyle.Unknown>.
		 */
		public ParameterStyle ParameterStyle
			{
			get
				{  return parameterStyle;  }
			set
				{
				#if DEBUG
				if (engineInstance.Languages.FromID(languageID).ParameterStyle != ParameterStyle.Unknown)
					{  throw new Exception("You should not set a prototype parameter style manually unless its language's style is unknown.");  }
				#endif

				parameterStyle = value;
				}
			}


		/* Property: LanguageID
		 * The language ID associated with this prototype.
		 */
		public int LanguageID
			{
			get
				{  return languageID;  }
			}


		/* Property: CommentTypeID
		 * The comment type ID associated with this prototype.
		 */
		public int CommentTypeID
			{
			get
				{  return commentTypeID;  }
			}


		/* Property: SupportsImpliedTypes
		 * Whether the prototype's language supports implied types.
		 */
		public bool SupportsImpliedTypes
			{
			get
				{  return engineInstance.Languages.FromID(languageID).ImpliedParameterTypes;  }
			}


		/* Property: EngineInstance
		 * The <Engine.Instance> associated with this prototype.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{  return engineInstance;  }
			}


		/* Property: DebuggerDisplay
		 * Shows the string contents when debugging Natural Docs.
		 */
		 internal string DebuggerDisplay
			{
			get
				{  return tokenizer.DebuggerDisplay;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: tokenizer
		 * The <Tokenizer> containing the full prototype.
		 */
		protected Tokenizer tokenizer;

		/* var: sections
		 * A list of <Sections> representing chunks of the prototype, or null if it hasn't been calculated yet.
		 */
		protected List<Section> sections;

		/* var: parameterStyle
		 * The format of the prototype's parameters, such as C-style ("int x") or Pascal-style ("x: int").  This is needed as a separate
		 * variable so it can be detected and set if the Language object's <ParameterStyle> is <ParameterStyle.Unknown>.
		 */
		protected ParameterStyle parameterStyle;

		/* var: languageID
		 * The language ID associated with this prototype.
		 */
		protected int languageID;

		/* var: commentTypeID
		 * The comment type ID associated with this prototype.
		 */
		protected int commentTypeID;

		/* var: engineInstance
		 * The <Engine.Instance> associated with this prototype.
		 */
		protected Engine.Instance engineInstance;



		// Group: Static Variables
		// __________________________________________________________________________


		/* Constant: StartOfParamsTypes
		 * An array of all the <PrototypeParsingTypes>, such as <PrototypeParsingType.StartOfParams> and
		 * <PrototypeParsingType.StartOfTemplateParams>.  These will be in order of importance, so when matching parameter
		 * names you should use parameter sections that appear earlier on this list first.
		 */
		public static PrototypeParsingType[] StartOfParamsTypes = {
			PrototypeParsingType.StartOfParams,
			PrototypeParsingType.StartOfAccessors,
			PrototypeParsingType.StartOfTemplateParams,
			PrototypeParsingType.StartOfMetadataParams
			};

		}
	}
