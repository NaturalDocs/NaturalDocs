/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Config_nd
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Config.nd>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 * 
 * File: Config.nd
 * 
 *		A file used to store information about the configuration as of last time this output target was built.
 *		
 *		> [String: Project Title or null]
 *		> [String: Project Subtitle or null]
 *		> [String: Project Copyright or null]
 *		> [String: Project Timestamp Code or null]
 *		
 *		The project info as of the last run.  If any of the properties were not set they will be null.
 *		
 *		> [String: Style Path]
 *		>    [String: Inherit] ... [String: null]
 *		>    [String: OnLoad] [Byte: Page Type] ... [String: null]
 *		>    [String: Link] [Byte: Page Type] ... [String: null]
 *		>    [String: Home Page or null]
 *		>
 *		> [String: Style Path]
 *		> ...
 *		> [String: null]
 *		
 *		Stores the list of styles that apply to this target in the order in which they must be loaded.  Each one starts as
 *		a path, which is either to the style's CSS file or <Style.txt>.  These are stored instead of the names so that if a
 *		name is interpreted differently from one run to the next it will be detected.
 *		
 *		The properties are a null-terminated list of inherit statements, then a null-terminated list of OnLoad statements
 *		each followed by a page type byte, then a null terminated list of Link statements each followed by a page type
 *		byte, then the path to the custom home page file or rull if it's not defined.  There will not be a page type byte
 *		following the null strings that end the OnLoad and Link lists.
 *		
 *		> [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
 *		> [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
 *		> ...
 *		> [Int32: 0]
 *		>
 *		> [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
 *		> [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
 *		> ...
 *		> [Int32: 0]
 *		
 *		Stores all the <FileSource> IDs and what their numbers are.  This allows us to purge the related output folders if
 *		one is deleted or changes.
 *		
 *	
 *		Revision History:
 *		
 *			- 2.2
 *				- Added the project title, subtitle, copyright, and timestamp code.
 *				- Added the properties of each style.  Previously it only stored each one's style path.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public class Config_nd
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Config_nd
		 */
		public Config_nd ()
			{
			}


		/* Function: Load
		 * Loads the information in <Config.nd> and returns whether it was successful.  If not all the out parameters will still 
		 * return objects, they will just be empty.  
		 */
		public bool Load (Path filename, out Config.ProjectInfo projectInfo, out List<Style> styles, out List<FileSourceInfo> fileSourceInfoList)
			{
			projectInfo = new Config.ProjectInfo();
			styles = new List<Style>();
			fileSourceInfoList = new List<FileSourceInfo>();

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename, "2.2") == false)
					{  result = false;  }
				else
					{
					// [String: Project Title or null]
					// [String: Project Subtitle or null]
					// [String: Project Copyright or null]
					// [String: Project Timestamp Code or null]

					projectInfo.Title = binaryFile.ReadString();
					projectInfo.TitlePropertyLocation = Config.PropertySource.PreviousRun;

					projectInfo.Subtitle = binaryFile.ReadString();
					projectInfo.SubtitlePropertyLocation = Config.PropertySource.PreviousRun;

					projectInfo.Copyright = binaryFile.ReadString();
					projectInfo.CopyrightPropertyLocation = Config.PropertySource.PreviousRun;

					projectInfo.TimestampCode = binaryFile.ReadString();
					projectInfo.TimestampCodePropertyLocation = Config.PropertySource.PreviousRun;


					// [String: Style Path]
					//    (properties)
					// [String: Style Path]
 					// ...
 					// [String: null]

					string stylePath = binaryFile.ReadString();

					while (stylePath != null)
						{
						Style style;

						if (stylePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
							{  style = new Styles.CSSOnly(stylePath);  }
						else
							{  style = new Styles.Advanced(stylePath);  }

						styles.Add(style);


						// [String: Inherit] ... [String: null]

						string inheritStatement = binaryFile.ReadString();

						while (inheritStatement != null)
							{
							// Find the name in the list of styles so we can connect the objects together properly.  There should only
							// be one style per name so we can just compare by name.  Also, this list is stored in the order in which 
							// they must be applied, which means inherited styles will appear before the ones that inherit from them,
							// so we can search the list we've built so far instead of waiting until they're all loaded.
							Style matchingStyle = null;

							for (int i = 0; i < styles.Count; i++)
								{
								if (string.Compare(inheritStatement, styles[i].Name, StringComparison.OrdinalIgnoreCase) == 0)
									{
									matchingStyle = styles[i];
									break;
									}
								}

							// If there's no match just add it as null.
							style.AddInheritedStyle(inheritStatement, Config.PropertySource.PreviousRun, matchingStyle);

							inheritStatement = binaryFile.ReadString();
							}


						// [String: OnLoad] [Byte: Page Type] ... [String: null]

						string onLoadStatement = binaryFile.ReadString();

						while (onLoadStatement != null)
							{
							Engine.Styles.PageType pageType = (Engine.Styles.PageType)binaryFile.ReadByte();
							style.AddOnLoad(onLoadStatement, Config.PropertySource.PreviousRun, pageType);

							onLoadStatement = binaryFile.ReadString();
							}


						// [String: Link] [Byte: Page Type] ... [String: null]

						string linkStatement = binaryFile.ReadString();

						while (linkStatement != null)
							{
							Engine.Styles.PageType pageType = (Engine.Styles.PageType)binaryFile.ReadByte();
							style.AddLinkedFile(linkStatement, Config.PropertySource.PreviousRun, pageType);

							linkStatement = binaryFile.ReadString();
							}


						// [String: Home Page or null]

						string homePage = binaryFile.ReadString();

						if (homePage != null)
							{  style.SetHomePage(homePage, Config.PropertySource.PreviousRun);  }


						// Next style path
						stylePath = binaryFile.ReadString();
						}

					projectInfo.StyleName = styles[styles.Count - 1].Name;
					projectInfo.StyleNamePropertyLocation = Config.PropertySource.PreviousRun;


					// [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
					// [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
					// ...
					// [Int32: 0]

					FileSourceInfo fileSourceInfo = new FileSourceInfo();
					fileSourceInfo.Type = Files.InputType.Source;

					for (;;)
						{
						fileSourceInfo.Number = binaryFile.ReadInt32();

						if (fileSourceInfo.Number == 0)
							{  break;  }

						fileSourceInfo.UniqueIDString = binaryFile.ReadString();
						fileSourceInfoList.Add(fileSourceInfo);
						}

					// [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
					// [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
					// ...
					// [Int32: 0]

					fileSourceInfo.Type = Files.InputType.Image;

					for (;;)
						{
						fileSourceInfo.Number = binaryFile.ReadInt32();

						if (fileSourceInfo.Number == 0)
							{  break;  }

						fileSourceInfo.UniqueIDString = binaryFile.ReadString();
						fileSourceInfoList.Add(fileSourceInfo);
						}
					}
				}
			catch
				{  result = false;  }
			finally
				{  binaryFile.Dispose();  }

			if (result == false)
				{
				projectInfo = new Config.ProjectInfo();
				styles.Clear();
				fileSourceInfoList.Clear();
				}

			return result;
			}


		/* Function: Save
		 * Saves the passed information in <Config.nd>.
		 */
		public void Save (Path filename, Config.ProjectInfo projectInfo, List<Style> styles, List<FileSourceInfo> fileSourceInfoList)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [String: Project Title or null]
				// [String: Project Subtitle or null]
				// [String: Project Copyright or null]
				// [String: Project Timestamp Code or null]

				binaryFile.WriteString(projectInfo.Title);
				binaryFile.WriteString(projectInfo.Subtitle);
				binaryFile.WriteString(projectInfo.Copyright);
				binaryFile.WriteString(projectInfo.TimestampCode);


				// [String: Style Path]
				//    (properties)
				// [String: Style Path]
 				// ...
 				// [String: null]

				foreach (var style in styles)
					{
					if (style is Styles.CSSOnly)
						{  binaryFile.WriteString( (style as Styles.CSSOnly).CSSFile );  }
					else if (style is Styles.Advanced)
						{  binaryFile.WriteString( (style as Styles.Advanced).ConfigFile );  }
					else
						{  throw new NotImplementedException();  }


					// [String: Inherit] ... [String: null]

					if (style.Inherits != null)
						{
						foreach (var inheritStatement in style.Inherits)
							{  binaryFile.WriteString(inheritStatement.Name);  }
						}
					
					binaryFile.WriteString(null);


					// [String: OnLoad] [Byte: Page Type] ... [String: null]

					if (style.OnLoad != null)
						{
						foreach (var onLoadStatement in style.OnLoad)
							{
							binaryFile.WriteString(onLoadStatement.Statement);
							binaryFile.WriteByte((byte)onLoadStatement.Type);
							}
						}

					binaryFile.WriteString(null);


					// [String: Link] [Byte: Page Type] ... [String: null]

					if (style.Links != null)
						{
						foreach (var linkStatement in style.Links)
							{
							binaryFile.WriteString(linkStatement.File);
							binaryFile.WriteByte((byte)linkStatement.Type);
							}
						}

					binaryFile.WriteString(null);


					// [String: Home Page or null]

					binaryFile.WriteString(style.HomePage);
					}

				// End of style paths
				binaryFile.WriteString(null);


				// [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
				// [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
				// ...
				// [Int32: 0]

				foreach (FileSourceInfo fileSourceInfo in fileSourceInfoList)
					{
					if (fileSourceInfo.Type == Files.InputType.Source)
						{
						binaryFile.WriteInt32(fileSourceInfo.Number);
						binaryFile.WriteString(fileSourceInfo.UniqueIDString);
						}
					}

				binaryFile.WriteInt32(0);

				// [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
				// [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
				// ...
				// [Int32: 0]

				foreach (FileSourceInfo fileSourceInfo in fileSourceInfoList)
					{
					if (fileSourceInfo.Type == Files.InputType.Image)
						{
						binaryFile.WriteInt32(fileSourceInfo.Number);
						binaryFile.WriteString(fileSourceInfo.UniqueIDString);
						}
					}

				binaryFile.WriteInt32(0);
				}
			}

		}
	}