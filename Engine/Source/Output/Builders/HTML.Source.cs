/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.TopicTypes;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________
		
		/* Function: BuildSourceFile
		 * Builds an output file based on a source file.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildSourceFile (int fileID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			accessor.GetReadOnlyLock();
			bool haveDBLock = true;

			try
				{
				IList<Topic> topics = accessor.GetTopicsInFile(fileID, cancelDelegate, CodeDB.Accessor.GetTopicsFlags.ParsePrototypes |
																																			  CodeDB.Accessor.GetTopicsFlags.HighlightPrototypes);
				
				if (cancelDelegate())
					{  return;  }
					
				if (topics.Count == 0)
					{
					accessor.ReleaseLock();
					haveDBLock = false;
					
					DeleteOutputFileIfExists(Source_OutputFile(fileID));
					DeleteOutputFileIfExists(Source_SummaryFile(fileID));
					DeleteOutputFileIfExists(Source_SummaryToolTipsFile(fileID));

					lock (writeLock)
						{
						if (sourceFilesWithContent.Remove(fileID) == true)
							{  buildFlags |= BuildFlags.FileMenu;  }
						}
					}

				else // (topics.Count != 0)
					{
					StringBuilder html = new StringBuilder("\r\n\r\n");
					HTMLTopic topicBuilder = new HTMLTopic(this);

					// Must use a case-insensitive StringSet because IE 6-9 doesn't treat anchors that only differ in case as different.
					StringSet usedAnchors = new StringSet(true, false);
						
					for (int i = 0; i < topics.Count; i++)
						{  
						string extraClass = null;

						if (i == 0)
							{  extraClass = "first";  }
						else if (i == topics.Count - 1)
							{  extraClass = "last";  }

						topicBuilder.Build(topics[i], html, extraClass, usedAnchors);  
						html.Append("\r\n\r\n");
						}
							
					accessor.ReleaseLock();
					haveDBLock = false;

					Path outputPath = Source_OutputFile(fileID);

					// Can't get this from outputPath because it may have substituted characters to satisfy the path restrictions.
					string title = Instance.Files.FromID(fileID).FileName.NameWithoutPath;

					BuildFile(outputPath, title, html.ToString(), PageType.Content);

					HTMLSummary summaryBuilder = new HTMLSummary(this);
					summaryBuilder.Build(topics, title, Source_OutputFileHashPath(fileID), Source_SummaryFile(fileID), Source_SummaryToolTipsFile(fileID));

					lock (writeLock)
						{
						if (sourceFilesWithContent.Add(fileID) == true)
							{  buildFlags |= BuildFlags.FileMenu;  }
						}
					}
				}
				
			finally
				{ 
				if (haveDBLock)
					{  accessor.ReleaseLock();  }
				}
			}


		/* Function: DeleteOutputFileIfExists
		 * If the passed file exists, deletes it and adds its parent folder to <foldersToCheckForDeletion>.  It's okay for the
		 * output path to be null.
		 */
		protected void DeleteOutputFileIfExists (Path outputFile)
			{
			if (outputFile != null && System.IO.File.Exists(outputFile))
				{  
				System.IO.File.Delete(outputFile);  
				foldersToCheckForDeletion.Add(outputFile.ParentFolder);
				}
			}



		// Group: Shared HTML Generation Functions
		// __________________________________________________________________________


		/* Function: BuildWrappedTitle
		 * Builds a title with zero-width spaces added so that long identifiers wrap.  Will also add a span surrounding the qualifiers
		 * with a "qualifier" CSS class.  The HTML will be appended to the StringBuilder, but you must provide your own surrounding
		 * div if required.
		 */
		protected internal void BuildWrappedTitle (string title, int topicTypeID, StringBuilder output)
			{
			MatchCollection splitSymbols = null;

			if (IsFileTopicType(topicTypeID))
				{  splitSymbols = FileSplitSymbolsRegex.Matches(title);  }
			else if (IsCodeTopicType(topicTypeID))
				{  splitSymbols = CodeSplitSymbolsRegex.Matches(title);  }

			int splitCount = (splitSymbols == null ? 0 : splitSymbols.Count);


			// Don't count separators on the end of the string.

			if (splitCount > 0)
				{
				int endOfString = title.Length;

				for (int i = splitCount - 1; i >= 0; i--)
					{
					if (splitSymbols[i].Index + splitSymbols[i].Length == endOfString)
						{
						splitCount--;
						endOfString = splitSymbols[i].Index;
						}
					else
						{  break;  }
					}
				}


			// Build the HTML.

			if (splitCount == 0)
				{
				output.Append(title.ToHTML());
				}
			else
				{
				int appendedSoFar = 0;
				output.Append("<span class=\"qualifier\">");

				for (int i = 0; i < splitCount; i++)
					{
					int endOfSection = splitSymbols[i].Index + splitSymbols[i].Length;
					string titleSection = title.Substring(appendedSoFar, endOfSection - appendedSoFar);
					output.Append( titleSection.ToHTML() );

					if (i < splitCount - 1)
						{
						// Insert a zero-width space for wrapping.  We have to put the final one outside the closing </span> or 
						// Webkit browsers won't wrap on it.
						output.Append("&#8203;");
						}

					appendedSoFar = endOfSection;
					}

				output.Append("</span>&#8203;");  // zero-width space for wrapping

				output.Append( title.Substring(appendedSoFar).ToHTML() );
				}
			}


		/* Function: BuildWrappedTitle
		 * Builds a title with zero-width spaces added so that long identifiers wrap.  Will also add a span surrounding the qualifiers
		 * with a "qualifier" CSS class.  The HTML will be returned as a string, but you must provide your own surrounding div if
		 * required.  If the string will be directly appended to a StringBuilder, it is more efficient to use the other form.
		 */
		protected internal string BuildWrappedTitle (string title, int topicTypeID)
			{
			StringBuilder temp = new StringBuilder();
			BuildWrappedTitle(title, topicTypeID, temp);
			return temp.ToString();
			}


		/* Function: BuildSyntaxHighlightedText
		 */
		protected internal void BuildSyntaxHighlightedText (TokenIterator iterator, TokenIterator end, StringBuilder html)
			{
			while (iterator < end)
				{
				if (iterator.FundamentalType == FundamentalType.LineBreak)
					{
					html.Append("<br />");
					iterator.Next();
					}
				else
					{
					TokenIterator startStretch = iterator;
					TokenIterator endStretch = iterator;
					endStretch.Next();

					SyntaxHighlightingType stretchType = startStretch.SyntaxHighlightingType;

					for (;;)
						{
						if (endStretch == end || endStretch.FundamentalType == FundamentalType.LineBreak)
							{  break;  }
						else if (endStretch.SyntaxHighlightingType == stretchType)
							{  endStretch.Next();  }

						// We can include unhighlighted whitespace if there's content of the same type beyond it.  This prevents
						// unnecessary span tags.
						else if (stretchType != SyntaxHighlightingType.Null &&
									 endStretch.SyntaxHighlightingType == SyntaxHighlightingType.Null &&
									 endStretch.FundamentalType == FundamentalType.Whitespace)
							{
							TokenIterator lookahead = endStretch;

							do 
								{  lookahead.Next();  }
							while (lookahead.SyntaxHighlightingType == SyntaxHighlightingType.Null &&
										lookahead.FundamentalType == FundamentalType.Whitespace &&
										lookahead < end);

							if (lookahead < end && lookahead.SyntaxHighlightingType == stretchType)
								{
								endStretch = lookahead;
								endStretch.Next();
								}
							else
								{  break;  }
							}

						else
							{  break;  }
						}

					switch (stretchType)
						{
						case SyntaxHighlightingType.Comment:
							html.Append("<span class=\"SHComment\">");
							break;
						case SyntaxHighlightingType.Keyword:
							html.Append("<span class=\"SHKeyword\">");
							break;
						case SyntaxHighlightingType.Number:
							html.Append("<span class=\"SHNumber\">");
							break;
						case SyntaxHighlightingType.String:
							html.Append("<span class=\"SHString\">");
							break;
						}

					html.EntityEncodeAndAppend(iterator.Tokenizer.TextBetween(startStretch, endStretch));

					if (stretchType != SyntaxHighlightingType.Null)
						{  html.Append("</span>");  }

					iterator = endStretch;
					}
				}
			}


		/* Function: BuildTypeLinkedAndSyntaxHighlightedText
		 * Converts the text between the iterators to HTML with syntax highlighting applied and any tokens marked with
		 * <PrototypeParsingType.Type> and <PrototypeParsingType.TypeQualifier> having links.  If extendTypeSearch is
		 * true, it will search beyond the bounds of the iterators to get the complete type.  This allows you to format only
		 * a portion of the link with this function yet still have the link go to the complete destination.
		 */
		protected internal void BuildTypeLinkedAndSyntaxHighlightedText (TokenIterator start, TokenIterator end, StringBuilder html,
																															  bool extendTypeSearch = false)
			{
			TokenIterator iterator = start;

			while (iterator < end)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
					 iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
					{
					TokenIterator textStart = iterator;
					TokenIterator textEnd = iterator;

					do
						{  textEnd.Next();  }
					while (textEnd < end &&
								(textEnd.PrototypeParsingType == PrototypeParsingType.Type ||
								 textEnd.PrototypeParsingType == PrototypeParsingType.TypeQualifier) );

					// If the type is a keyword, assume it's a built-in type and thus doesn't get a link.
					if (textStart.SyntaxHighlightingType == SyntaxHighlightingType.Keyword)
						{
						BuildSyntaxHighlightedText(textStart, textEnd, html);
						}
					else
						{
						TokenIterator symbolStart = textStart;
						TokenIterator symbolEnd = textEnd;

						if (extendTypeSearch && symbolStart == start)
							{
							TokenIterator temp = symbolStart;
							temp.Previous();

							while (temp.IsInBounds &&
										(temp.PrototypeParsingType == PrototypeParsingType.Type ||
										 temp.PrototypeParsingType == PrototypeParsingType.TypeQualifier))
								{
								symbolStart = temp;
								temp.Previous();
								}
							}

						if (extendTypeSearch && symbolEnd == end)
							{
							while (symbolEnd.IsInBounds &&
										(symbolEnd.PrototypeParsingType == PrototypeParsingType.Type ||
										 symbolEnd.PrototypeParsingType == PrototypeParsingType.TypeQualifier))
								{  symbolEnd.Next();  }
							}

						html.Append("<a href=\"about:"); //xxx
						html.EntityEncodeAndAppend(start.Tokenizer.TextBetween(symbolStart, symbolEnd));
						html.Append("\">");
						BuildSyntaxHighlightedText(textStart, textEnd, html);
						html.Append("</a>");
						}

					iterator = textEnd;
					}

				else // not on a type
					{
					TokenIterator startText = iterator;

					do
						{  iterator.Next();  }
					while (iterator < end && 
								iterator.PrototypeParsingType != PrototypeParsingType.Type &&
								iterator.PrototypeParsingType != PrototypeParsingType.TypeQualifier);

					BuildSyntaxHighlightedText(startText, iterator, html);
					}
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Function: Source_OutputFolder
		 * Returns the output folder of the passed file source number and, if specified, the folder within it.  If the folder is null
		 * it returns the root output folder for the file source number.
		 */
		public Path Source_OutputFolder (int number, Path relativeFolder = default(Path))
			{
			StringBuilder result = new StringBuilder(OutputFolder);
			result.Append("/files");  

			if (number != 1)
				{  result.Append(number);  }
					
			if (relativeFolder != null)
				{
				result.Append('/');
				result.Append(SanitizePath(relativeFolder));
				}

			return result.ToString();
			}


		/* Function: Source_OutputFolderHashPath
		 * Returns the hash path of the output folder of the passed file source number and, if specified, the folder within it.
		 * If the folder is null it returns the root output folder hash path for the file source number.  The hash path will always
		 * include a trailing symbol so that the file name can simply be concatenated.
		 */
		public string Source_OutputFolderHashPath (int number, Path relativeFolder = default(Path))
			{
			StringBuilder result = new StringBuilder("File");

			if (number != 1)
				{  result.Append(number);  }

			result.Append(':');

			// Since we're building a string we can't rely on Path to simplify out ./					
			if (relativeFolder != null && relativeFolder != ".")
				{
				result.Append(SanitizePath(relativeFolder).ToURL());
				result.Append('/');
				}

			return result.ToString();
			}


		/* Function: Source_OutputFile
		 * Returns the output path of the passed source file ID, or null if none.  It may be null if the <FileSource> that created
		 * it no longer exists.
		 */
		public Path Source_OutputFile (int fileID)
			{
			Files.File file = Engine.Instance.Files.FromID(fileID);
			Files.FileSource fileSource = Engine.Instance.Files.FileSourceOf(file);

			if (fileSource == null)
				{  return null;  }

			Path relativePath = fileSource.MakeRelative(file.FileName);

			return Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + 
						 Source_OutputFileNameOnly(relativePath.NameWithoutPath);
			}


		/* Function: Source_OutputFileHashPath
		 * Returns the hash path of the passed source file ID, or null if none.  It may be null if the <FileSource> that created
		 * it no longer exists.
		 */
		public string Source_OutputFileHashPath (int fileID)
			{
			Files.File file = Engine.Instance.Files.FromID(fileID);
			Files.FileSource fileSource = Engine.Instance.Files.FileSourceOf(file);

			if (fileSource == null)
				{  return null;  }

			Path relativePath = fileSource.MakeRelative(file.FileName);

			// OutputFolderHashPath already includes the trailing symbol so we don't need + '/' +
			return Source_OutputFolderHashPath(fileSource.Number, relativePath.ParentFolder) + 
						 Source_OutputFileNameOnlyHashPath(relativePath.NameWithoutPath);
			}


		/* Function: Source_OutputFileNameOnly
		 * Returns the output file name of the passed file.  Any path attached to it will be ignored and not included in the result.
		 */
		public static Path Source_OutputFileNameOnly (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();

			// We can't have dots in the file name because Apache will try to execute Script.pl.html even though .pl is not
			// the last extension.  Dots in folder names are okay though.
			nameString = nameString.Replace('.', '-');
			
			nameString = SanitizePathString(nameString);
			return nameString + ".html";
			}


		/* Function: Source_OutputFileNameOnlyHashPath
		 * Returns the hash path of the passed file.  Any path attached to it will be ignored and not included in the result.
		 */
		public string Source_OutputFileNameOnlyHashPath (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();
			return SanitizePath(nameString);
			}


		/* Function: Source_SummaryFile
		 * Returns the summary file path of the passed source file ID, or null if none.  It may be null if the <FileSource> that 
		 * created it no longer exists.
		 */
		public Path Source_SummaryFile (int fileID)
			{
			Files.File file = Engine.Instance.Files.FromID(fileID);
			Files.FileSource fileSource = Engine.Instance.Files.FileSourceOf(file);

			if (fileSource == null)
				{  return null;  }

			Path relativePath = fileSource.MakeRelative(file.FileName);

			return Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + 
						 Source_SummaryFileNameOnly(relativePath.NameWithoutPath);
			}


		/* Function: Source_SummaryFileNameOnly
		 * Returns the summary file name of the passed file.  Any path attached to it will be ignored and not included in the result.
		 */
		public static Path Source_SummaryFileNameOnly (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();

			// This is just for consistency with Source_OutputFileNameOnly.  I'm not sure if we actually need it.
			nameString = nameString.Replace('.', '-');
			
			nameString = SanitizePathString(nameString);
			return nameString + "-Summary.js";
			}


		/* Function: Source_SummaryToolTipsFile
		 * Returns the summary tooltips file path of the passed source file ID, or null if none.  It may be null if the <FileSource> 
		 * that created it no longer exists.
		 */
		public Path Source_SummaryToolTipsFile (int fileID)
			{
			Files.File file = Engine.Instance.Files.FromID(fileID);
			Files.FileSource fileSource = Engine.Instance.Files.FileSourceOf(file);

			if (fileSource == null)
				{  return null;  }

			Path relativePath = fileSource.MakeRelative(file.FileName);

			return Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + 
						 Source_SummaryToolTipsFileNameOnly(relativePath.NameWithoutPath);
			}


		/* Function: Source_SummaryToolTipsFileNameOnly
		 * Returns the summary tooltips file name of the passed file.  Any path attached to it will be ignored and not included in the result.
		 */
		public static Path Source_SummaryToolTipsFileNameOnly (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();

			// This is just for consistency with Source_OutputFileNameOnly.  I'm not sure if we actually need it.
			nameString = nameString.Replace('.', '-');
			
			nameString = SanitizePathString(nameString);
			return nameString + "-SummaryToolTips.js";
			}


		/* Function: Source_Anchor
		 * Returns an anchor built from the passed <Topic>.  If you supply a usedAnchors set, it will avoid duplicating anything
		 * found in it.  The return value will be added to it automatically.
		 */
		public static string Source_Anchor (Topic topic, bool includeClass = true, Collections.StringSet usedAnchors = null)
			{
			StringBuilder anchor;

			// We want to work from Topic.Title instead of Topic.Symbol so that we can use the separator characters as originally
			// written, as opposed to having them normalized and condensed in the anchor.  This means we have to strip the parethesis
			// ourselves as they'll be attached later via Topic.Parameters.  If we didn't and the parenthesis came from the title instead
			// of the prototype we'd end up with two copies in the anchor.
			int parenthesisIndex = Symbols.ParameterString.GetEndingParenthesisIndex(topic.Title);

			if (parenthesisIndex == -1)
				{  anchor = new StringBuilder(topic.Title);  }
			else
				{
				anchor = new StringBuilder(parenthesisIndex);
				anchor.Append(topic.Title, 0, parenthesisIndex);
				}

			anchor.Replace('\t', ' ');

			// Remove percent signs so they don't conflict with URI encoding.
			anchor.Replace('%', ' ');

			// Remove all whitespace unless it separates two text characters.  Replace the ones that remain with underscores.
			int i = 0;
			while (i < anchor.Length)
				{
				if (anchor[i] == ' ')
					{
					if (i == 0 || i == anchor.Length - 1)
						{  anchor.Remove(i, 1);  }
					else if (Tokenizer.FundamentalTypeOf(anchor[i - 1]) == FundamentalType.Text &&
								 Tokenizer.FundamentalTypeOf(anchor[i + 1]) == FundamentalType.Text)
						{
						anchor[i] = '_';
						i++;
						}
					else
						{  anchor.Remove(i, 1);  }
					}
				else
					{  i++;  }
				}

			// Add parameters if present
			if (topic.Parameters != null)
				{
				string parameterString = topic.Parameters.ToString();

				// Doesn't need to be any more elaborate than this because we can assume tab characters and unnecessary spaces
				// have already been filtered out by ParameterString's normalization.
				parameterString = parameterString.Replace(' ', '_');
				parameterString = parameterString.Replace(Symbols.ParameterString.SeparatorChar, ',');

				anchor.Append('(');
				anchor.Append(parameterString);
				anchor.Append(')');
				}

			// Add class if present and desired.
			// xxx when class id is included in topic test for that here
			if (includeClass)
				{
				// Find the part of the symbol that isn't generated by the title, if any.
				string ignore;
				string titleSymbol = SymbolString.FromPlainText(topic.Title, out ignore).ToString();
				string fullSymbol = topic.Symbol.ToString();

				if (titleSymbol.Length < fullSymbol.Length && 
					 fullSymbol.Substring(fullSymbol.Length - titleSymbol.Length) == titleSymbol)
					{
					string classSymbol = fullSymbol.Substring(0, fullSymbol.Length - titleSymbol.Length);
					string memberOperator = Engine.Instance.Languages.FromID(topic.LanguageID).MemberOperator;
					classSymbol = classSymbol.Replace(SymbolString.SeparatorChar.ToString(), memberOperator);

					// The class symbol should already have a trailing member operator.
					anchor.Insert(0, classSymbol);
					}
				}

			// Entity encode so we don't have to worry about quotes and such appearing in the anchor and ruining its use
			// in tags.
			string anchorString = anchor.ToString().EntityEncode();

			// Avoid duplicates
			if (usedAnchors != null)
				{
				if (usedAnchors.Contains(anchorString))
					{
 					int number = 2;
					string extendedAnchorString;

					do
						{  
						extendedAnchorString = anchorString + '(' + number + ')';
						number++;
						}
					while (usedAnchors.Contains(extendedAnchorString));

					anchorString = extendedAnchorString;
					}

				usedAnchors.Add(anchorString);
				}

			return anchorString;
			}



		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________
		
		
		override public void OnAddTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			lock (writeLock)
				{
				sourceFilesToRebuild.Add(topic.FileID);
				}
			}

		override public void OnUpdateTopic (Topic oldTopic, int newCommentLineNumber, int newCodeLineNumber, string newBody, 
															CodeDB.EventAccessor eventAccessor)
			{
			lock (writeLock)
				{
				sourceFilesToRebuild.Add(oldTopic.FileID);
				}
			}

		override public void OnDeleteTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			lock (writeLock)
				{
				sourceFilesToRebuild.Add(topic.FileID);
				}
			}



		// Group: Static Variables
		// __________________________________________________________________________


		static protected Regex.Output.HTML.FileSplitSymbols FileSplitSymbolsRegex = new Regex.Output.HTML.FileSplitSymbols();
		static protected Regex.Output.HTML.CodeSplitSymbols CodeSplitSymbolsRegex = new Regex.Output.HTML.CodeSplitSymbols();

		}
	}

