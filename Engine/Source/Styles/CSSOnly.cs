/* 
 * Class: CodeClear.NaturalDocs.Engine.Styles.CSSOnly
 * ____________________________________________________________________________
 * 
 * A class representing a style that is only a single CSS file.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	public class CSSOnly : Style
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: CSSOnly
		 * Pass the location of the .css file.
		 */
		public CSSOnly (Path cssFile) : base ()
			{
			this.cssFile = cssFile;
			}


		/* Function: Contains
		 * Returns whether this style contains the passed file.
		 */
		override public bool Contains (Path file)
			{  
			return (file == cssFile);  
			}


		/* Function: MakeRelative
		 * Converts the passed filename to one relative to this style.  If this style doesn't contain the file, it will return null.
		 */
		override public Path MakeRelative (Path file)
			{
			if (file == cssFile)
				{  return file.NameWithoutPath;  }
			else
				{  return null;  }
			}


		/* Function: IsSameFundamentalStyle
		 * Returns whether this style is fundamentally the same as the passed one, meaning any identifying properties will be the
		 * same (i.e. both referencing the same style folder) but secondary properties may be different.
		 */
		override public bool IsSameFundamentalStyle (Style other)
			{
			return (other is Styles.CSSOnly && cssFile == (other as Styles.CSSOnly).cssFile);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Name
		 */
		override public string Name
			{
			get
				{  return cssFile.NameWithoutPathOrExtension;  }
			}


		/* Property: CSSFile
		 * A path to the style's CSS file.
		 */
		public Path CSSFile
			{
			get
				{  return cssFile;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: cssFile
		 * The path to the style's CSS file.
		 */
		protected Path cssFile;

		}
	}