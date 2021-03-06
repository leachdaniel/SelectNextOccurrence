﻿using System;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace SelectNextOccurrence.Commands
{
    /// <summary>
    /// Handles keyboard, typing and combinations in text-editor
    /// </summary>
    class CommandTarget : IOleCommandTarget
    {
        private readonly IWpfTextView view;

        private ITextSnapshot Snapshot { get { return this.view.TextSnapshot; } }

        private Selector Selector { get { return this.adornmentLayer.Selector; } }

        private readonly AdornmentLayer adornmentLayer;

        public IOleCommandTarget NextCommandTarget { get; set; }

        public CommandTarget(IWpfTextView view)
        {
            this.view = view;
            this.adornmentLayer = view.Properties
                .GetProperty<AdornmentLayer>(typeof(AdornmentLayer));
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // based on ankh should return not supported when the command does nothing
            // https://ctf.open.collab.net/integration/viewvc/viewvc.cgi/trunk/src/Ankh.VS/Dialogs/VSCommandRouting.cs?view=markup&root=ankhsvn&system=exsy1005&pathrev=11527
            // https://ctf.open.collab.net/integration/viewvc/viewvc.cgi/trunk/src/Ankh.Services/VSErr.cs?view=markup&root=ankhsvn&system=exsy1005&pathrev=12510
            int result = unchecked((int)Constants.OLECMDERR_E_NOTSUPPORTED);

            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID) nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.SolutionPlatform:
                        
                        return result;
                }
            }

            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID) nCmdID)
                {
                    case VSConstants.VSStd97CmdID.SolutionCfg:
                        return result;
                }
            }

            result = VSConstants.S_OK;
            System.Diagnostics.Debug.WriteLine("grp: {0}, id: {1}", pguidCmdGroup.ToString(), nCmdID.ToString());

            if (!Selector.Selections.Any())
                return ProcessSingleCursor(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut, ref result);

            bool modifySelections = false;
            bool clearSelections = false;

            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID
                || pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
                {
                    switch (nCmdID)
                    {
                        case ((uint)VSConstants.VSStd97CmdID.Copy):
                        case ((uint)VSConstants.VSStd97CmdID.Cut):
                            return HandleCopyCut(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                        case ((uint)VSConstants.VSStd97CmdID.Paste):
                            // Only multi-paste different texts if all our selections have been copied with 
                            // this extension, otherwise paste as default. 
                            // Copied text get reset when new new selections are added
                            if (Selector.Selections.All(s => !String.IsNullOrEmpty(s.CopiedText)))
                            {
                                return HandlePaste(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            }
                            break;
                        case ((uint)VSConstants.VSStd97CmdID.Undo):
                        case ((uint)VSConstants.VSStd97CmdID.Redo):
                            result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            Selector.IsReversing = false;
                            adornmentLayer.DrawAdornments();
                            return result;
                    }
                }
                if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
                {
                    switch (nCmdID)
                    {
                        case ((uint)VSConstants.VSStd2KCmdID.LEFT):
                        case ((uint)VSConstants.VSStd2KCmdID.RIGHT):
                        case ((uint)VSConstants.VSStd2KCmdID.UP):
                        case ((uint)VSConstants.VSStd2KCmdID.DOWN):
                        case ((uint)VSConstants.VSStd2KCmdID.WORDPREV):
                        case ((uint)VSConstants.VSStd2KCmdID.WORDNEXT):
                            // Remove selected spans but keep carets
                            clearSelections = true;
                            Selector.IsReversing = false;
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.CANCEL):
                            Selector.IsReversing = false;
                            Selector.DiscardSelections();
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.PAGEDN):
                        case ((uint)VSConstants.VSStd2KCmdID.PAGEUP):
                        case ((uint)VSConstants.VSStd2KCmdID.END):
                        case ((uint)VSConstants.VSStd2KCmdID.HOME):
                        case ((uint)VSConstants.VSStd2KCmdID.END_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.HOME_EXT):
                            Selector.DiscardSelections();
                            Selector.IsReversing = false;
                            result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.WORDPREV_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.BOL_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.LEFT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.UP_EXT):
                            Selector.IsReversing = Selector.Selections.All(s => !s.IsSelection())
                                || Selector.IsReversing
                                || Selector.Selections.Last().Reversing(Snapshot);
                            modifySelections = true;
                            break;
                        case ((uint)VSConstants.VSStd2KCmdID.WORDNEXT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.EOL_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.RIGHT_EXT):
                        case ((uint)VSConstants.VSStd2KCmdID.DOWN_EXT):
                            Selector.IsReversing = !(Selector.Selections.All(s => !s.IsSelection()) || !Selector.IsReversing);
                            modifySelections = true;
                            break;
                    }
                }

                if (Selector.Selections.Any())
                {
                    result = ProcessSelections(modifySelections, clearSelections, ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }

                view.Selection.Clear();
                Selector.RemoveDuplicates();
            }
            else
            {
                if (Selector.Selections.Any())
                {
                    result = ProcessSelections(modifySelections, clearSelections, ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }

                view.Selection.Clear();
                Selector.RemoveDuplicates();
            }

            adornmentLayer.DrawAdornments();

            return result;
        }

        /// <summary>
        /// When no multiple selections are active, perform checks for multi-paste.
        /// Multi-paste gets active if its previously stored on selections that are now discarded
        /// and the current clipboards content equals the last stored clipboard-item
        /// </summary>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private int ProcessSingleCursor(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut, ref int result)
        {
            // if paste, see if we have a saved clipboard to apply
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97
                    && nCmdID == (uint)VSConstants.VSStd97CmdID.Paste
                    && Selector.SavedClipboard.Any())
            {
                // Clipboard saved, paste these on new lines if current clipboard does match the last item
                // If they dont match, a copy/cut has been made from somewhere else
                if (Clipboard.GetText() != Selector.SavedClipboard.Last())
                    Selector.ClearSavedClipboard();

                if (Selector.SavedClipboard.Count() > 1)
                {
                    int count = 1;
                    int clipboardCount = Selector.SavedClipboard.Count();

                    if (!Selector.Dte.UndoContext.IsOpen)
                        Selector.Dte.UndoContext.Open(Vsix.Name);

                    foreach (var clipboardText in Selector.SavedClipboard)
                    {
                        Clipboard.SetText(clipboardText);
                        result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                        if (count < clipboardCount)
                        {
                            Selector.editorOperations.InsertNewLine();
                            count++;
                        }
                    }

                    if (Selector.Dte.UndoContext.IsOpen)
                        Selector.Dte.UndoContext.Close();

                    return result;
                }
                else
                {
                    return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }
            else
            {
                // if copy/cut, clear saved clipboard
                if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97
                            && (nCmdID == (uint)VSConstants.VSStd97CmdID.Copy
                                || (nCmdID == (uint)VSConstants.VSStd97CmdID.Cut)
                                )
                            )
                    Selector.ClearSavedClipboard();

                // continue normal processing
                return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
        }

        private int ProcessSelections(bool modifySelections, bool clearSelections, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int result = VSConstants.S_OK;

            if (!Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Open(Vsix.Name);

            foreach (var selection in Selector.Selections)
            {
                if (selection.IsSelection())
                {
                    view.Selection.Select(
                        new SnapshotSpan(
                            selection.Start.GetPoint(Snapshot),
                            selection.End.GetPoint(Snapshot)
                        ),
                        Selector.IsReversing
                    );
                }

                view.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));

                result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                selection.Caret = Snapshot.CreateTrackingPoint(
                    view.Caret.Position.BufferPosition.Position,
                    PointTrackingMode.Positive
                );

                if (view.Selection.IsEmpty)
                {
                    selection.Start = null;
                    selection.End = null;
                    modifySelections = false;
                }

                if (modifySelections)
                {
                    var newSpan = view.Selection.StreamSelectionSpan;

                    selection.Start = Snapshot.CreateTrackingPoint(
                        newSpan.Start.Position.Position > newSpan.End.Position.Position ?
                        newSpan.End.Position.Position
                        : newSpan.Start.Position.Position,
                        PointTrackingMode.Positive
                    );

                    selection.End = Snapshot.CreateTrackingPoint(
                        newSpan.Start.Position.Position > newSpan.End.Position.Position ?
                        newSpan.Start.Position.Position
                        : newSpan.End.Position.Position,
                        PointTrackingMode.Positive
                    );

                    view.Selection.Clear();
                }
            }

            if (Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Close();

            // Set new searchtext needed if selection is modified
            if (modifySelections)
            {
                var startPosition = Selector.Selections.Last().Start.GetPosition(Snapshot);
                var endPosition = Selector.Selections.Last().End.GetPosition(Snapshot);

                Selector.SearchText = Snapshot.GetText(
                    startPosition,
                    endPosition - startPosition
                );
            }

            // Goes to caret-only mode
            if (clearSelections)
            {
                Selector.Selections.ForEach(s =>
                    {
                        s.Start = null;
                        s.End = null;
                    }
                );
            }

            return result;
        }

        /// <summary>
        /// Copies/cuts each selection as normal, and saves the text into the selection-item
        /// </summary>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <returns></returns>
        private int HandleCopyCut(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int result = VSConstants.S_OK;

            if (!Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Open(Vsix.Name);

            foreach (var selection in Selector.Selections)
            {
                if (selection.IsSelection())
                {
                    view.Selection.Select(
                        new SnapshotSpan(
                            selection.Start.GetPoint(Snapshot),
                            selection.End.GetPoint(Snapshot)
                        ),
                        false
                    );

                    // Copies/cuts and saves the text on the selection
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    selection.CopiedText = Clipboard.GetText();
                }
            }

            if (Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Close();

            return result;
        }

        /// <summary>
        /// If a previous multi-copy/cut has been made, this pastes the saved text at cursor-positions
        /// </summary>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <returns></returns>
        private int HandlePaste(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int result = VSConstants.S_OK;

            if (!Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Open(Vsix.Name);

            foreach (var selection in Selector.Selections)
            {
                if (!String.IsNullOrEmpty(selection.CopiedText))
                {
                    if (selection.IsSelection())
                    {
                        view.Selection.Select(
                            new SnapshotSpan(
                                selection.Start.GetPoint(Snapshot),
                                selection.End.GetPoint(Snapshot)
                            ),
                            false
                        );
                    }

                    view.Caret.MoveTo(selection.Caret.GetPoint(Snapshot));

                    Clipboard.SetText(selection.CopiedText);
                    result = NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            if (Selector.Dte.UndoContext.IsOpen)
                Selector.Dte.UndoContext.Close();

            return result;
        }
    }
}
