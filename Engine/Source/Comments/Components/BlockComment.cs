/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.Components.BlockComment
 * ____________________________________________________________________________
 * 
 * A class to handle the generated output of a native comment which can be represented in text and list blocks.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Comments.Components
	{
	public class BlockComment
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: BlockComment
		 */
		public BlockComment ()
			{
			blocks = new List<Block>();
			}

		
		/* Function: GetTextBlock
		 * Returns a reference to the <TextBlock> associated with the tag type.  If the last block added to the comment
		 * was the same type it will be returned so new content can be appended.  If it isn't it will add a new block to the
		 * end of the list and return it.
		 */
		public TextBlock GetTextBlock (string type)
			{
			if (blocks.Count > 0 && blocks[blocks.Count - 1].Type == type)
				{  return (blocks[blocks.Count - 1] as TextBlock);  }
			else
				{
				TextBlock newBlock = new TextBlock(type);
				blocks.Add(newBlock);
				return newBlock;
				}
			}


		/* Function: GetListBlock
		 * Returns a reference to the <ListBlock> associated with the tag type.  If there are any blocks already created for
		 * the tag type it will be returned so new items can be added and they will always stay together in a single group.  
		 * If there aren't it will add a new block to the end of the list and return it.
		 */
		public ListBlock GetListBlock (string type)
			{
			foreach (var block in blocks)
				{
				if (block.Type == type)
					{  return (block as ListBlock);  }
				}

			ListBlock newBlock = new ListBlock(type);
			blocks.Add(newBlock);
			return newBlock;
			}


		
		// Group: Properties
		// __________________________________________________________________________


		/* Property: Blocks
		 * The complete list of blocks that were added to the comment.
		 */
		public List<Block> Blocks
			{
			get
				{  return blocks;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected List<Block> blocks;




		/* __________________________________________________________________________
		 * 
		 * Class: GregValure.NaturalDocs.Engine.Comments.Components.BlockComment.Block
		 * __________________________________________________________________________
		 */
		public class Block
			{
			public Block (string type)
				{
				this.Type = type;
				}

			public string Type;
			}


		/* __________________________________________________________________________
		 * 
		 * Struct: GregValure.NaturalDocs.Engine.Comments.Components.BlockComment.TextBlock
		 * __________________________________________________________________________
		 */
		public class TextBlock : Block
			{
			public TextBlock (string type) : base (type)
				{
				Text = new StringBuilder();
				}

			public StringBuilder Text;
			}


		/* __________________________________________________________________________
		 * 
		 * Struct: GregValure.NaturalDocs.Engine.Comments.Components.BlockComment.ListBlock
		 * __________________________________________________________________________
		 */
		public class ListBlock : Block
			{
			public ListBlock (string type) : base (type)
				{
				List = new List<ListItem>();
				}

			public void Add (string name, string description)
				{
				List.Add( new ListItem(name, description) );
				}

			public bool HasNames
				{
				get
					{
					foreach (var item in List)
						{
						if (item.Name != null)
							{  return true;  }
						}

					return false;
					}
				}

			public bool HasDescriptions
				{
				get
					{
					foreach (var item in List)
						{
						if (item.Description != null)
							{  return true;  }
						}

					return false;
					}
				}

			public List<ListItem> List;
			}


		/* __________________________________________________________________________
		 * 
		 * Struct: GregValure.NaturalDocs.Engine.Comments.Components.BlockComment.ListItem
		 * __________________________________________________________________________
		 */
		public struct ListItem
			{
			public ListItem (string name, string description)
				{
				this.Name = name;
				this.Description = description;
				}

			public string Name;
			public string Description;
			}
		}
	}