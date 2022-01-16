/*
 * Class: CodeClear.NaturalDocs.Engine.Files.ImageFile
 * ____________________________________________________________________________
 *
 * A class containing information about an image file.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
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
		public ImageFile (AbsolutePath fileName, DateTime lastModified) : base (fileName, FileType.Image, lastModified)
			{
			width = 0;
			height = 0;
			}

		/* Function: ImageFile
		 * Creates a new ImageFile with known dimensions.
		 */
		public ImageFile (AbsolutePath fileName, DateTime lastModified, uint width, uint height) : base (fileName, FileType.Image, lastModified)
			{
			SetDimensions(width, height);
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


		/* Function: SetDimensions
		 * Assigns the image to known dimensions.  If you want to set them back to unknown, set <DimensionsKnown> to
		 * false.
		 */
		public void SetDimensions (uint width, uint height)
			{
			if (width == 0 || height == 0)
				{  throw new InvalidOperationException();  }

			this.width = width;
			this.height = height;
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: DimensionsKnown
		 * Whether the image's dimensions are known.  You can only set it to false.  Use <SetDimensions()> to set it to true.
		 */
		public bool DimensionsKnown
			{
			get
				{  return (width != 0);  }
			set
				{
				if (value == true)
					{  throw new InvalidOperationException();  }

				width = 0;
				height = 0;
				}
			}

		/* Property: Width
		 * The image's width.  Check <DimensionsKnown> before reading.  Attempting to read while <DimensionsKnown> is
		 * false will throw an exception.
		 */
		public uint Width
			{
			get
				{
				if (!DimensionsKnown)
					{  throw new InvalidOperationException();  }

				return width;
				}
			}

		/* Property: Height
		 * The image's height.  Check <DimensionsKnown> before reading.  Attempting to read while <DimensionsKnown> is
		 * false will throw an exception.
		 */
		public uint Height
			{
			get
				{
				if (!DimensionsKnown)
					{  throw new InvalidOperationException();  }

				return height;
				}
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
