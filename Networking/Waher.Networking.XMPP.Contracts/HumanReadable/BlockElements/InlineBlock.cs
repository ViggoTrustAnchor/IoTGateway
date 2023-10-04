﻿using System.Threading.Tasks;

namespace Waher.Networking.XMPP.Contracts.HumanReadable.BlockElements
{
	/// <summary>
	/// Abstract base class of blocks with inline elements.
	/// </summary>
	public abstract class InlineBlock : BlockElement
	{
		private HumanReadableElement[] elements;

		/// <summary>
		/// Inline elements
		/// </summary>
		public HumanReadableElement[] Elements
		{
			get => this.elements;
			set => this.elements = value;
		}

		/// <summary>
		/// Checks if the element is well-defined.
		/// </summary>
		public override async Task<bool> IsWellDefined()
		{
			if (this.elements is null)
				return false;

			foreach (HumanReadableElement E in this.elements)
			{
				if (E is null || !await E.IsWellDefined())
					return false;
			}

			return true;
		}

		/// <summary>
		/// Generates markdown for the human-readable text.
		/// </summary>
		/// <param name="Markdown">Markdown output.</param>
		/// <param name="SectionLevel">Current section level.</param>
		/// <param name="Indentation">Current indentation.</param>
		/// <param name="Settings">Settings used for Markdown generation of human-readable text.</param>
		public override void GenerateMarkdown(MarkdownOutput Markdown, int SectionLevel, int Indentation, MarkdownSettings Settings)
		{
			if (!(this.Elements is null))
			{
				foreach (HumanReadableElement E in this.Elements)
					E.GenerateMarkdown(Markdown, SectionLevel, Indentation, Settings);
			}
		}
	}
}
