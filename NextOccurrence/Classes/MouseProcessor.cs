﻿using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace NextOccurrence
{
    [Export(typeof(IMouseProcessorProvider))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Name("NextOccurrenceMouseProcessorProvider")]
    public class MouseProcessorProvider : IMouseProcessorProvider
    {
        public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            return new NextOccurrenceMouseProcessor(wpfTextView);
        }
    }

    class NextOccurrenceMouseProcessor : IMouseProcessor
    {
        private readonly IWpfTextView textView;
        private readonly NextOccurrenceAdornment adornmentLayer;

        public NextOccurrenceMouseProcessor(IWpfTextView wpfTextView)
        {
            textView = wpfTextView;
            adornmentLayer = textView.Properties.GetProperty<NextOccurrenceAdornment>(
                typeof(NextOccurrenceAdornment)
            );
        }

        public void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            // Only act on single clicks, not selections
            if (adornmentLayer != null && textView.Selection.IsEmpty)
            {
                if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                {
                    adornmentLayer.Selector.AddCurrentCaretToSelections();
                }
                else
                {
                    adornmentLayer.Selector.Selections.Clear();
                }

                adornmentLayer.DrawAdornments();
            }
        }

        /// <summary>
        /// Saves the first cursor if no previous selections has been made
        /// </summary>
        /// <param name="e"></param>
        public void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // Only act on single clicks, not selections
            if (adornmentLayer != null && textView.Selection.IsEmpty && adornmentLayer.Selector.Selections.Count == 0)
            {
                if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                {
                    adornmentLayer.Selector.AddCurrentCaretToSelections();
                }
                else
                {
                    adornmentLayer.Selector.Selections.Clear();
                }
            }
        }

#pragma warning disable S1186 // Methods should not be empty
        public void PostprocessDragEnter(DragEventArgs e)
        {
        }

        public void PostprocessDragLeave(DragEventArgs e)
        {
        }

        public void PostprocessDragOver(DragEventArgs e)
        {
        }

        public void PostprocessDrop(DragEventArgs e)
        {
        }

        public void PostprocessGiveFeedback(GiveFeedbackEventArgs e)
        {
        }

        public void PostprocessMouseDown(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseEnter(MouseEventArgs e)
        {
        }

        public void PostprocessMouseLeave(MouseEventArgs e)
        {
        }

        public void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseMove(MouseEventArgs e)
        {
        }

        public void PostprocessMouseRightButtonDown(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseRightButtonUp(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseUp(MouseButtonEventArgs e)
        {
        }

        public void PostprocessMouseWheel(MouseWheelEventArgs e)
        {
        }

        public void PostprocessQueryContinueDrag(QueryContinueDragEventArgs e)
        {
        }

        public void PreprocessDragEnter(DragEventArgs e)
        {
        }

        public void PreprocessDragLeave(DragEventArgs e)
        {
        }

        public void PreprocessDragOver(DragEventArgs e)
        {
        }

        public void PreprocessDrop(DragEventArgs e)
        {
        }

        public void PreprocessGiveFeedback(GiveFeedbackEventArgs e)
        {
        }

        public void PreprocessMouseDown(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseEnter(MouseEventArgs e)
        {
        }

        public void PreprocessMouseLeave(MouseEventArgs e)
        {
        }

        public void PreprocessMouseLeftButtonUp(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseMove(MouseEventArgs e)
        {
        }

        public void PreprocessMouseRightButtonDown(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseRightButtonUp(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseUp(MouseButtonEventArgs e)
        {
        }

        public void PreprocessMouseWheel(MouseWheelEventArgs e)
        {
        }

        public void PreprocessQueryContinueDrag(QueryContinueDragEventArgs e)
        {
        }
#pragma warning restore S1186 // Methods should not be empty

    }
}
