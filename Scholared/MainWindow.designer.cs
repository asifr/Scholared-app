// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace Scholared
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSTextView ArticleAbstract { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScrollView ResultsScrollView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView SearchResultsTableView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField SearchTermInput { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ArticleAbstract != null) {
				ArticleAbstract.Dispose ();
				ArticleAbstract = null;
			}

			if (SearchResultsTableView != null) {
				SearchResultsTableView.Dispose ();
				SearchResultsTableView = null;
			}

			if (SearchTermInput != null) {
				SearchTermInput.Dispose ();
				SearchTermInput = null;
			}

			if (ResultsScrollView != null) {
				ResultsScrollView.Dispose ();
				ResultsScrollView = null;
			}
		}
	}

	[Register ("MainWindow")]
	partial class MainWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
