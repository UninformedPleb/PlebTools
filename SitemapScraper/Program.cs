using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AngleSharp;

namespace SitemapScraper
{
	class Program
	{
		static List<string> foundURLs = new List<string>(10000);
		static IConfiguration cfg = Configuration.Default.WithDefaultLoader();
		static IBrowsingContext ctx = BrowsingContext.New(cfg);
		static Uri startingUri;
		static Stream fs;
		static StreamWriter sw;
		static CliArgs cliArgs;

		static async Task Main(string[] args)
		{
			// parse the args
			cliArgs = CliArgs.Parse(args);

			// figure out the base domain of the starting url so we can fix any relative url's later
			startingUri = new Uri(cliArgs.StartingURL);

			// create the sitemap XML file
			Console.WriteLine($"Creating file {cliArgs.SitemapXMLFilePath}...");
			try
			{
				fs = File.OpenWrite(cliArgs.SitemapXMLFilePath);
				sw = new StreamWriter(fs);
				sw.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
				sw.WriteLine(@"<urlset xsi:schemaLocation=""http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

				// find all child urls from the starting url
				Console.WriteLine("Spidering site, please wait...");
				await ProcessURL(cliArgs.StartingURL);
				Console.WriteLine($"Spidering complete. Found {foundURLs.Count} pages.");

				// close out the file
				sw.WriteLine(@"</urlset>");
				sw.Flush();
			}
			finally
			{
				sw?.Dispose();
				fs?.Dispose();
			}
			Console.WriteLine("Finished writing file.");
		}

		private static async Task ProcessURL(string url)
		{
			// validate the url and skip things that are janky
			if(url.StartsWith("tel:")) { return; } // flat-out skip this, it breaks the Uri class
			if(url.StartsWith("#")) { return; } // skip this too, it's just a named anchor
			// convert relative urls into absolute ones
			if(url.StartsWith("/"))
			{
				url = startingUri.Scheme + "://" + startingUri.Authority + url;
			}
			else
			{
				try
				{
					Uri scrapeUrl = new Uri(url);
					if(scrapeUrl.Scheme == "tel") { return; }
					if(scrapeUrl.Authority != startingUri.Authority) { return; }
				}
				catch(UriFormatException) { return; }
			}
			// trim a trailing slash off to keep things consistent
			if(url.EndsWith("/")) { url = url.Trim('/'); }
			// now normalize the whole thing
			try
			{
				Uri normalUrl = new Uri(url);
				url = normalUrl.AbsoluteUri;
			}
			catch { return; }

			// now that the URL is "fixed", compare to the one we stored
			if(foundURLs.Contains(url)) { return; }

			// test for an "ignore" rule
			if(cliArgs.Ignore.IsMatch(url)) { return; }

			// add this URL to the list, whether it loads or not
			Console.WriteLine($"#{foundURLs.Count}: {url}");
			foundURLs.Add(url);

			// make the request and retrieve the page
			var doc = await ctx.OpenAsync(url);
			if(doc == null) { return; }

			// write this URL to the file
			sw.WriteLine("\t<url>");
			sw.WriteLine($"\t\t<loc>{url}</loc>");
			sw.WriteLine("\t</url>");

			// find all <A HREF>'s
			foreach(var child in doc.All.Where(e => e.LocalName == "a" && e.HasAttribute("href") && (!e.HasAttribute("rel") || e.Attributes["rel"].Value != "nofollow")).Select(e => e.Attributes["href"].Value).Distinct())
			{
				// process the children
				await ProcessURL(child);
			}
		}
	}
}
