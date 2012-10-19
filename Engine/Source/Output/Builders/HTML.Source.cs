/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
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
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildSourceFile() when the accessor already holds a database lock.");  }
			#endif

			accessor.GetReadOnlyLock();
			bool haveDBLock = true;

			try
				{
				IList<Topic> topics = accessor.GetTopicsInFile(fileID, cancelDelegate);
				
				if (cancelDelegate())
					{  return;  }
				
					
				// Delete the file if there are no topics.

				if (topics.Count == 0)
					{
					accessor.ReleaseLock();
					haveDBLock = false;
					
					DeleteOutputFileIfExists(Source_OutputFile(fileID));
					DeleteOutputFileIfExists(Source_ToolTipsFile(fileID));
					DeleteOutputFileIfExists(Source_SummaryFile(fileID));
					DeleteOutputFileIfExists(Source_SummaryToolTipsFile(fileID));

					lock (writeLock)
						{
						if (sourceFilesWithContent.Remove(fileID) == true)
							{  buildFlags |= BuildFlags.BuildMenu;  }
						}
					}

				
				// Build the file if it has topics
				
				else
					{

					// Get links and their targets

					// We can't skip looking up classes and contexts here.  Later code will be trying to compare generated 
					// links to the ones in this list and that requires them having all their properties.
					IList<Link> links = accessor.GetLinksInFile(fileID, cancelDelegate);

					if (cancelDelegate())
						{  return;  }

					IDObjects.SparseNumberSet linkTargetIDs = new IDObjects.SparseNumberSet();

					foreach (Link link in links)
						{
						if (link.IsResolved)
							{  linkTargetIDs.Add(link.TargetTopicID);  }
						}

					IList<Topic> linkTargets = accessor.GetTopicsByID(linkTargetIDs, cancelDelegate);

					if (cancelDelegate())
						{  return;  }

					// We also need to get any links appearing inside the link targets.  Wut?  When you have a resolved link, 
					// a tooltip shows up when you hover over it.  The tooltip is built from the link targets we just retrieved.  
					// However, if the summary appearing in the tooltip contains any Natural Docs links, we need to know if
					// they're resolved and how to know what text to show (originaltext, named links, etc.)  Links don't store
					// which topic they appear in, but they do store the file, so gather the file IDs of the link targets that
					// have Natural Docs links in the summaries and get all the links in those files.

					IDObjects.SparseNumberSet inceptionFileIDs = new IDObjects.SparseNumberSet();

					foreach (Topic linkTarget in linkTargets)
						{
						if (linkTarget.Summary != null && linkTarget.Summary.IndexOf("<link type=\"naturaldocs\"") != -1)
							{  inceptionFileIDs.Add(linkTarget.FileID);  }
						}

					IList<Link> inceptionLinks = null;
					
					if (!inceptionFileIDs.IsEmpty)
						{  
						// Can't skip looking up classes and contexts here either.
						inceptionLinks = accessor.GetNaturalDocsLinksInFiles(inceptionFileIDs, cancelDelegate);  
						}

					if (cancelDelegate())
						{  return;  }

					accessor.ReleaseLock();
					haveDBLock = false;


					// Build the HTML for the list of topics

					StringBuilder html = new StringBuilder("\r\n\r\n");
					HTMLTopic topicBuilder = new HTMLTopic(this);

					// We don't put embedded topics in the output, so we need to find the last non-embedded one so
					// that the "last" CSS tag is correctly applied.
					int lastNonEmbeddedTopic = topics.Count - 1;
					while (lastNonEmbeddedTopic > 0 && topics[lastNonEmbeddedTopic].IsEmbedded == true)
						{  lastNonEmbeddedTopic--;  }

					for (int i = 0; i <= lastNonEmbeddedTopic; i++)
						{  
						string extraClass = null;

						if (i == 0)
							{  extraClass = "first";  }
						else if (i == lastNonEmbeddedTopic)
							{  extraClass = "last";  }

						if (topics[i].IsEmbedded == false)
							{
							topicBuilder.Build(topics[i], links, linkTargets, html, topics, i + 1, extraClass);  
							html.Append("\r\n\r\n");
							}
						}
							

					// Build the full HTML files

					Path outputPath = Source_OutputFile(fileID);

					// Can't get this from outputPath because it may have substituted characters to satisfy the path restrictions.
					string title = Instance.Files.FromID(fileID).FileName.NameWithoutPath;

					BuildFile(outputPath, title, html.ToString(), PageType.Content);


					// Build the tooltips file

					using (System.IO.StreamWriter file = CreateTextFileAndPath(Source_ToolTipsFile(fileID)))
						{
						file.Write("NDContentPage.OnToolTipsLoaded({");

						#if DONT_SHRINK_FILES
							file.WriteLine();
						#endif

						for (int i = 0; i < linkTargets.Count; i++)
							{
							Topic topic = linkTargets[i];
							string toolTipHTML = topicBuilder.BuildToolTip(topic, inceptionLinks);

							if (toolTipHTML != null)
								{
								#if DONT_SHRINK_FILES
									file.Write("   ");
								#endif

								file.Write(topic.TopicID);
								file.Write(":\"");
								file.Write(toolTipHTML.StringEscape());
								file.Write('"');

								if (i != linkTargets.Count - 1)
									{  file.Write(',');  }

								#if DONT_SHRINK_FILES
									file.WriteLine();
								#endif
								}
							}

						#if DONT_SHRINK_FILES
							file.Write("   ");
						#endif
						file.Write("});");
						}


					// Build summary and summary tooltips metadata files

					HTMLSummary summaryBuilder = new HTMLSummary(this);
					summaryBuilder.Build(topics, links, title, 
														 Source_OutputFileHashPath(fileID), Source_SummaryFile(fileID), Source_SummaryToolTipsFile(fileID));

					lock (writeLock)
						{
						if (sourceFilesWithContent.Add(fileID) == true)
							{  buildFlags |= BuildFlags.BuildMenu;  }
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

				lock (writeLock)
					{
					foldersToCheckForDeletion.Add(outputFile.ParentFolder);
					buildFlags |= BuildFlags.CheckFoldersForDeletion;
					}
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
				result.Append(SanitizePath(relativeFolder.ToURL()));
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
		public Path Source_OutputFileNameOnly (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();
			return SanitizePath(nameString, true) + ".html";
			}


		/* Function: Source_OutputFileNameOnlyHashPath
		 * Returns the hash path of the passed file.  Any path attached to it will be ignored and not included in the result.
		 */
		public string Source_OutputFileNameOnlyHashPath (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();
			return SanitizePath(nameString);
			}


		/* Function: Source_ToolTipsFile
		 * Returns the tooltips file path of the passed source file ID, or null if none.  It may be null if the <FileSource> 
		 * that created it no longer exists.
		 */
		public Path Source_ToolTipsFile (int fileID)
			{
			Files.File file = Engine.Instance.Files.FromID(fileID);
			Files.FileSource fileSource = Engine.Instance.Files.FileSourceOf(file);

			if (fileSource == null)
				{  return null;  }

			Path relativePath = fileSource.MakeRelative(file.FileName);

			return Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + 
						 Source_ToolTipsFileNameOnly(relativePath.NameWithoutPath);
			}


		/* Function: Source_ToolTipsFileNameOnly
		 * Returns the tooltips file name of the passed file.  Any path attached to it will be ignored and not included in the result.
		 */
		public Path Source_ToolTipsFileNameOnly (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();
			return SanitizePath(nameString, true) + "-ToolTips.js";
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
		public Path Source_SummaryFileNameOnly (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();
			return SanitizePath(nameString, true) + "-Summary.js";
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
		public Path Source_SummaryToolTipsFileNameOnly (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();
			return SanitizePath(nameString, true) + "-SummaryToolTips.js";
			}


		/* Function: Source_TopicHashPath
		 * Returns a hash path representing a specific <Topic> within an output file.
		 */
		public static string Source_TopicHashPath (Topic topic, bool includeClass = true)
			{
			// We want to work from Topic.Title instead of Topic.Symbol so that we can use the separator characters as originally
			// written, as opposed to having them normalized and condensed in the anchor.

			int titleParenthesesIndex = Symbols.ParameterString.GetEndingParenthesesIndex(topic.Title);

			StringBuilder hashPath;
			if (titleParenthesesIndex == -1)
				{
				hashPath = new StringBuilder(topic.Title);
				}
			else
				{
				hashPath = new StringBuilder(titleParenthesesIndex);
				hashPath.Append(topic.Title, 0, titleParenthesesIndex);
				}

			hashPath.Replace('\t', ' ');

			// Remove all whitespace unless it separates two text characters.
			int i = 0;
			while (i < hashPath.Length)
				{
				if (hashPath[i] == ' ')
					{
					if (i == 0 || i == hashPath.Length - 1)
						{  hashPath.Remove(i, 1);  }
					else if (Tokenizer.FundamentalTypeOf(hashPath[i - 1]) == FundamentalType.Text &&
								 Tokenizer.FundamentalTypeOf(hashPath[i + 1]) == FundamentalType.Text)
						{  i++;  }
					else
						{  hashPath.Remove(i, 1);  }
					}
				else
					{  i++;  }
				}

			// Add parentheses to distinguish between multiple symbols in the same file.
			// xxx this will be a problem when doing class hash paths as symboldefnumber is only unique to a file
			if (topic.SymbolDefinitionNumber != 1)
				{
				hashPath.Append('(');
				hashPath.Append(topic.SymbolDefinitionNumber);
				hashPath.Append(')');
				}

			// Add class if present and desired.
			// xxx when class id is included in topic test for that here, maybe instead of having a flag
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

					// We only support :: and . in hash paths.  Default to . for anything else.
					if (memberOperator != "::")
						{  memberOperator = ".";  }

					classSymbol = classSymbol.Replace(SymbolString.SeparatorChar.ToString(), memberOperator);

					// The class symbol should already have a trailing member operator.
					hashPath.Insert(0, classSymbol);
					}
				}

			return SanitizePath(hashPath.ToString());
			}



		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________
		
		
		override public void OnAddTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			lock (writeLock)
				{
				sourceFilesToRebuild.Add(topic.FileID);

				if (topic.ClassID != 0)
					{  classFilesToRebuild.Add(topic.ClassID);  }
				}
			}

		override public void OnUpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags, 
																		 CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about line number changes.  They don't affect the output.  We also don't care about context
			// changes.  They might affect links but if they do it will be handled in OnChangeLinkTarget().
			changeFlags &= ~(Topic.ChangeFlags.CommentLineNumber | Topic.ChangeFlags.CodeLineNumber |
											 Topic.ChangeFlags.PrototypeContext | Topic.ChangeFlags.BodyContext);

			if (changeFlags != 0)
				{
				lock (writeLock)
					{
					sourceFilesToRebuild.Add(oldTopic.FileID);

					if (oldTopic.ClassID != 0)
						{  classFilesToRebuild.Add(oldTopic.ClassID);  }
					if (newTopic.ClassID != 0)
						{  classFilesToRebuild.Add(newTopic.ClassID);  }
					}

				// If the summary or prototype changed this means its tooltip changed.  Rebuild any file that contains links 
				// to this topic.
				if ((changeFlags & (Topic.ChangeFlags.Prototype | Topic.ChangeFlags.Summary | 
													Topic.ChangeFlags.LanguageID | Topic.ChangeFlags.TopicTypeID)) != 0)
					{
					IDObjects.SparseNumberSet fileIDs;
					eventAccessor.GetInfoOnLinksThatResolveToTopicID(oldTopic.TopicID, out fileIDs);

					if (fileIDs != null)
						{  
						lock (writeLock)
							{  sourceFilesToRebuild.Add(fileIDs);  }
						}
					}
				}
			}

		override public void OnDeleteTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			lock (writeLock)
				{
				sourceFilesToRebuild.Add(topic.FileID);

				if (topic.ClassID != 0)
					{  classFilesToRebuild.Add(topic.ClassID);  }
				}
			}

		override public void OnAddLink (Link link, CodeDB.EventAccessor eventAccessor)
			{
			}
		
		override public void OnChangeLinkTarget (Link link, int oldTargetTopicID, CodeDB.EventAccessor eventAccessor)
			{
			// If this is triggered by the first resolution of a new link, we don't have to do anything.  The file that contains this
			// link must have changed to create it, so it should already be on the build list.  If it appears in the summary of any
			// topics, that means the summary must have changed since this link didn't exist before and rebuilding any tooltips 
			// will be handled by the topic events.
			if (oldTargetTopicID == UnresolvedTargetTopicID.NewLink)
				{  return;  }

			sourceFilesToRebuild.Add(link.FileID);

			// If this is a Natural Docs link, see if it appears in the summary for any topics.  This would mean that it appears in
			// these topics' tooltips, so we have to find any links to these topics and rebuild the files those links appear in.

			// Why do we have to do this if links aren't used in tooltips?  Because how it's resolved can affect it's appearance.
			// It will show up as "link" versus "<link>" if it's resolved or not, and "a at b" versus "a" depending on if it resolves to
			// the topic "a at b" or the topic "b".

			// Why don't we do this for type links?  Because unlike Natural Docs links, type links don't change in appearance
			// based on whether they're resolved or not.  Therefore the logic that we don't have to worry about it because links
			// don't appear in tooltips holds true.

			if (link.Type == LinkType.NaturalDocs)
				{
				IDObjects.SparseNumberSet fileIDs;
				eventAccessor.GetInfoOnLinksToTopicsWithNDLinkInSummary(link, out fileIDs);

				if (fileIDs != null)
					{  sourceFilesToRebuild.Add(fileIDs);  }
				}
			}
		
		override public void OnDeleteLink (Link link, CodeDB.EventAccessor eventAccessor)
			{
			}



		// Group: Static Variables
		// __________________________________________________________________________


		static protected Regex.Output.HTML.FileSplitSymbols FileSplitSymbolsRegex = new Regex.Output.HTML.FileSplitSymbols();
		static protected Regex.Output.HTML.CodeSplitSymbols CodeSplitSymbolsRegex = new Regex.Output.HTML.CodeSplitSymbols();

		}
	}

