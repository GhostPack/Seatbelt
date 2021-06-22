#nullable disable
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using Seatbelt.Output.Formatters;
using Seatbelt.Output.TextWriters;

namespace Seatbelt.Commands
{
    internal class SearchIndexCommand : CommandBase
    {
        public override string Command => "SearchIndex";

        public override string Description => "Query results from the Windows Search Index, default term of 'passsword'. (argument(s) == <search path> <pattern1,pattern2,...>";

        public override CommandGroup[] Group => new[] { CommandGroup.Misc };
        public override bool SupportRemote => false; // maybe?

        public SearchIndexCommand(Runtime runtime) : base(runtime)
        {
        }

        private IEnumerable<WindowsSearchIndexDTO> SearchWindowsIndex(string searchPath = @"C:\Users\", string criteria = "password")
        {
            var format = @"SELECT System.ItemPathDisplay,System.FileOwner,System.Size,System.DateCreated,System.DateAccessed,System.Search.Autosummary FROM SystemIndex WHERE Contains(*, '""*{0}*""') AND SCOPE = '{1}' AND (System.FileExtension = '.txt' OR System.FileExtension = '.doc' OR System.FileExtension = '.docx' OR System.FileExtension = '.ppt' OR System.FileExtension = '.pptx' OR System.FileExtension = '.xls' OR System.FileExtension = '.xlsx' OR System.FileExtension = '.ps1' OR System.FileExtension = '.vbs' OR System.FileExtension = '.config' OR System.FileExtension = '.ini')";
            var connectionString = "Provider=Search.CollatorDSO;Extended Properties=\"Application=Windows\"";
            
            using var connection = new OleDbConnection(connectionString);
            var query = string.Format(format, criteria, searchPath);
#pragma warning disable CA2100
            var command = new OleDbCommand(query, connection);
#pragma warning restore CA2100
            connection.Open();

            OleDbDataReader reader;
            try
            {
                reader = command.ExecuteReader();
            }
            catch
            {
                WriteError("Unable to query the Search Indexer, Search Index is likely not running.");
                yield break;
            }

            while (reader.Read())
            {
                var AutoSummary = "";
                var FileOwner = "";
                try { AutoSummary = reader.GetString(5); } catch { }
                try { FileOwner = reader.GetString(1); } catch { }

                yield return new WindowsSearchIndexDTO()
                {
                    Path = reader.GetString(0),
                    FileOwner = FileOwner,
                    Size = Decimal.ToUInt64((Decimal)reader.GetValue(2)),
                    DateCreated = reader.GetDateTime(3),
                    DateAccessed = reader.GetDateTime(4),
                    AutoSummary = AutoSummary
                };
            }

            connection.Close();
        }


        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            if (args.Length == 0)
            {
                foreach (var result in SearchWindowsIndex())
                {
                    yield return result;
                }
            }
            else if (args.Length == 1)
            {
                if (System.IO.Directory.Exists(args[0]))
                {
                    foreach (var result in SearchWindowsIndex(args[0]))
                    {
                        yield return result;
                    }
                }
                else
                {
                    var terms = args[0].Split(',');
                    foreach (var term in terms)
                    {
                        foreach (var result in SearchWindowsIndex(@"C:\Users\", term))
                        {
                            yield return result;
                        }
                    }
                }
            }
            else if (args.Length == 2)
            {
                var terms = args[1].Split(',');
                foreach (var term in terms)
                {
                    foreach (var result in SearchWindowsIndex(args[0], term))
                    {
                        yield return result;
                    }
                }
            }
        }
    }

    internal class WindowsSearchIndexDTO : CommandDTOBase
    {
        public string Path { get; set; }
        public string FileOwner { get; set; }
        public UInt64 Size { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateAccessed { get; set; }
        public string AutoSummary { get; set; }
    }

    [CommandOutputType(typeof(WindowsSearchIndexDTO))]
    internal class WindowsSearchIndexTextFormatter : TextFormatterBase
    {
        public WindowsSearchIndexTextFormatter(ITextWriter writer) : base(writer)
        {
        }

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (WindowsSearchIndexDTO)result;

            WriteLine("ItemUrl          : {0}", dto.Path);
            WriteLine("FileOwner        : {0}", dto.FileOwner);
            WriteLine("Size             : {0}", dto.Size);
            WriteLine("DateCreated      : {0}", dto.DateCreated);
            WriteLine("DateAccessed     : {0}", dto.DateAccessed);
            WriteLine("AutoSummary      :");
            WriteLine("{0}\n\n", dto.AutoSummary);
        }
    }
}
#nullable enable