/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 * 
 * File: Source Metadata
 * 
 *		Each source file that has a content file built for it will also have a metadata file.  It's in the same location and 
 *		has the same file name, only substituting .html for .js.  When executed, this file will pass the source file's title to
 *		<NDFramePage.OnPageTitleLoaded()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________
		
		/* Function: BuildSourceFile
		 * Builds an output file based on a source file.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata file.
		 */
		protected void BuildSourceFile (int fileID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			accessor.GetReadOnlyLock();
			bool haveDBLock = true;

			try
				{
				IList<Topic> topics = accessor.GetTopicsInFile(fileID, cancelDelegate);
				
				if (cancelDelegate())
					{  return;  }
					
				if (topics.Count == 0)
					{
					accessor.ReleaseLock();
					haveDBLock = false;
					
					Path outputFile = Source_OutputFile(fileID);

					if (outputFile != null && System.IO.File.Exists(outputFile))
						{  
						System.IO.File.Delete(outputFile);
						foldersToCheckForDeletion.Add(outputFile.ParentFolder);
						}

					Path metadataFile = Source_MetaDataFile(fileID);

					if (metadataFile != null && System.IO.File.Exists(metadataFile))
						{  
						System.IO.File.Delete(metadataFile);  
						foldersToCheckForDeletion.Add(metadataFile.ParentFolder);
						}

					lock (writeLock)
						{
						if (sourceFilesWithContent.Remove(fileID) == true)
							{  buildFlags |= BuildFlags.FileMenu;  }
						}
					}

				else // (topics.Count != 0)
					{
					StringBuilder html = new StringBuilder("\r\n\r\n");
						
					for (int i = 0; i < topics.Count; i++)
						{  
						string extraClass = null;

						if (i == 0)
							{  extraClass = "first";  }
						else if (i == topics.Count - 1)
							{  extraClass = "last";  }

						BuildTopic(topics[i], html, extraClass);  
						html.Append("\r\n\r\n");
						}
							
					accessor.ReleaseLock();
					haveDBLock = false;

					Path outputPath = Source_OutputFile(fileID);

					// Can't get this from outputPath because it may have substituted characters to satisfy the path restrictions.
					string title = Instance.Files.FromID(fileID).FileName.NameWithoutPath;

					BuildFile(outputPath, title, html.ToString(), PageType.Content);

					BuildMetaData(fileID, topics, title);

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


		/* Function: BuildTopic
		 */
		protected void BuildTopic (Topic topic, StringBuilder html, string extraClass = null)
			{
			string topicTypeName = Instance.TopicTypes.FromID(topic.TopicTypeID).SimpleIdentifier;
			string languageName = Instance.Languages.FromID(topic.LanguageID).SimpleIdentifier;

			html.Append(
				"<div class=\"CTopic T" + topicTypeName + " L" + languageName + (extraClass == null ? "" : ' ' + extraClass) + "\">" +

					"\r\n ");
					BuildTitle(topic, html);

					#if SHOW_NDMARKUP
						if (topic.Body != null)
							{
							html.Append(
							"\r\n " +
							"<div class=\"CBodyNDMarkup\">" +
								topic.Body.ToHTML() +
							"</div>");
							}
					#endif

					if (topic.Prototype != null)
						{
						html.Append("\r\n ");
						BuildPrototype(topic, html);
						}

					if (topic.Body != null)
						{
						html.Append("\r\n ");
						BuildBody(topic, html);
						}

				html.Append(
				"\r\n" +
				"</div>"
				);
			}


		/* Function: BuildTitle
		 */
		protected void BuildTitle (Topic topic, StringBuilder html)
			{
			MatchCollection splitSymbols = null;

			if (topic.TopicTypeID == fileTopicTypeID)
				{  splitSymbols = FileSplitSymbolsRegex.Matches(topic.Title);  }
			else if (nonCodeTopicTypeIDs.Contains(topic.TopicTypeID) == false)
				{  splitSymbols = CodeSplitSymbolsRegex.Matches(topic.Title);  }

			int splitCount = (splitSymbols == null ? 0 : splitSymbols.Count);


			// Don't count separators on the end of the string.

			if (splitCount > 0)
				{
				int endOfString = topic.Title.Length;

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

			html.Append("<div class=\"CTitle\">");

			if (splitCount == 0)
				{
				html.Append( topic.Title.ToHTML() );
				}
			else
				{
				int appendedSoFar = 0;
				html.Append("<span class=\"qualifier\">");

				for (int i = 0; i < splitCount; i++)
					{
					int endOfSection = splitSymbols[i].Index + splitSymbols[i].Length;
					string titleSection = topic.Title.Substring(appendedSoFar, endOfSection - appendedSoFar);
					html.Append( titleSection.ToHTML() );

					if (i < splitCount - 1)
						{
						// Insert a zero-width space for wrapping.  We have to put the final one outside the closing </span> or 
						// Webkit browsers won't wrap on it.
						html.Append("&#8203;");
						}

					appendedSoFar = endOfSection;
					}

				html.Append("</span>");
				html.Append("&#8203;");  // zero-width space for wrapping

				html.Append( topic.Title.Substring(appendedSoFar).ToHTML() );
				}

			html.Append("</div>");
			}


		/* Function: BuildPrototype
		 */
		protected void BuildPrototype (Topic topic, StringBuilder html)
			{
			html.Append("<div class=\"NDPrototype\">");

			Language language = Engine.Instance.Languages.FromID(topic.LanguageID);

			if (language == null)
				{  html.EntityEncodeAndAppend(topic.Prototype);  }
			else
				{
				ParsedPrototype prototype = language.ParsePrototype(topic.Prototype);
				TokenIterator start, end;
				
				prototype.GetBeforeParameters(out start, out end);
				html.EntityEncodeAndAppend( prototype.Tokenizer.TextBetween(start, end) );

				for (int i = 0; i < prototype.NumberOfParameters; i++)
					{
					html.Append("<br>&nbsp;&nbsp;&nbsp;"); //xxx
					prototype.GetParameter(i, out start, out end);
					html.EntityEncodeAndAppend( prototype.Tokenizer.TextBetween(start, end) );
					}

				if (prototype.GetAfterParameters(out start, out end))
					{
					html.Append("<br>&nbsp;&nbsp;&nbsp;");
					html.EntityEncodeAndAppend( prototype.Tokenizer.TextBetween(start, end) );
					}
				}

			html.Append("</div>");
			}


		/* Function: BuildBody
		 */
		protected void BuildBody (Topic topic, StringBuilder html)
			{
			html.Append("<div class=\"CBody\">");

			NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Body);

			while (iterator.IsInBounds)
				{
				switch (iterator.Type)
					{
					case NDMarkup.Iterator.ElementType.Text:
						if (topic.Body.IndexOf("  ", iterator.RawTextIndex, iterator.Length) == -1)
							{  iterator.AppendTo(html);  }
						else
							{  html.Append( iterator.String.ConvertMultipleWhitespaceChars() );  }
						break;

					case NDMarkup.Iterator.ElementType.ParagraphTag:
					case NDMarkup.Iterator.ElementType.BulletListTag:
					case NDMarkup.Iterator.ElementType.BulletListItemTag:
					case NDMarkup.Iterator.ElementType.BoldTag:
					case NDMarkup.Iterator.ElementType.ItalicsTag:
					case NDMarkup.Iterator.ElementType.UnderlineTag:
					case NDMarkup.Iterator.ElementType.LTEntityChar:
					case NDMarkup.Iterator.ElementType.GTEntityChar:
					case NDMarkup.Iterator.ElementType.AmpEntityChar:
					case NDMarkup.Iterator.ElementType.QuoteEntityChar:
						iterator.AppendTo(html);
						break;

					case NDMarkup.Iterator.ElementType.HeadingTag:
						if (iterator.IsOpeningTag)
							{  html.Append("<div class=\"CHeading\">");  }
						else
							{  html.Append("</div>");  }
						break;

					case NDMarkup.Iterator.ElementType.PreTag:
						// Because we can assume the NDMarkup is valid, we can assume it's an opening tag and that we will run
						// into a closing tag.

						html.Append("<pre>");

						for (;;)
							{
							iterator.Next();

							if (iterator.Type == NDMarkup.Iterator.ElementType.PreTag)
								{  break;  }
							else
								{  
								// Includes PreLineBreakTags
								iterator.AppendTo(html);  
								}
							}

						html.Append("</pre>");
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListTag:
						if (iterator.IsOpeningTag)
							{  html.Append("<table class=\"CDefinitionList\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">");  }
						else
							{  html.Append("</table>");  }
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListEntryTag:
						if (iterator.IsOpeningTag)
							{  html.Append("<tr><td class=\"CDLEntry\">");  }
						else
							{  html.Append("</td>");  }
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListDefinitionTag:
						if (iterator.IsOpeningTag)
							{  html.Append("<td class=\"CDLDefinition\">");  }
						else
							{  html.Append("</td></tr>");  }
						break;

					case NDMarkup.Iterator.ElementType.LinkTag:
						string type = iterator.Property("type");

						if (type == "email")
							{  BuildEMailLink(iterator, html);  }
						else if (type == "url")
							{  BuildURLLink(iterator, html);  }
						else // type == "naturaldocs"
							{  BuildNaturalDocsLink(iterator, html);  }

						break;

					case NDMarkup.Iterator.ElementType.ImageTag: // xxx
						html.Append( "<i>" + iterator.String.ToHTML() + "</i>" );
						break;
					}

				iterator.Next();
				}

			html.Append("</div>");
			}


		/* Function: BuildEMailLink
		 */
		protected void BuildEMailLink (NDMarkup.Iterator iterator, StringBuilder html)
			{
			string address = iterator.Property("target");
			int atIndex = address.IndexOf('@');
			int cutPoint1 = atIndex / 2;
			int cutPoint2 = (atIndex+1) + ((address.Length - (atIndex+1)) / 2);
			
			html.Append("<a href=\"#\" onclick=\"javascript:location.href='ma\\u0069'+'lto\\u003a'+'");
			html.Append( EMailSegmentForJavaScriptString( address.Substring(0, cutPoint1) ));
			html.Append("'+'");
			html.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint1, atIndex - cutPoint1) ));
			html.Append("'+'\\u0040'+'");
			html.Append( EMailSegmentForJavaScriptString( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
			html.Append("'+'");
			html.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint2, address.Length - cutPoint2) ));
			html.Append("';return false;\">");

			string text = iterator.Property("text");

			if (text != null)
				{  html.EntityEncodeAndAppend(text);  }
			else
				{
				html.Append( EMailSegmentForHTML( address.Substring(0, cutPoint1) ));
				html.Append("<span style=\"display: none\">[xxx]</span>");
				html.Append( EMailSegmentForHTML( address.Substring(cutPoint1, atIndex - cutPoint1) ));
				html.Append("<span>&#64;</span>");
				html.Append( EMailSegmentForHTML( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
				html.Append("<span style=\"display: none\">[xxx]</span>");
				html.Append( EMailSegmentForHTML( address.Substring(cutPoint2, address.Length - cutPoint2) ));
				}

			html.Append("</a>");
			}

		/* Function: EMailSegmentForJavaScriptString
		 */
		protected string EMailSegmentForJavaScriptString (string segment)
			{
			segment = segment.StringEscape();
			segment = segment.Replace(".", "\\u002e");
			return segment;
			}

		/* Function: EMailSegmentForHTML
		 */
		protected string EMailSegmentForHTML (string segment)
			{
			segment = segment.EntityEncode();
			segment = segment.Replace(".", "&#46;");
			return segment;
			}

		/* Function: BuildURLLink
		 */
		protected void BuildURLLink (NDMarkup.Iterator iterator, StringBuilder html)
			{
			string target = iterator.Property("target");
			html.Append("<a href=\"");
				html.EntityEncodeAndAppend(target);
			html.Append("\" target=\"_top\">");

			string text = iterator.Property("text");

			if (text != null)
				{  html.EntityEncodeAndAppend(text);  }
			else
				{
				int startIndex = 0;
				int breakIndex;

				// Skip the protocol and any following slashes since we don't want a break after every slash in http:// or
				// file:///.

				int endOfProtocolIndex = target.IndexOf(':');

				if (endOfProtocolIndex != -1)
					{
					do
						{  endOfProtocolIndex++;  }
					while (endOfProtocolIndex < target.Length && target[endOfProtocolIndex] == '/');

					html.EntityEncodeAndAppend( target.Substring(0, endOfProtocolIndex) );
					html.Append("&#8203;");  // Zero width space
					startIndex = endOfProtocolIndex;
					}

				for (;;)
					{
					breakIndex = target.IndexOfAny(breakURLCharacters, startIndex);

					if (breakIndex == -1)
						{
						if (target.Length - startIndex > maxUnbrokenURLCharacters)
							{  breakIndex = startIndex + maxUnbrokenURLCharacters;  }
						else
							{  break;  }
						}
					else if (breakIndex - startIndex > maxUnbrokenURLCharacters)
						{  breakIndex = startIndex + maxUnbrokenURLCharacters;  }

					html.EntityEncodeAndAppend( target.Substring(startIndex, breakIndex - startIndex) );
					html.Append("&#8203;");  // Zero width space
					html.EntityEncodeAndAppend(target[breakIndex]);

					startIndex = breakIndex + 1;
					}

				html.EntityEncodeAndAppend( target.Substring(startIndex) );
				}

			html.Append("</a>");
			}


		/* Function: BuildNaturalDocsLink
		 */
		protected void BuildNaturalDocsLink (NDMarkup.Iterator iterator, StringBuilder html)
			{
			// xxx
			html.EntityEncodeAndAppend(iterator.Property("originaltext"));
			}


		/* Function: BuildMetaData
		 */
		protected void BuildMetaData (int fileID, IList<Topic> topics, string title)
			{
			string hashPath = Source_OutputFileHashPath(fileID);

			System.IO.StreamWriter metadataFile = CreateTextFileAndPath( Source_MetaDataFile(fileID) );

			try
				{
				metadataFile.Write(
					"NDFramePage.OnPageTitleLoaded(\"" + hashPath.StringEscape() + "\", \"" + title.StringEscape() + "\");"
					);
				}
			finally
				{
				metadataFile.Dispose();
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
					
			if (relativeFolder != null)
				{
				result.Append(SanitizePath(relativeFolder).ToURL());
				result.Append('/');
				}

			return result.ToString();
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


		/* Function: Source_MetaDataFileNameOnly
		 * Returns the metadata file name of the passed file.  Any path attached to it will be ignored and not included in the result.
		 */
		public static Path Source_MetaDataFileNameOnly (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();

			// This is just for consistency with Source_OutputFileNameOnly.  I'm not sure if we actually need it.
			nameString = nameString.Replace('.', '-');
			
			nameString = SanitizePathString(nameString);
			return nameString + ".js";
			}


		/* Function: Source_OutputFileNameOnlyHashPath
		 * Returns the hash path of the passed file.  Any path attached to it will be ignored and not included in the result.
		 */
		public string Source_OutputFileNameOnlyHashPath (Path filename)
			{
			string nameString = filename.NameWithoutPath.ToString();
			return SanitizePath(nameString);
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


		/* Function: Source_MetaDataFile
		 * Returns the metadata file path of the passed source file ID, or null if none.  It may be null if the <FileSource> that 
		 * created it no longer exists.
		 */
		public Path Source_MetaDataFile (int fileID)
			{
			Files.File file = Engine.Instance.Files.FromID(fileID);
			Files.FileSource fileSource = Engine.Instance.Files.FileSourceOf(file);

			if (fileSource == null)
				{  return null;  }

			Path relativePath = fileSource.MakeRelative(file.FileName);

			return Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + 
						 Source_MetaDataFileNameOnly(relativePath.NameWithoutPath);
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

		}
	}

