/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.ShebangScript
 * ____________________________________________________________________________
 * 
 * A container that parses the first line of a file for shebang (#!) strings and then uses that to determinewhich parser to send the rest of the file to.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.IO;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class ShebangScript : Parser
		{
	
		public ShebangScript (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}

		override public ParseResult Parse (Path filePath, int fileID, CancelDelegate cancelDelegate, 
													  out IList<Topic> topics, out LinkSet classParentLinks)
			{
			topics = null;
			classParentLinks = null;

			StreamReader file = null;
			Language language = null;
			string content = null;
				
			try
				{
				var fileInfo = EngineInstance.Files.FromPath(filePath);

				// file may be null when running unit tests
				if (file == null || fileInfo.AutoDetectUnicodeEncoding)
					{
					file = new StreamReader(filePath.ToString(), detectEncodingFromByteOrderMarks: true);
					}
				else
					{
					var encoding = System.Text.Encoding.GetEncoding(fileInfo.CharacterEncodingID);
					file = new StreamReader(filePath.ToString(), encoding);
					}

				// If there's no shebang line we treat it as a successful parse with no content.
				if ((char)file.Read() != '#')
					{  return ParseResult.Success;  }
				if ((char)file.Read() != '!')
					{  return ParseResult.Success;  }

				// If there's a shebang then it should be safe to use ReadLine().  There's a chance extensionless files are binary so
				// we don't want to use ReadLine() right away in case it returns the entire file and the file is huge.
				string shebangLine = file.ReadLine();

				language = Manager.FromShebangLine(shebangLine);

				if (language != null)
					{  content = file.ReadToEnd() ?? "";  }
				}

			catch (System.IO.FileNotFoundException)
				{  return ParseResult.FileDoesntExist;  }
			catch (System.IO.DirectoryNotFoundException)
				{  return ParseResult.FileDoesntExist;  }
			catch
				{  return ParseResult.CantAccessFile;  }
			finally
				{
				if (file != null)
					{  
					file.Dispose();  
					file = null;
					}
				}

			if (language == null)
				{  return ParseResult.Success;  }

			// Since we ate the first line, start the tokenizer at line 2.
			Tokenizer tokenizedContent = new Tokenizer(content, startingLineNumber: 2, tabWidth: EngineInstance.Config.TabWidth);

			return language.Parser.Parse(tokenizedContent, fileID, cancelDelegate, out topics, out classParentLinks);
			}

		}
	}