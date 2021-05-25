using System;
using System.Text.RegularExpressions;

namespace SitemapScraper
{
	internal sealed class CliArgs
	{
		public class CliArgsHelpException : Exception
		{
			public override string Message => @"Spiders from a starting URL and retrieves a list of all accessible pages, writing them to a sitemap.xml set.\n\nSYNTAX: SitemapScraper {startingURL} {sitemapXMLFilePath}";
		}
		
		public string StartingURL { get; private set; }
		public string SitemapXMLFilePath { get; private set; }
		public Regex Ignore { get; private set; }

		private CliArgs() { }

		public static CliArgs Parse(string[] args)
		{
			if(args.Length < 2) { throw new CliArgsHelpException(); }
			CliArgs cliArgs = new CliArgs()
			{
				StartingURL = args[0],
				SitemapXMLFilePath = args[1],
			};

			if(args.Length > 2)
			{
				for(int x = 2; x < args.Length; x++)
				{
					switch(args[x].Trim('-', '/'))
					{
						case "i":
						case "ignore":
							cliArgs.Ignore = new Regex(args[++x]);
						break;
					}
				}
			}

			return cliArgs;
		}
	}
}
