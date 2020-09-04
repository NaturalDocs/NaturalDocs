/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.ImageFile
 * ____________________________________________________________________________
 * 
 * A class containing information about an image file.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class ImageFile : File
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		/* Function: ImageFile
		 * Creates a new ImageFile with unknown dimensions.
		 */
		public ImageFile (Path newFileName, DateTime newLastModified) : base (newFileName, FileType.Image, newLastModified)
			{
			width = 0;
			height = 0;
			}
			
		/* Function: ImageFile
		 * Creates a new ImageFile with known dimensions.
		 */
		public ImageFile (Path newFileName, DateTime newLastModified, uint width, uint height) : base (newFileName, FileType.Image, newLastModified)
			{
			#if DEBUG
			if (width <= 0 || height <= 0)
				{  throw new Exception("Tried to create ImageFile with invalid dimensions.");  }
			#endif

			this.width = width;
			this.height = height;
			}
			
		/* Function: CreateSnapshotOfProperties
		 * Creates a duplicate File object that contains all the file's properties at the time this function was called.
		 * The duplicate will not change so it can be used to compare to the original File object later to see if any 
		 * of the properties have changed.
		 */
		override public File CreateSnapshotOfProperties ()
			{
			ImageFile duplicate = new ImageFile (fileName, lastModified);
			duplicate.ID = ID;
			duplicate.deleted = deleted;

			// We do this here instead of using the other constructor to avoid the validation check
			duplicate.width = width;
			duplicate.height = height;

			return duplicate;
			}

			
		// Group: Properties
		// __________________________________________________________________________

		/* Property: DimensionsKnown
		 * Whether the image's dimensions are known.
		 */
		public bool DimensionsKnown
			{
			get
				{  return (width > 0 && height > 0);  }
			}

		/* Property: Width
		 * The image's width.  Check <DimensionsKnown> before reading.  In debug builds attempting to read while
		 * <DimensionsKnown> is false will throw an exception.
		 */
		public uint Width
			{
			get
				{
				#if DEBUG
				if (DimensionsKnown == false)
					{  throw new Exception("Tried to read ImageFile.Width while DimensionsKnown was false.");  }
				#endif

				return width;
				}
			set
				{  width = value;  }
			}

		/* Property: Height
		 * The image's height.  Check <DimensionsKnown> before reading.  In debug builds attempting to read while
		 * <DimensionsKnown> is false will throw an exception.
		 */
		public uint Height
			{
			get
				{
				#if DEBUG
				if (DimensionsKnown == false)
					{  throw new Exception("Tried to read ImageFile.Height while DimensionsKnown was false.");  }
				#endif

				return height;
				}
			set
				{  height = value;  }
			}
		
			
		// Group: Variables
		// __________________________________________________________________________
			
		/* var: width
		 * The width of the image, or zero if unknown.
		 */
		protected uint width;
		
		/* var: height
		 * The height of the image, or zero if unknown.
		 */
		protected uint height;
		}
	}