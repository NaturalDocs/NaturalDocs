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
					HTMLTopic topicBuilder = new HTMLTopic(this);
						
					for (int i = 0; i < topics.Count; i++)
						{  
						string extraClass = null;

						if (i == 0)
							{  extraClass = "first";  }
						else if (i == topics.Count - 1)
							{  extraClass = "last";  }

						topicBuilder.Build(topics[i], html, extraClass);  
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

