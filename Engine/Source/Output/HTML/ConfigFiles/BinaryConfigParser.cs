/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.ConfigFiles.BinaryConfigParser
 * ____________________________________________________________________________
 *
 * A class to handle loading and saving <Config.nd>.
 *
 *
 * Threading: Not Thread Safe
 *
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.ConfigFiles
	{
	public class BinaryConfigParser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: BinaryConfigParser
		 */
		public BinaryConfigParser ()
			{
			}


		/* Function: Load
		 * Loads the information in <Config.nd> and returns whether it was successful.  If not all the out parameters will still
		 * return objects, they will just be empty.
		 */
		public bool Load (Path filename, out Config.OverridableOutputSettings overridableSettings, out List<Style> styles,
								 out List<FileSourceInfo> fileSourceInfoList)
			{
			overridableSettings = new Config.OverridableOutputSettings();
			styles = new List<Style>();
			fileSourceInfoList = new List<FileSourceInfo>();

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename) == false)
					{
					result = false;
					}
				else if (!binaryFile.Version.IsAtLeastRelease("2.2") &&  // we can support the change in 2.3
						  !binaryFile.Version.IsSamePreRelease(Engine.Instance.Version))
					{
					binaryFile.Close();
					result = false;
					}
				else
					{
					// [String: Project Title or null]
					// [String: Project Subtitle or null]
					// [String: Project Copyright or null]
					// [String: Project Timestamp Code or null]
					// [String: Project Home Page Path (absolute) or null]

					overridableSettings.Title = binaryFile.ReadString();
					overridableSettings.TitlePropertyLocation = Config.PropertySource.PreviousRun;

					overridableSettings.Subtitle = binaryFile.ReadString();
					overridableSettings.SubtitlePropertyLocation = Config.PropertySource.PreviousRun;

					overridableSettings.Copyright = binaryFile.ReadString();
					overridableSettings.CopyrightPropertyLocation = Config.PropertySource.PreviousRun;

					overridableSettings.TimestampCode = binaryFile.ReadString();
					overridableSettings.TimestampCodePropertyLocation = Config.PropertySource.PreviousRun;

					overridableSettings.HomePage = (AbsolutePath)binaryFile.ReadString();
					overridableSettings.HomePagePropertyLocation= Config.PropertySource.PreviousRun;


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


						// [String: OnLoad] [String: OnLoad After Substitutions] [Byte: Page Type] ... [String: null]

						string onLoadStatement = binaryFile.ReadString();

						while (onLoadStatement != null)
							{
							string onLoadAfterSubstitutions;

							// OnLoad After Substitutions is new for 2.3.  Since there were no substitutions in earlier versions, use
							// the same value for them.
							if (binaryFile.Version.IsAtLeastRelease("2.3") ||
								binaryFile.Version.IsSamePreRelease(Engine.Instance.Version))
								{  onLoadAfterSubstitutions = binaryFile.ReadString();  }
							else
								{  onLoadAfterSubstitutions = onLoadStatement;  }

							Engine.Styles.PageType pageType = (Engine.Styles.PageType)binaryFile.ReadByte();

							style.AddOnLoad(onLoadStatement, onLoadAfterSubstitutions, Config.PropertySource.PreviousRun, pageType);

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


						// [String: Home Page Path (absolute) or null]

						string homePage = binaryFile.ReadString();

						if (homePage != null)
							{  style.SetHomePage((AbsolutePath)homePage, Config.PropertySource.PreviousRun);  }


						// Next style path
						stylePath = binaryFile.ReadString();
						}

					overridableSettings.StyleName = styles[styles.Count - 1].Name;
					overridableSettings.StyleNamePropertyLocation = Config.PropertySource.PreviousRun;


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
				{
				result = false;
				}
			finally
				{
				if (binaryFile.IsOpen)
					{  binaryFile.Close();  }
				}

			if (result == false)
				{
				overridableSettings = new Config.OverridableOutputSettings();
				styles.Clear();
				fileSourceInfoList.Clear();
				}

			return result;
			}


		/* Function: Save
		 * Saves the passed information in <Config.nd>.
		 */
		public void Save (Path filename, Config.OverridableOutputSettings overridableSettings, List<Style> styles,
								  List<FileSourceInfo> fileSourceInfoList)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [String: Project Title or null]
				// [String: Project Subtitle or null]
				// [String: Project Copyright or null]
				// [String: Project Timestamp Code or null]
				// [String: Project Home Page Path (absolute) or null]

				binaryFile.WriteString(overridableSettings.Title);
				binaryFile.WriteString(overridableSettings.Subtitle);
				binaryFile.WriteString(overridableSettings.Copyright);
				binaryFile.WriteString(overridableSettings.TimestampCode);
				binaryFile.WriteString(overridableSettings.HomePage);


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


					// [String: OnLoad] [String: OnLoad After Substitutions] [Byte: Page Type] ... [String: null]

					if (style.OnLoad != null)
						{
						foreach (var onLoadStatement in style.OnLoad)
							{
							binaryFile.WriteString(onLoadStatement.Statement);
							binaryFile.WriteString(onLoadStatement.StatementAfterSubstitutions);
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


					// [String: Home Page Path (absolute) or null]

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
