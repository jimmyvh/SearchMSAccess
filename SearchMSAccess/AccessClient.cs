using Microsoft.Office.Interop.Access;
using Microsoft.Office.Interop.Access.Dao;
using Microsoft.Vbe.Interop;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Application = Microsoft.Office.Interop.Access.Application;

namespace SearchMSAccess
{
    public class AccessClient : IDisposable
    {
        private Application application;

        public AccessClient()
        {
            application = new Application();
        }

        public IEnumerable<SearchResultModel> SearchFile(string path, string searchTerm)
        {
            if(application.CurrentDb() != null)
            {
                application.CloseCurrentDatabase();
            }
            // deze regex split tekst op per lijn, zodat we lijnnummer van zoekresultaat kunnen bepalen
            var regex = new Regex($".*\n?");
            application.OpenCurrentDatabase(path);
            foreach(VBProject project in application.VBE.VBProjects)
            {
                foreach(VBComponent component in project.VBComponents)
                {
                    var lineCount = component.CodeModule.CountOfLines;
                    for(int i = 1; i < lineCount; i++)
                    {
                        var content = component.CodeModule.Lines[i, 1];
                        if (content.ToLower().Contains(searchTerm.ToLower()))
                        {
                            yield return new SearchResultModel(path, component.Name, i, content);
                        }
                    }
                }
            }

            foreach (AccessObject queryObject in application.CurrentData.AllQueries)
            {
                QueryDef query = application.CurrentDb().OpenQueryDef(queryObject.Name);
                if (regex.IsMatch(query.SQL))
                {
                    var matches = regex.Matches(query.SQL);
                    for(int i = 0; i < matches.Count; i++)
                    {
                        if (matches[i].Value.ToLower().Contains(searchTerm.ToLower()))
                        {
                            yield return new SearchResultModel(path, "Query: " + query.Name, i + 1, matches[i].Value.Trim('\n', '\r'));
                        }
                    }
                }
            }

            application.CloseCurrentDatabase();
        }

        public void Dispose()
        {
            if (application.CurrentDb() != null)
            {
                application.CloseCurrentDatabase();
            }

            application = null;
        }
    }
}
