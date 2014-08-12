using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Scholared
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		protected string searchTerm = "";
		public static PubMedQuery PMQ;

		#region Constructors
		// Called when created from unmanaged code
		public MainWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public MainWindowController () : base ("MainWindow")
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion
		//strongly typed window accessor
		public new MainWindow Window {
			get {
				return (MainWindow)base.Window;
			}
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			// assign the custom datasource to the tableview datasource
			SearchResultsTableView.DataSource = new SearchResultsTableViewDataSource();
			SearchResultsTableView.Delegate = new SearchResultsTableDelegate (ArticleAbstract);
			PMQ = new PubMedQuery();

			// http://borkware.com/quickies/one?topic=NSScrollView
			// http://stackoverflow.com/questions/7612166/nsnotificationcenter-postnotificationname-with-null-object-does-not-trigger-b
			// first tell the contentView (the NSClipView) to post notifications when its bounds changes
			ResultsScrollView.ContentView.PostsBoundsChangedNotifications = true;
//			NSNotificationCenter center = new NSNotificationCenter ();
//			center.AddObserver("boundsDidChangeNotification", recievedNotice);

			// event callback if user pressed the enter button
			SearchTermInput.EditingEnded += (object sender, EventArgs e) => {
				// don't allow duplicate searches in sequence or search terms for empty strings
				if (SearchTermInput.StringValue != searchTerm && SearchTermInput.StringValue != "") {
					// remember last search term so duplicate searches are not made in sequence
					searchTerm = SearchTermInput.StringValue;
					PMQ.Search(searchTerm);

					// set the window title to number of results
					Window.Title = PMQ.totalMatchedItems + " results";

					// update the tableview with authors and titles
					SearchResultsTableViewDataSource.authors.Clear();
					SearchResultsTableViewDataSource.titles.Clear();
					foreach (Dictionary<string,string> article in PMQ.results) {
						SearchResultsTableViewDataSource.authors.Add(article["author"]);
						SearchResultsTableViewDataSource.titles.Add(article["title"]);
						SearchResultsTableViewDataSource.years.Add(article["year"]);
					}
					SearchResultsTableView.ReloadData();
				}
			};

//			Console.WriteLine (ResultsScrollView.ContentView.Bounds.Location);

			// bug in xamarin.mac so this doesn't work as it should
			// http://forums.xamarin.com/discussion/4632/nstableview-selectiondidchange-problem
			// https://bugzilla.xamarin.com/show_bug.cgi?id=12467
//			SearchResultsTableView.SelectionDidChange += (object sender, EventArgs e) => {
//				Console.WriteLine(SearchResultsTableView.SelectedRow);
//			};
		}
	}

	// webrequest and webresponse usage
	// http://msdn.microsoft.com/en-us/library/debx8sh9.aspx
	[Register ("PubMedQuery")]
	public class PubMedQuery
	{
		public string term;
		public string totalMatchedItems;
		public List<string> ids = new List<string>();
		public List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
		public int retstart = 0;
		public int retmax = 20;

		private string ESearchUrl = "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db={0}&tool={1},&email={2}&term={3}&usehistory=y&retmax={4}&retstart={5}";
		private string EFetchUrl = "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db={0}&tool={1}&email={2}&id={3}&mode={4}";

		public void Search (string term) {
			this.term = term;
			this.ids.Clear ();
			this.results.Clear ();

			// make the webrequest and get the response xml text
			string url = string.Format (this.ESearchUrl, "pubmed", "scholared", System.Uri.EscapeDataString("email@yourdomain.com"), System.Uri.EscapeDataString(this.term), this.retmax, this.retstart);
			WebRequest request = WebRequest.Create(url);
			WebResponse response = request.GetResponse ();
			var dataStream = response.GetResponseStream ();
			System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
			string xmlResponse = reader.ReadToEnd ();

			// parse the xml response
			System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings();
			settings.DtdProcessing = System.Xml.DtdProcessing.Ignore;
			using (System.Xml.XmlReader xmlResponseReader = System.Xml.XmlReader.Create (new System.IO.StringReader (xmlResponse), settings)) {
				// record the number of results available
				if (xmlResponseReader.ReadToFollowing ("Count")) {
					this.totalMatchedItems = xmlResponseReader.ReadElementContentAsString ();
				}
				// save ids into an array as strings
				if (xmlResponseReader.ReadToFollowing ("IdList")) {
					while (xmlResponseReader.ReadToFollowing ("Id")) {
						ids.Add(xmlResponseReader.ReadElementContentAsString());
					}
				}
//				int itr = Convert.ToInt16(this.totalMatchedItems) / this.retmax;
				// query efetch and parse efetch results
				string efetchurl = string.Format (this.EFetchUrl, "pubmed", "scholared", System.Uri.EscapeDataString("email@yourdomain.com"), System.Uri.EscapeDataString(string.Join(",",this.ids)), "xml");
				WebRequest webrequest = WebRequest.Create(efetchurl);
				WebResponse webresponse = webrequest.GetResponse ();
				var webdataStream = webresponse.GetResponseStream ();
				System.IO.StreamReader efetchreader = new System.IO.StreamReader (webdataStream);
				string xmlEfetchResponse = efetchreader.ReadToEnd ();
				using (System.Xml.XmlReader xmlEfetchResponseReader = System.Xml.XmlReader.Create (new System.IO.StringReader (xmlEfetchResponse), settings)) {
					while (xmlEfetchResponseReader.ReadToFollowing ("PubDate")) {
						string lastname = "";
						string forename = "";
						string author = "";
						Dictionary<string, string> article = new Dictionary<string, string>();

						if (xmlEfetchResponseReader.ReadToFollowing ("Year")) {
							article.Add ("year", xmlEfetchResponseReader.ReadElementContentAsString ());
						} else {
							article.Add ("year", "");
						}

						xmlEfetchResponseReader.ReadToFollowing ("ArticleTitle");
						article.Add ("title",xmlEfetchResponseReader.ReadElementContentAsString());

						if (xmlEfetchResponseReader.ReadToFollowing ("AbstractText")) {
							article.Add ("abstractText",xmlEfetchResponseReader.ReadElementContentAsString());
						} else {
							article.Add ("abstractText","");
						}

						if (xmlEfetchResponseReader.ReadToFollowing ("LastName")) {
							lastname = xmlEfetchResponseReader.ReadElementContentAsString ();
						}

						if (xmlEfetchResponseReader.ReadToFollowing ("Initials")) {
							forename = xmlEfetchResponseReader.ReadElementContentAsString ();
						}

						if (lastname != "") {
							author = lastname + ", " + forename;
						}
						article.Add ("author",author);

						this.results.Add (article);
					}
				}
			}
		}
	}

	// more information of assigning a custom datasource to tableview, including examples for List<T>
	// http://www.netneurotic.net/Mono/MonoMac-NSTableView.html
	// http://stackoverflow.com/questions/18293613/how-to-populate-table-view-in-xamarin
	// http://stackoverflow.com/questions/9696045/mono-project-how-do-i-assign-a-c-sharp-generic-list-as-a-tableviews-datasource
	[Register ("SearchResultsTableViewDataSource")]
	public partial class SearchResultsTableViewDataSource: NSTableViewDataSource
	{
		public static List<string> authors = new List<string>();
		public static List<string> titles =  new List<string>();
		public static List<string> years =  new List<string>();

		public SearchResultsTableViewDataSource ()
		{
		}

		[Export ("numberOfRowsInTableView:")]
		public int NumberOfRowsInTableView (NSTableView table) {
			return authors.Count;
		}

		[Export ("tableView:objectValueForTableColumn:row:")]
		public NSObject ObjectValueForTableColumn(NSTableView table, NSTableColumn col, int row) {
			// if requesting authors column first check the row exists in authors list
			if (col.Identifier == "author" && authors.ElementAtOrDefault(row) != null) {
				return new NSString(authors[row]);
			}
			// if requesting title column first check the row exists in titles list
			if (col.Identifier == "title" && titles.ElementAtOrDefault(row) != null) {
				return new NSString(titles[row]);
			}
			// if requesting title column first check the row exists in titles list
			if (col.Identifier == "year" && years.ElementAtOrDefault(row) != null) {
				return new NSString(years[row]);
			}
			return null;
		}
	}

	[Register ("SearchResultsTableDelegate")]
	public class SearchResultsTableDelegate : NSTableViewDelegate
	{
		protected NSTextView textView;

		public SearchResultsTableDelegate (NSTextView textView)
		{
			this.textView = textView;
		}

		[Export ("tableViewSelectionDidChange:")]
		public override void SelectionDidChange(NSNotification notification) {

		}

		[Export ("tableView:shouldSelectRow:")]
		public override bool ShouldSelectRow (NSTableView tableView, int row) {
			if (MainWindowController.PMQ.results[row].ContainsKey("abstractText")) {
				this.textView.Value = MainWindowController.PMQ.results [row] ["title"] + "\n\n" + MainWindowController.PMQ.results [row] ["abstractText"];
			} else {
				this.textView.Value = "";
			}
			return true;
		}
	}
}

